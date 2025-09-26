using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;
using Whisper.net;
using Whisper.net.Ggml;

namespace VoiceInputAssistant.Services.SpeechEngines;

/// <summary>
/// Local Whisper implementation using Whisper.net
/// </summary>
public class WhisperLocalEngine : ISpeechRecognitionEngine, IVoiceActivityDetection, IDisposable
{
    private readonly ILogger<WhisperLocalEngine> _logger;
    private readonly IMetricsService _metricsService;
    
    private WhisperFactory? _whisperFactory;
    private WhisperProcessor? _whisperProcessor;
    private bool _isInitialized;
    private bool _isDisposed;
    private EngineConfiguration? _configuration;
    
    private static readonly Dictionary<RecognitionQuality, string> QualityModelMap = new()
    {
        { RecognitionQuality.Fast, "ggml-tiny.bin" },
        { RecognitionQuality.Balanced, "ggml-base.bin" },
        { RecognitionQuality.HighAccuracy, "ggml-small.bin" },
        { RecognitionQuality.Maximum, "ggml-medium.bin" }
    };

    private static readonly string[] SupportedLanguageList = 
    {
        "en", "zh", "de", "es", "ru", "ko", "fr", "ja", "pt", "tr", "pl", "ca", "nl", 
        "ar", "sv", "it", "id", "hi", "fi", "vi", "he", "uk", "el", "ms", "cs", "ro",
        "da", "hu", "ta", "no", "th", "ur", "hr", "bg", "lt", "la", "mi", "ml", "cy",
        "sk", "te", "fa", "lv", "bn", "sr", "az", "sl", "kn", "et", "mk", "br", "eu",
        "is", "hy", "ne", "mn", "bs", "kk", "sq", "sw", "gl", "mr", "pa", "si", "km",
        "sn", "yo", "so", "af", "oc", "ka", "be", "tg", "sd", "gu", "am", "yi", "lo",
        "uz", "fo", "ht", "ps", "tk", "nn", "mt", "sa", "lb", "my", "bo", "tl", "mg",
        "as", "tt", "haw", "ln", "ha", "ba", "jw", "su"
    };

    public SpeechEngine EngineType => SpeechEngine.WhisperLocal;
    public string Name => "Whisper Local";
    public bool IsAvailable => _isInitialized && !_isDisposed;
    public bool RequiresInternet => false;
    public string[] SupportedLanguages => SupportedLanguageList;

    public event EventHandler<EngineStatusChangedEventArgs>? StatusChanged;

    public WhisperLocalEngine(ILogger<WhisperLocalEngine> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<bool> InitializeAsync(EngineConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(WhisperLocalEngine));

        try
        {
            _logger.LogInformation("Initializing Whisper Local engine...");
            _configuration = configuration;

            // Get model path from configuration
            var modelPath = configuration.ModelPath;
            if (string.IsNullOrEmpty(modelPath))
            {
                // Use default model based on quality setting
                var modelFileName = QualityModelMap[configuration.DefaultQuality];
                modelPath = Path.Combine(GetModelsDirectory(), modelFileName);
            }

            // Download model if it doesn't exist
            if (!File.Exists(modelPath))
            {
                _logger.LogInformation("Model not found at {ModelPath}, downloading...", modelPath);
                await DownloadModelAsync(modelPath, configuration.DefaultQuality, cancellationToken);
            }

            // Initialize Whisper factory
            _whisperFactory = WhisperFactory.FromPath(modelPath);
            
            // Test the processor
            _whisperProcessor = _whisperFactory.CreateBuilder()
                .WithLanguage("en")
                .WithProbabilities()
                .WithSegmentEventHandler(OnSegmentReceived)
                .Build();

            _isInitialized = true;
            _logger.LogInformation("Whisper Local engine initialized successfully");
            
            OnStatusChanged(true, "Engine initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Whisper Local engine");
            _metricsService.RecordError("WhisperLocalEngine", $"Initialization failed: {ex.Message}");
            OnStatusChanged(false, $"Initialization failed: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<SpeechRecognitionResult> RecognizeAsync(SpeechRecognitionRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Engine is not available");

        if (_whisperProcessor == null)
            throw new InvalidOperationException("Whisper processor is not initialized");

        var stopwatch = Stopwatch.StartNew();
        var result = new SpeechRecognitionResult
        {
            RequestId = request.Id,
            Engine = EngineType
        };

        try
        {
            _logger.LogDebug("Starting Whisper recognition for request {RequestId}", request.Id);

            // Convert audio data to the format expected by Whisper
            var audioData = PrepareAudioData(request.AudioData);
            
            // Configure processor for this request
            ConfigureProcessorForRequest(request);

            // Process the audio
            await foreach (var segment in _whisperProcessor.ProcessAsync(audioData, cancellationToken))
            {
                if (segment.Text.Trim().Length > 0)
                {
                    result.Text += segment.Text;
                    result.Confidence = Math.Max(result.Confidence, segment.Probability);
                    
                    // Add alternative if it has different text
                    if (segment.Probability < 0.9 && !string.IsNullOrEmpty(segment.Text))
                    {
                        result.Alternatives.Add(new AlternativeTranscription
                        {
                            Text = segment.Text.Trim(),
                            Confidence = segment.Probability
                        });
                    }
                }
            }

            result.Text = result.Text.Trim();
            result.IsSuccess = !string.IsNullOrEmpty(result.Text);
            
            if (!result.IsSuccess && string.IsNullOrEmpty(result.ErrorMessage))
            {
                result.ErrorMessage = "No speech detected in audio";
            }

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
            result.CompletedTime = DateTime.UtcNow;

            // Record metrics
            _metricsService.RecordSpeechRecognitionAttempt(
                EngineType.ToString(), 
                result.ProcessingTime, 
                result.Confidence, 
                result.IsSuccess);

            _logger.LogDebug("Whisper recognition completed for request {RequestId} in {ElapsedMs}ms, Success: {Success}, Text: '{Text}'",
                request.Id, result.ProcessingTime.TotalMilliseconds, result.IsSuccess, result.Text);

            return result;
        }
        catch (OperationCanceledException)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "Recognition was cancelled";
            _logger.LogDebug("Whisper recognition cancelled for request {RequestId}", request.Id);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = stopwatch.Elapsed;
            
            _logger.LogError(ex, "Error during Whisper recognition for request {RequestId}", request.Id);
            _metricsService.RecordError("WhisperLocalEngine", $"Recognition failed: {ex.Message}");
            
            return result;
        }
    }

    public async Task<EngineTestResult> TestConfigurationAsync(EngineConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new EngineTestResult();

        try
        {
            // Test if we can create a temporary instance
            var modelPath = configuration.ModelPath;
            if (string.IsNullOrEmpty(modelPath))
            {
                var modelFileName = QualityModelMap[configuration.DefaultQuality];
                modelPath = Path.Combine(GetModelsDirectory(), modelFileName);
            }

            if (!File.Exists(modelPath))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Model file not found at {modelPath}";
                return result;
            }

            // Try to create a factory instance
            using var testFactory = WhisperFactory.FromPath(modelPath);
            using var testProcessor = testFactory.CreateBuilder()
                .WithLanguage("en")
                .Build();

            stopwatch.Stop();
            result.IsSuccess = true;
            result.ResponseTime = stopwatch.Elapsed;
            result.TestResults["ModelPath"] = modelPath;
            result.TestResults["ModelExists"] = true;
            
            _logger.LogDebug("Whisper configuration test passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.ResponseTime = stopwatch.Elapsed;
            
            _logger.LogWarning(ex, "Whisper configuration test failed");
        }

        return result;
    }

    public EngineConfigurationSchema GetConfigurationSchema()
    {
        return new EngineConfigurationSchema
        {
            EngineName = Name,
            Description = "Local Whisper speech recognition using OpenAI's Whisper models",
            Fields = new List<ConfigurationField>
            {
                new()
                {
                    Name = nameof(EngineConfiguration.ModelPath),
                    DisplayName = "Model Path",
                    Description = "Path to the Whisper model file (optional - will auto-download if not specified)",
                    Type = ConfigurationFieldType.FilePath,
                    IsRequired = false
                },
                new()
                {
                    Name = nameof(EngineConfiguration.DefaultQuality),
                    DisplayName = "Default Quality",
                    Description = "Default recognition quality level",
                    Type = ConfigurationFieldType.Dropdown,
                    IsRequired = true,
                    DefaultValue = RecognitionQuality.Balanced.ToString(),
                    Options = Enum.GetNames<RecognitionQuality>()
                }
            },
            DefaultValues = new Dictionary<string, object>
            {
                { nameof(EngineConfiguration.DefaultQuality), RecognitionQuality.Balanced }
            }
        };
    }

    public Task<VoiceActivityResult> DetectVoiceActivityAsync(byte[] audioData, int sampleRate)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Engine is not available");

        // Simple energy-based VAD implementation
        // In a production implementation, you might use more sophisticated algorithms
        var samples = ConvertBytesToSamples(audioData);
        var energy = CalculateRMSEnergy(samples);
        
        // Threshold can be made configurable
        var threshold = 0.01;
        var hasVoice = energy > threshold;
        
        var result = new VoiceActivityResult
        {
            HasVoice = hasVoice,
            Confidence = hasVoice ? Math.Min(energy * 10, 1.0) : 0.0,
            EnergyLevel = energy,
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromMilliseconds(audioData.Length / (sampleRate * 2.0) * 1000)
        };
        
        return Task.FromResult(result);
    }

    public void ConfigureVAD(double sensitivity)
    {
        // Store VAD sensitivity for future use
        _logger.LogDebug("VAD sensitivity configured to {Sensitivity}", sensitivity);
    }

    private void ConfigureProcessorForRequest(SpeechRecognitionRequest request)
    {
        // The processor is immutable once created, so configuration changes
        // would require recreating it. For now, we'll use the default configuration.
        // In a more advanced implementation, we might cache processors for different configurations.
    }

    private float[] PrepareAudioData(byte[] audioBytes)
    {
        // Convert byte array to float array expected by Whisper
        var samples = new float[audioBytes.Length / 2];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = BitConverter.ToInt16(audioBytes, i * 2) / 32768f;
        }
        return samples;
    }

    private float[] ConvertBytesToSamples(byte[] audioBytes)
    {
        var samples = new float[audioBytes.Length / 2];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = BitConverter.ToInt16(audioBytes, i * 2) / 32768f;
        }
        return samples;
    }

    private double CalculateRMSEnergy(float[] samples)
    {
        if (samples.Length == 0) return 0;
        
        double sum = samples.Sum(sample => sample * sample);
        return Math.Sqrt(sum / samples.Length);
    }

    private async Task DownloadModelAsync(string modelPath, RecognitionQuality quality, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Downloading Whisper model for quality {Quality}...", quality);
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
            
            // Use Whisper.net's built-in model downloader with correct API
            var modelType = quality switch
            {
                RecognitionQuality.Fast => GgmlType.Tiny,
                RecognitionQuality.Balanced => GgmlType.Base,
                RecognitionQuality.HighAccuracy => GgmlType.Small,
                RecognitionQuality.Maximum => GgmlType.Medium,
                _ => GgmlType.Base
            };

            // Download model stream and save to specified path
            // The Whisper.net API now returns a Stream that we need to save
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(modelType);
            
            // Save the stream to the specified file path
            using var fileStream = File.Create(modelPath);
            await modelStream.CopyToAsync(fileStream, cancellationToken);
            
            _logger.LogDebug("Model stream saved to {ModelPath}", modelPath);
            
            _logger.LogInformation("Model downloaded successfully to {ModelPath}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Whisper model");
            throw;
        }
    }

    private string GetModelsDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var modelsPath = Path.Combine(appDataPath, "VoiceInputAssistant", "Models", "Whisper");
        Directory.CreateDirectory(modelsPath);
        return modelsPath;
    }

    private void OnSegmentReceived(SegmentData segment)
    {
        // Handle segment data if needed for streaming scenarios
        _logger.LogTrace("Received segment: {Text} (Probability: {Probability})", 
            segment.Text, segment.Probability);
    }

    private void OnStatusChanged(bool isAvailable, string? message = null, Exception? error = null)
    {
        StatusChanged?.Invoke(this, new EngineStatusChangedEventArgs
        {
            Engine = EngineType,
            IsAvailable = isAvailable,
            StatusMessage = message,
            Error = error
        });
    }

    public void UpdateConfiguration(EngineConfiguration configuration)
    {
        _configuration = configuration;
        
        // Reinitialize the engine with new configuration
        Task.Run(async () => await InitializeAsync(configuration));
        
        _logger.LogDebug("Whisper Local Engine configuration updated");
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            _whisperProcessor?.Dispose();
            _whisperFactory?.Dispose();
            _isDisposed = true;
            _isInitialized = false;
            
            _logger.LogDebug("Whisper Local engine disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing Whisper Local engine");
        }
    }
}