using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Interfaces;
using Whisper.net;
using Whisper.net.Ggml;

namespace VoiceInputAssistant.Infrastructure.Services;

/// <summary>
/// Speech recognition service using local Whisper model
/// </summary>
public class WhisperSpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private readonly ILogger<WhisperSpeechRecognitionService> _logger;
    private readonly WhisperSettings _settings;
    private readonly IAdaptiveLearningService? _adaptiveLearningService;
    private WhisperProcessor? _processor;
    private WhisperFactory? _factory;
    private bool _isInitialized;
    private bool _isListening;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Event raised when transcription is available
    /// </summary>
    public event EventHandler<SpeechRecognitionResult>? TranscriptionReceived;

    /// <inheritdoc />
    public bool IsListening => _isListening;

    /// <inheritdoc />
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc />
    public SpeechEngineType? CurrentEngine => SpeechEngineType.WhisperLocal;

public WhisperSpeechRecognitionService(
        ILogger<WhisperSpeechRecognitionService> logger,
        IOptions<WhisperSettings> settings,
        IAdaptiveLearningService? adaptiveLearningService = null)
    {
        _logger = logger;
        _settings = settings.Value;
        _adaptiveLearningService = adaptiveLearningService;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            // Download model if not present
            if (!System.IO.File.Exists(_settings.ModelPath))
            {
                _logger.LogInformation("Downloading Whisper model...");
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
                using var fileStream = System.IO.File.Create(_settings.ModelPath);
                await modelStream.CopyToAsync(fileStream);
            }

            // Create factory and processor
            _factory = WhisperFactory.FromPath(_settings.ModelPath);
            _processor = _factory.CreateBuilder()
                .WithLanguage("auto")
                .Build();
            _isInitialized = true;
            _logger.LogInformation("Whisper initialized successfully with model at {ModelPath}", _settings.ModelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Whisper");
            throw;
        }
    }

    public Task StartListeningAsync()
    {
        if (_isListening)
        {
            return Task.CompletedTask;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _isListening = true;
        _logger.LogInformation("Whisper listening started");
        return Task.CompletedTask;
    }

    public Task StopListeningAsync()
    {
        if (!_isListening)
        {
            return Task.CompletedTask;
        }

        _cancellationTokenSource?.Cancel();
        _isListening = false;
        _logger.LogInformation("Whisper listening stopped");
        return Task.CompletedTask;
    }

    public async Task<SpeechRecognitionResult> TranscribeAsync(byte[] audioData)
    {
        if (!_isInitialized || _processor == null)
        {
            throw new InvalidOperationException("Whisper not initialized. Call InitializeAsync first.");
        }

        try
        {
            // Process audio data
            using var audioStream = new System.IO.MemoryStream(audioData);
            
            // Build transcription result
            var text = string.Empty;
            float confidence = 0;
            var segmentCount = 0;

            await foreach (var segment in _processor.ProcessAsync(audioStream))
            {
                text += segment.Text + " ";
                confidence += segment.Probability; // Use Probability instead of Confidence
                segmentCount++;
            }

            // Average confidence across segments
            confidence = segmentCount > 0 ? confidence / segmentCount : 0;
            var finalText = text.Trim();

            // Apply adaptive learning post-processing if available
            try
            {
                if (_adaptiveLearningService != null && !string.IsNullOrWhiteSpace(finalText))
                {
                    // Try to apply learned corrections automatically for high-confidence cases
                    var suggestions = await _adaptiveLearningService.GetContextSuggestionsAsync(
                        finalText, "general", "system");
                    
                    if (suggestions?.Any() == true)
                    {
                        var bestSuggestion = suggestions
                            .OrderByDescending(s => s.ConfidenceScore)
                            .FirstOrDefault();
                            
                        if (bestSuggestion != null && bestSuggestion.ConfidenceScore > 0.9f)
                        {
                            _logger.LogDebug("Applying high-confidence adaptive learning correction: '{Original}' -> '{Corrected}'",
                                finalText, bestSuggestion.SuggestedText);
                            finalText = bestSuggestion.SuggestedText;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply adaptive learning corrections");
                // Don't fail transcription if adaptive learning fails
            }

            return new SpeechRecognitionResult
            {
                Text = finalText,
                Confidence = confidence,
                Language = "auto", // Whisper auto-detects language
                Engine = SpeechEngineType.WhisperLocal,
                Success = true,
                IsFinal = true,
                Timestamp = DateTimeOffset.UtcNow,
                WordCount = finalText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper transcription failed");
            return new SpeechRecognitionResult
            {
                Text = string.Empty,
                Confidence = 0,
                Language = "auto",
                Engine = SpeechEngineType.WhisperLocal,
                Success = false,
                ErrorMessage = ex.Message,
                IsFinal = true,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public Task<VoiceInputAssistant.Core.Models.SpeechEngineStatus> GetStatusAsync()
    {
        var status = new VoiceInputAssistant.Core.Models.SpeechEngineStatus(
            IsAvailable: _isInitialized,
            Engine: SpeechEngineType.WhisperLocal,
            EngineVersion: typeof(WhisperProcessor).Assembly.GetName().Version?.ToString(),
            RequiresNetwork: false,
            SupportedLanguages: GetSupportedLanguages(),
            SupportsInterimResults: false,
            SupportsSpeakerDiarization: false,
            StatusMessage: null,
            LastChecked: DateTime.UtcNow
        );

        return Task.FromResult(status);
    }

    /// <summary>
    /// Process real-time audio stream
    /// </summary>
    /// <param name="audioStream">Audio stream to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcription result</returns>
    public async Task<SpeechRecognitionResult> ProcessAudioStreamAsync(Stream audioStream, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _processor == null)
        {
            throw new InvalidOperationException("Whisper not initialized. Call InitializeAsync first.");
        }

        try
        {
            var segments = new List<string>();
            var totalProbability = 0f;
            var segmentCount = 0;

            await foreach (var segment in _processor.ProcessAsync(audioStream, cancellationToken))
            {
                segments.Add(segment.Text);
                totalProbability += segment.Probability;
                segmentCount++;

                // Raise event for real-time updates
                var intermediateResult = new SpeechRecognitionResult
                {
                    Text = segment.Text,
                    Confidence = segment.Probability,
                    Language = "auto",
                    Engine = SpeechEngineType.WhisperLocal,
                    Success = true,
                    IsFinal = false,
                    Timestamp = DateTimeOffset.UtcNow,
                    WordCount = segment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                };
                
                TranscriptionReceived?.Invoke(this, intermediateResult);
            }

            var fullText = string.Join(" ", segments).Trim();
            var averageConfidence = segmentCount > 0 ? totalProbability / segmentCount : 0;

            // Apply adaptive learning post-processing if available
            try
            {
                if (_adaptiveLearningService != null && !string.IsNullOrWhiteSpace(fullText))
                {
                    // Try to apply learned corrections automatically for high-confidence cases
                    var suggestions = await _adaptiveLearningService.GetContextSuggestionsAsync(
                        fullText, "general", "system");
                    
                    if (suggestions?.Any() == true)
                    {
                        // Apply the highest confidence correction
                        var bestSuggestion = suggestions
                            .OrderByDescending(s => s.ConfidenceScore)
                            .FirstOrDefault();
                            
                        if (bestSuggestion != null && bestSuggestion.ConfidenceScore > 0.9f)
                        {
                            _logger.LogDebug("Applying high-confidence adaptive learning correction: '{Original}' -> '{Corrected}'",
                                fullText, bestSuggestion.SuggestedText);
                                
                            // Apply the correction to the text
                            fullText = bestSuggestion.SuggestedText;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply adaptive learning corrections");
                // Don't fail transcription if adaptive learning fails
            }

            var finalResult = new SpeechRecognitionResult
            {
                Text = fullText,
                Confidence = averageConfidence,
                Language = "auto",
                Engine = SpeechEngineType.WhisperLocal,
                Success = true,
                IsFinal = true,
                Timestamp = DateTimeOffset.UtcNow,
                WordCount = fullText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            };

            TranscriptionReceived?.Invoke(this, finalResult);
            return finalResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio processing cancelled");
            return new SpeechRecognitionResult
            {
                Text = string.Empty,
                Confidence = 0,
                Language = "auto",
                Engine = SpeechEngineType.WhisperLocal,
                Success = false,
                ErrorMessage = "Processing cancelled",
                IsFinal = true,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Real-time audio processing failed");
            return new SpeechRecognitionResult
            {
                Text = string.Empty,
                Confidence = 0,
                Language = "auto",
                Engine = SpeechEngineType.WhisperLocal,
                Success = false,
                ErrorMessage = ex.Message,
                IsFinal = true,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Process audio file from file path
    /// </summary>
    /// <param name="audioFilePath">Path to audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcription result</returns>
    public async Task<SpeechRecognitionResult> ProcessAudioFileAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        using var fileStream = File.OpenRead(audioFilePath);
        return await ProcessAudioStreamAsync(fileStream, cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _processor?.Dispose();
            _factory?.Dispose();
        }
    }

    private static string[] GetSupportedLanguages()
    {
        // Whisper supports 99 languages: https://github.com/openai/whisper#available-models-and-languages
        return new[]
        {
            "en", "zh", "de", "es", "ru", "ko", "fr", "ja", "pt", "tr", "pl", "ca", "nl", "ar", "sv", "it", "id", "hi",
            "fi", "vi", "iw", "uk", "el", "ms", "cs", "ro", "da", "hu", "ta", "no", "th", "ur", "hr", "bg", "lt", "la",
            "mi", "ml", "cy", "sk", "te", "fa", "lv", "bn", "sr", "az", "sl", "kn", "et", "mk", "br", "eu", "is", "hy",
            "ne", "mn", "bs", "kk", "sq", "sw", "gl", "mr", "pa", "si", "km", "sn", "yo", "so", "af", "oc", "ka", "be",
            "tg", "sd", "gu", "am", "yi", "lo", "uz", "fo", "ht", "ps", "tk", "nn", "mt", "sa", "lb", "my", "bo", "tl",
            "mg", "as", "tt", "haw", "ln", "ha", "ba", "jw", "su"
        };
    }
}

public class WhisperSettings
{
    public string ModelPath { get; set; } = "whisper-model.bin";
}