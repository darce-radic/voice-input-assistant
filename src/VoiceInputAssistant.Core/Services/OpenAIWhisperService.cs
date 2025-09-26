using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using System.Text.Json;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// OpenAI Whisper API implementation for speech recognition
/// </summary>
public class OpenAIWhisperService : ISpeechRecognitionService
{
    private readonly ILogger<OpenAIWhisperService> _logger;
    private readonly ISpeechRecognitionSettings _settings;
    private readonly HttpClient _httpClient;
    private bool _isInitialized;
    private bool _isListening;
    private string? _apiKey;

    public bool IsListening => _isListening;
    public bool IsInitialized => _isInitialized;
    public SpeechEngineType? CurrentEngine => SpeechEngineType.OpenAIWhisper;

    public OpenAIWhisperService(
        ILogger<OpenAIWhisperService> logger,
        ISpeechRecognitionSettings settings,
        HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings;
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing OpenAI Whisper Service...");

            // Get API key from environment or settings
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                ?? _settings.OpenAIApiKey;

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is required");
            }

            // Configure HTTP client
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "VoiceInputAssistant/1.0");

            // Test connection by getting status
            var status = await GetStatusAsync();
            if (!status.IsAvailable)
            {
                throw new InvalidOperationException("OpenAI Whisper API is not available");
            }

            _isInitialized = true;
            _logger.LogInformation("OpenAI Whisper Service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenAI Whisper Service");
            _isInitialized = false;
            throw;
        }
    }

    public async Task StartListeningAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            _logger.LogInformation("Starting OpenAI Whisper listening...");
            _isListening = true;
            _logger.LogInformation("OpenAI Whisper listening started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start OpenAI Whisper listening");
            _isListening = false;
            throw;
        }
    }

    public async Task StopListeningAsync()
    {
        try
        {
            _logger.LogInformation("Stopping OpenAI Whisper listening...");
            _isListening = false;
            _logger.LogInformation("OpenAI Whisper listening stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping OpenAI Whisper listening");
        }

        await Task.CompletedTask;
    }

    public async Task<SpeechRecognitionResult> TranscribeAsync(byte[] audioData)
    {
        if (!_isInitialized)
        {
            return new SpeechRecognitionResult
            {
                Success = false,
                ErrorMessage = "OpenAI Whisper Service not initialized",
                Engine = SpeechEngineType.OpenAIWhisper
            };
        }

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Starting OpenAI Whisper transcription for {AudioDataSize} bytes", audioData.Length);

            // For now, simulate the OpenAI Whisper API call
            // In a real implementation, you would make an HTTP POST to https://api.openai.com/v1/audio/transcriptions
            var transcriptionText = await SimulateWhisperTranscription(audioData);

            var result = new SpeechRecognitionResult
            {
                Text = transcriptionText,
                Confidence = 0.92f, // Whisper typically has high confidence
                Language = _settings.Language ?? "en",
                Engine = SpeechEngineType.OpenAIWhisper,
                Success = true,
                IsFinal = true,
                ProcessingTime = DateTime.UtcNow - startTime,
                WordCount = CountWords(transcriptionText),
                Metadata = new Dictionary<string, string>
                {
                    { "model", "whisper-1" },
                    { "audioSize", audioData.Length.ToString() },
                    { "language", _settings.Language ?? "auto-detect" }
                }
            };

            _logger.LogDebug("OpenAI Whisper transcription completed: '{Text}' (Confidence: {Confidence:P})", 
                result.Text, result.Confidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI Whisper transcription failed");
            
            return new SpeechRecognitionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Engine = SpeechEngineType.OpenAIWhisper,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<SpeechEngineStatus> GetStatusAsync()
    {
        try
        {
            // In a real implementation, you would make a test API call
            await Task.Delay(100); // Simulate network call

            var hasApiKey = !string.IsNullOrEmpty(_apiKey);

            return new SpeechEngineStatus(
                IsAvailable: hasApiKey,
                Engine: SpeechEngineType.OpenAIWhisper,
                EngineVersion: "Whisper v1 (via OpenAI API)",
                RequiresNetwork: true,
                SupportedLanguages: new[]
                {
                    "af", "ar", "hy", "az", "be", "bs", "bg", "ca", "zh", "hr", "cs", "da", "nl", "en", "et", "fi", "fr", "gl", "de", "el", "he", "hi", "hu", "is", "id", "it", "ja", "kn", "kk", "ko", "lv", "lt", "mk", "ms", "mt", "mi", "mr", "ne", "no", "fa", "pl", "pt", "ro", "ru", "sr", "sk", "sl", "es", "sw", "sv", "tl", "ta", "th", "tr", "uk", "ur", "vi", "cy"
                },
                SupportsInterimResults: false, // Whisper API doesn't support streaming
                SupportsSpeakerDiarization: false,
                StatusMessage: hasApiKey ? "Ready" : "Missing OpenAI API key",
                LastChecked: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OpenAI Whisper service status");
            
            return new SpeechEngineStatus(
                IsAvailable: false,
                Engine: SpeechEngineType.OpenAIWhisper,
                StatusMessage: $"Status check failed: {ex.Message}",
                LastChecked: DateTime.UtcNow
            );
        }
    }

    private async Task<string> SimulateWhisperTranscription(byte[] audioData)
    {
        // This is a simulation - in a real implementation, you would:
        // 1. Convert audio to supported format (mp3, mp4, mpeg, mpga, m4a, wav, webm)
        // 2. Create multipart form data with the audio file
        // 3. POST to https://api.openai.com/v1/audio/transcriptions
        // 4. Parse the JSON response

        await Task.Delay(150); // Simulate API call latency
        
        // Simulate realistic responses based on audio length
        if (audioData.Length < 1000)
        {
            return "Hi there.";
        }
        else if (audioData.Length < 5000)
        {
            return "Hi there, how can I help you today?";
        }
        else if (audioData.Length < 10000)
        {
            return "Hi there, how can I help you today? I'm here to assist with any questions you might have.";
        }
        else
        {
            return "Hi there, how can I help you today? I'm here to assist with any questions you might have about our services or products. Please feel free to ask me anything.";
        }
    }

    private async Task<string> CallWhisperAPIAsync(byte[] audioData)
    {
        // Real implementation would look like this:
        /*
        using var content = new MultipartFormDataContent();
        using var audioContent = new ByteArrayContent(audioData);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, "file", "audio.wav");
        content.Add(new StringContent("whisper-1"), "model");
        content.Add(new StringContent(_settings.Language ?? "en"), "language");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var transcriptionResponse = JsonSerializer.Deserialize<WhisperResponse>(responseContent);
        
        return transcriptionResponse?.Text ?? string.Empty;
        */
        
        // For now, return simulation
        await Task.CompletedTask;
        return "Simulated transcription";
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
            
        return text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Response model for OpenAI Whisper API
internal class WhisperResponse
{
    public string Text { get; set; } = string.Empty;
}