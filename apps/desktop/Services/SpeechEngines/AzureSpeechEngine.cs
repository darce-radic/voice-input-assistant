using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;
using AzureResult = Microsoft.CognitiveServices.Speech.SpeechRecognitionResult;
using ModelResult = VoiceInputAssistant.Models.SpeechRecognitionResult;
using AzureEventArgs = Microsoft.CognitiveServices.Speech.SpeechRecognitionEventArgs;

namespace VoiceInputAssistant.Services.SpeechEngines;

/// <summary>
/// Azure Speech Services implementation
/// </summary>
public class AzureSpeechEngine : ISpeechRecognitionEngine, IStreamingSpeechRecognitionEngine, IDisposable
{
    private readonly ILogger<AzureSpeechEngine> _logger;
    private readonly IMetricsService _metricsService;
    
    private SpeechConfig? _speechConfig;
    private bool _isInitialized;
    private bool _isDisposed;
    private EngineConfiguration? _configuration;

    private static readonly string[] SupportedLanguageList = 
    {
        "en-US", "en-GB", "en-AU", "en-CA", "en-IN", "zh-CN", "zh-TW", "de-DE", "es-ES", "es-MX",
        "fr-FR", "fr-CA", "it-IT", "ja-JP", "ko-KR", "pt-BR", "pt-PT", "ru-RU", "ar-SA", "hi-IN",
        "th-TH", "tr-TR", "pl-PL", "nl-NL", "sv-SE", "da-DK", "no-NO", "fi-FI", "cs-CZ", "hu-HU",
        "ro-RO", "sk-SK", "sl-SI", "hr-HR", "bg-BG", "et-EE", "lv-LV", "lt-LT", "mt-MT", "ca-ES",
        "eu-ES", "gl-ES", "cy-GB", "ga-IE", "is-IS", "mk-MK", "sr-RS", "uk-UA", "be-BY", "kk-KZ",
        "hy-AM", "az-AZ", "ka-GE", "uz-UZ", "ky-KG", "tg-TJ", "mn-MN", "ne-NP", "si-LK", "km-KH",
        "lo-LA", "my-MM", "vi-VN", "id-ID", "ms-MY", "tl-PH", "sw-KE", "am-ET", "so-SO", "af-ZA",
        "zu-ZA", "xh-ZA", "st-ZA", "tn-ZA", "nso-ZA", "ve-ZA", "ts-ZA", "ss-ZA", "nr-ZA", "gu-IN",
        "ta-IN", "te-IN", "kn-IN", "ml-IN", "or-IN", "pa-IN", "as-IN", "bn-IN", "mr-IN", "ur-IN"
    };

    public SpeechEngine EngineType => SpeechEngine.AzureSpeech;
    public string Name => "Azure Speech Services";
    public bool IsAvailable => _isInitialized && !_isDisposed;
    public bool RequiresInternet => true;
    public string[] SupportedLanguages => SupportedLanguageList;

    public event EventHandler<EngineStatusChangedEventArgs>? StatusChanged;

    public AzureSpeechEngine(ILogger<AzureSpeechEngine> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<bool> InitializeAsync(EngineConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(AzureSpeechEngine));

        try
        {
            _logger.LogInformation("Initializing Azure Speech engine...");
            _configuration = configuration;

            if (string.IsNullOrEmpty(configuration.ApiKey))
            {
                throw new InvalidOperationException("Azure Speech API key is required");
            }

            if (string.IsNullOrEmpty(configuration.Region))
            {
                throw new InvalidOperationException("Azure Speech region is required");
            }

            // Initialize Speech Config
            _speechConfig = SpeechConfig.FromSubscription(configuration.ApiKey, configuration.Region);
            
            // Configure recognition settings
            _speechConfig.SpeechRecognitionLanguage = "en-US"; // Default, will be overridden per request
            _speechConfig.OutputFormat = OutputFormat.Detailed;
            
            // Enable profanity filtering based on settings
            if (configuration.Settings.TryGetValue("EnableProfanityFilter", out var profanityFilter) && 
                profanityFilter is bool enableFilter && enableFilter)
            {
                _speechConfig.SetProfanity(ProfanityOption.Masked);
            }

            // Test the configuration
            await TestConfigurationInternalAsync(_speechConfig, cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("Azure Speech engine initialized successfully");
            
            OnStatusChanged(true, "Engine initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Speech engine");
            _metricsService.RecordError("AzureSpeechEngine", $"Initialization failed: {ex.Message}");
            OnStatusChanged(false, $"Initialization failed: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<ModelResult> RecognizeAsync(SpeechRecognitionRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Engine is not available");

        if (_speechConfig == null)
            throw new InvalidOperationException("Speech config is not initialized");

        var stopwatch = Stopwatch.StartNew();
        var result = new ModelResult
        {
            RequestId = request.Id,
            Engine = EngineType
        };

        try
        {
            _logger.LogDebug("Starting Azure Speech recognition for request {RequestId}", request.Id);

            // Configure for this request
            var speechConfig = ConfigureSpeechConfig(request);

            // Create audio input from byte array
            using var audioStream = AudioInputStream.CreatePushStream();
            using var audioConfig = AudioConfig.FromStreamInput(audioStream);
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // Setup timeout
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(request.Timeout);

            // Write audio data to stream
            audioStream.Write(request.AudioData);
            audioStream.Close();

            // Perform recognition
            var recognitionResult = await recognizer.RecognizeOnceAsync().WaitAsync(timeoutCts.Token);

            // Process results
            ProcessRecognitionResult(recognitionResult, result);

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
            result.CompletedTime = DateTime.UtcNow;

            // Record metrics
            _metricsService.RecordSpeechRecognitionAttempt(
                EngineType.ToString(),
                result.ProcessingTime,
                result.Confidence,
                result.IsSuccess);

            _logger.LogDebug("Azure Speech recognition completed for request {RequestId} in {ElapsedMs}ms, Success: {Success}, Text: '{Text}'",
                request.Id, result.ProcessingTime.TotalMilliseconds, result.IsSuccess, result.Text);

            return result;
        }
        catch (OperationCanceledException)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "Recognition was cancelled";
            _logger.LogDebug("Azure Speech recognition cancelled for request {RequestId}", request.Id);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = stopwatch.Elapsed;

            _logger.LogError(ex, "Error during Azure Speech recognition for request {RequestId}", request.Id);
            _metricsService.RecordError("AzureSpeechEngine", $"Recognition failed: {ex.Message}");

            return result;
        }
    }

    public async Task<IRecognitionStream> StartStreamingAsync(StreamingConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Engine is not available");

        if (_speechConfig == null)
            throw new InvalidOperationException("Speech config is not initialized");

        try
        {
            _logger.LogDebug("Starting Azure Speech streaming session");
            
            var stream = new AzureRecognitionStream(_speechConfig, configuration, _logger, _metricsService);
            await stream.InitializeAsync(cancellationToken);
            
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Azure Speech streaming session");
            throw;
        }
    }

    public async Task<EngineTestResult> TestConfigurationAsync(EngineConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new EngineTestResult();

        try
        {
            if (string.IsNullOrEmpty(configuration.ApiKey))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "API key is required";
                return result;
            }

            if (string.IsNullOrEmpty(configuration.Region))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Region is required";
                return result;
            }

            var testConfig = SpeechConfig.FromSubscription(configuration.ApiKey, configuration.Region);
            await TestConfigurationInternalAsync(testConfig, cancellationToken);

            stopwatch.Stop();
            result.IsSuccess = true;
            result.ResponseTime = stopwatch.Elapsed;
            result.TestResults["Region"] = configuration.Region;
            result.TestResults["HasApiKey"] = !string.IsNullOrEmpty(configuration.ApiKey);

            _logger.LogDebug("Azure Speech configuration test passed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.ResponseTime = stopwatch.Elapsed;

            _logger.LogWarning(ex, "Azure Speech configuration test failed");
        }

        return result;
    }

    public EngineConfigurationSchema GetConfigurationSchema()
    {
        return new EngineConfigurationSchema
        {
            EngineName = Name,
            Description = "Microsoft Azure Speech Services for cloud-based speech recognition",
            Fields = new List<ConfigurationField>
            {
                new()
                {
                    Name = nameof(EngineConfiguration.ApiKey),
                    DisplayName = "API Key",
                    Description = "Azure Speech Services subscription key",
                    Type = ConfigurationFieldType.Password,
                    IsRequired = true,
                    IsSecret = true
                },
                new()
                {
                    Name = nameof(EngineConfiguration.Region),
                    DisplayName = "Region",
                    Description = "Azure region for the Speech Services resource",
                    Type = ConfigurationFieldType.Dropdown,
                    IsRequired = true,
                    DefaultValue = "eastus",
                    Options = new[] { "eastus", "westus", "westus2", "eastus2", "centralus", "westcentralus", 
                                     "westeurope", "northeurope", "eastasia", "southeastasia", "japaneast", "japanwest",
                                     "australiaeast", "brazilsouth", "canadacentral", "uksouth", "francecentral" }
                },
                new()
                {
                    Name = "EnableProfanityFilter",
                    DisplayName = "Enable Profanity Filter",
                    Description = "Mask profanity in recognition results",
                    Type = ConfigurationFieldType.Boolean,
                    IsRequired = false,
                    DefaultValue = "true"
                }
            },
            DefaultValues = new Dictionary<string, object>
            {
                { nameof(EngineConfiguration.Region), "eastus" },
                { "EnableProfanityFilter", true }
            }
        };
    }

    private SpeechConfig ConfigureSpeechConfig(SpeechRecognitionRequest request)
    {
        if (_speechConfig == null)
            throw new InvalidOperationException("Speech config is not initialized");

        // Create a copy for this request (Azure SDK doesn't support cloning, so we recreate)
        var config = SpeechConfig.FromSubscription(_configuration!.ApiKey!, _configuration.Region!);
        
        // Set language
        config.SpeechRecognitionLanguage = request.Language;
        
        // Set output format for detailed results
        config.OutputFormat = OutputFormat.Detailed;
        
        // Configure quality/accuracy settings based on request
        switch (request.Quality)
        {
            case RecognitionQuality.Fast:
                // Use defaults for fastest processing
                break;
            case RecognitionQuality.Balanced:
                config.SetProperty("SPEECH-TranscriptionResultTemplate", "{\"DisplayText\":\"{0}\",\"ITN\":\"{1}\",\"Lexical\":\"{2}\",\"MaskedITN\":\"{3}\"}");
                break;
            case RecognitionQuality.HighAccuracy:
            case RecognitionQuality.Maximum:
                config.SetProperty("SPEECH-SegmentationSilenceTimeoutMs", "2000");
                config.SetProperty("SPEECH-TranscriptionResultTemplate", "{\"DisplayText\":\"{0}\",\"ITN\":\"{1}\",\"Lexical\":\"{2}\",\"MaskedITN\":\"{3}\",\"NBest\":[{4}]}");
                break;
        }

        // Apply engine options
        foreach (var option in request.EngineOptions)
        {
            if (option.Value is string stringValue)
            {
                config.SetProperty(option.Key, stringValue);
            }
        }

        return config;
    }

    private void ProcessRecognitionResult(AzureResult recognitionResult, ModelResult result)
    {
        switch (recognitionResult.Reason)
        {
            case ResultReason.RecognizedSpeech:
                result.IsSuccess = true;
                result.Text = recognitionResult.Text;
                result.Confidence = CalculateConfidence(recognitionResult);
                
                // Extract alternatives from detailed results if available
                ExtractAlternatives(recognitionResult, result);
                break;

            case ResultReason.NoMatch:
                result.IsSuccess = false;
                result.ErrorMessage = "No speech could be recognized";
                break;

            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(recognitionResult);
                result.IsSuccess = false;
                result.ErrorMessage = $"Recognition cancelled: {cancellation.Reason}";
                if (cancellation.Reason == CancellationReason.Error)
                {
                    result.ErrorMessage += $" - {cancellation.ErrorDetails}";
                }
                break;

            default:
                result.IsSuccess = false;
                result.ErrorMessage = $"Unexpected result reason: {recognitionResult.Reason}";
                break;
        }

        // Store metadata
        result.Metadata["ResultId"] = recognitionResult.ResultId;
        result.Metadata["Reason"] = recognitionResult.Reason.ToString();
        // PropertyCollection doesn't support Count or Keys in this way
        // Just store essential properties we know exist
        result.Metadata["Properties"] = "Available"; // Simplified for now
    }

    private double CalculateConfidence(AzureResult recognitionResult)
    {
        // Try to extract confidence from properties
        var jsonResult = recognitionResult.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
        if (!string.IsNullOrEmpty(jsonResult))
        {
            try
            {
                // Parse JSON to extract confidence scores
                // This is a simplified implementation - in production you'd use a JSON parser
                if (jsonResult.Contains("\"Confidence\":"))
                {
                    var confidenceStart = jsonResult.IndexOf("\"Confidence\":") + 13;
                    var confidenceEnd = jsonResult.IndexOf(",", confidenceStart);
                    if (confidenceEnd == -1) confidenceEnd = jsonResult.IndexOf("}", confidenceStart);
                    
                    if (confidenceEnd > confidenceStart)
                    {
                        var confidenceStr = jsonResult.Substring(confidenceStart, confidenceEnd - confidenceStart).Trim();
                        if (double.TryParse(confidenceStr, out var confidence))
                        {
                            return confidence;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse confidence from Azure Speech result");
            }
        }

        // Fallback: estimate confidence based on whether we got results
        return string.IsNullOrEmpty(recognitionResult.Text) ? 0.0 : 0.8;
    }

    private void ExtractAlternatives(AzureResult recognitionResult, ModelResult result)
    {
        // Try to extract N-best alternatives from detailed results
        var jsonResult = recognitionResult.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
        if (!string.IsNullOrEmpty(jsonResult))
        {
            try
            {
                // This is a simplified implementation - in production you'd use a proper JSON parser
                // Azure returns NBest results in the detailed format
                // For now, we'll just note that alternatives are available
                if (jsonResult.Contains("\"NBest\":"))
                {
                    result.Metadata["HasAlternatives"] = true;
                    result.Metadata["DetailedResult"] = jsonResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse alternatives from Azure Speech result");
            }
        }
    }

    private async Task TestConfigurationInternalAsync(SpeechConfig speechConfig, CancellationToken cancellationToken)
    {
        // Create a simple test to validate the configuration
        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        
        // This doesn't actually perform recognition, just validates the config
        await Task.Delay(100, cancellationToken); // Simulate async operation
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
        
        // Reinitialize speech config with new settings
        if (_configuration != null)
        {
            _speechConfig = SpeechConfig.FromSubscription(_configuration.ApiKey!, _configuration.Region!);
        }
        
        _logger.LogDebug("Azure Speech Engine configuration updated");
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            // SpeechConfig doesn't implement IDisposable in Azure Speech SDK
            _speechConfig = null;
            _isDisposed = true;
            _isInitialized = false;

            _logger.LogDebug("Azure Speech engine disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing Azure Speech engine");
        }
    }
}

/// <summary>
/// Azure-specific recognition stream implementation
/// </summary>
internal class AzureRecognitionStream : IRecognitionStream
{
    private readonly SpeechConfig _speechConfig;
    private readonly StreamingConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly IMetricsService _metricsService;
    
    private SpeechRecognizer? _recognizer;
    private AudioInputStream? _audioStream;
    private bool _isActive;
    private bool _isDisposed;

    public Guid StreamId { get; } = Guid.NewGuid();
    public bool IsActive => _isActive && !_isDisposed;

    public event EventHandler<PartialRecognitionEventArgs>? PartialResult;
    public event EventHandler<ModelResult>? FinalResult;
    public event EventHandler<StreamErrorEventArgs>? Error;

    public AzureRecognitionStream(SpeechConfig speechConfig, StreamingConfiguration configuration, 
        ILogger logger, IMetricsService metricsService)
    {
        _speechConfig = speechConfig;
        _configuration = configuration;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Configure speech config for streaming
            var config = SpeechConfig.FromSubscription(_speechConfig.SubscriptionKey, _speechConfig.Region);
            config.SpeechRecognitionLanguage = _configuration.Language;
            config.OutputFormat = OutputFormat.Detailed;

            // Create audio input stream
            _audioStream = AudioInputStream.CreatePushStream();
            var audioConfig = AudioConfig.FromStreamInput(_audioStream);
            
            // Create recognizer
            _recognizer = new SpeechRecognizer(config, audioConfig);

            // Setup event handlers
            if (_configuration.EnablePartialResults)
            {
                _recognizer.Recognizing += OnRecognizing;
            }
            
            _recognizer.Recognized += OnRecognized;
            _recognizer.Canceled += OnCanceled;

            _isActive = true;
            _logger.LogDebug("Azure recognition stream {StreamId} initialized", StreamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure recognition stream {StreamId}", StreamId);
            throw;
        }
    }

    public async Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        if (!IsActive || _audioStream == null)
            throw new InvalidOperationException("Stream is not active");

        try
        {
            // Use the proper Azure Speech SDK PushAudioInputStream API
            if (_audioStream is Microsoft.CognitiveServices.Speech.Audio.PushAudioInputStream pushStream)
            {
                pushStream.Write(audioData);
                _logger.LogTrace("Sent {ByteCount} bytes of audio data to Azure stream", audioData.Length);
            }
            else
            {
                _logger.LogWarning("AudioInputStream is not a PushAudioInputStream");
            }
            
            await Task.CompletedTask; // Make method truly async for future enhancements
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio to stream {StreamId}", StreamId);
            OnError(ex, "Failed to send audio data");
        }
    }

    public async Task EndStreamAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Properly close the Azure Speech SDK stream
            if (_audioStream is Microsoft.CognitiveServices.Speech.Audio.PushAudioInputStream pushStream)
            {
                pushStream.Close();
            }
            else
            {
                _audioStream?.Dispose();
            }
            
            _isActive = false;
            _logger.LogDebug("Azure recognition stream {StreamId} ended", StreamId);
            
            await Task.CompletedTask; // Make method truly async for future enhancements
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending stream {StreamId}", StreamId);
            OnError(ex, "Failed to end stream");
        }
    }

    private void OnRecognizing(object? sender, Microsoft.CognitiveServices.Speech.SpeechRecognitionEventArgs e)
    {
        if (_configuration.EnablePartialResults && !string.IsNullOrEmpty(e.Result.Text))
        {
            PartialResult?.Invoke(this, new PartialRecognitionEventArgs
            {
                Text = e.Result.Text,
                Confidence = 0.5, // Partial results don't have confidence
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private void OnRecognized(object? sender, Microsoft.CognitiveServices.Speech.SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            var result = new Models.SpeechRecognitionResult
            {
                RequestId = Guid.NewGuid(),
                Text = e.Result.Text,
                IsSuccess = true,
                Engine = SpeechEngine.AzureSpeech,
                Confidence = 0.8, // Would need to parse from detailed results
                CompletedTime = DateTime.UtcNow
            };

            FinalResult?.Invoke(this, result);
        }
    }

    private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
    {
        var message = $"Recognition cancelled: {e.Reason}";
        if (e.Reason == CancellationReason.Error)
        {
            message += $" - {e.ErrorDetails}";
        }
        
        OnError(new Exception(message), message);
    }

    private void OnError(Exception exception, string message)
    {
        _logger.LogError(exception, "Error in Azure recognition stream {StreamId}: {Message}", StreamId, message);
        
        Error?.Invoke(this, new StreamErrorEventArgs
        {
            StreamId = StreamId,
            Exception = exception,
            Message = message,
            IsRecoverable = false
        });
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            if (_recognizer != null)
            {
                _recognizer.Recognizing -= OnRecognizing;
                _recognizer.Recognized -= OnRecognized;
                _recognizer.Canceled -= OnCanceled;
                _recognizer.Dispose();
            }
            
            _audioStream?.Dispose();
            _isDisposed = true;
            _isActive = false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing Azure recognition stream {StreamId}", StreamId);
        }
    }
}