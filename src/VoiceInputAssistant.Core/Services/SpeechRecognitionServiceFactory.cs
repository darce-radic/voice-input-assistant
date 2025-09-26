using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// Factory service for managing multiple speech recognition engines
/// </summary>
public class SpeechRecognitionServiceFactory : ISpeechRecognitionServiceFactory
{
    private readonly ILogger<SpeechRecognitionServiceFactory> _logger;
    private readonly Dictionary<SpeechEngineType, ISpeechRecognitionService> _services;
    private readonly ISpeechRecognitionSettings _settings;
    private ISpeechRecognitionService? _currentService;
    private SpeechEngineType _currentEngineType = SpeechEngineType.Unspecified;

    public SpeechEngineType CurrentEngineType => _currentEngineType;
    public ISpeechRecognitionService? CurrentService => _currentService;
    public bool IsInitialized => _currentService?.IsInitialized ?? false;
    public bool IsListening => _currentService?.IsListening ?? false;
    public SpeechEngineType? CurrentEngine => _currentEngineType != SpeechEngineType.Unspecified ? _currentEngineType : null;

    public SpeechRecognitionServiceFactory(
        ILogger<SpeechRecognitionServiceFactory> logger,
        ISpeechRecognitionSettings settings,
        AzureSpeechRecognitionService azureService,
        OpenAIWhisperService openAIService,
        LocalWhisperService localWhisperService)
    {
        _logger = logger;
        _settings = settings;
        
        _services = new Dictionary<SpeechEngineType, ISpeechRecognitionService>
        {
            { SpeechEngineType.AzureSpeech, azureService },
            { SpeechEngineType.OpenAIWhisper, openAIService },
            { SpeechEngineType.WhisperLocal, localWhisperService }
        };
    }

    public async Task InitializeAsync(SpeechEngineType? engineType = null)
    {
        var targetEngineType = engineType ?? _settings.PreferredEngine ?? SpeechEngineType.WhisperLocal;
        
        try
        {
            _logger.LogInformation("Initializing Speech Recognition Factory with engine: {EngineType}", targetEngineType);

            // If we're already using the requested engine and it's initialized, nothing to do
            if (_currentEngineType == targetEngineType && _currentService?.IsInitialized == true)
            {
                _logger.LogInformation("Engine {EngineType} already initialized", targetEngineType);
                return;
            }

            // Stop current service if running
            if (_currentService?.IsListening == true)
            {
                await _currentService.StopListeningAsync();
            }

            // Get the requested service
            if (!_services.TryGetValue(targetEngineType, out var service))
            {
                throw new ArgumentException($"Unsupported engine type: {targetEngineType}");
            }

            // Try to initialize the requested engine
            try
            {
                await service.InitializeAsync();
                _currentService = service;
                _currentEngineType = targetEngineType;
                
                _logger.LogInformation("Successfully initialized Speech Recognition engine: {EngineType}", targetEngineType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize preferred engine {EngineType}, trying fallback", targetEngineType);
                
                // Try fallback engines
                var fallbackEngine = await TryFallbackEnginesAsync(targetEngineType);
                if (fallbackEngine != null)
                {
                    _currentService = fallbackEngine.Value.service;
                    _currentEngineType = fallbackEngine.Value.engineType;
                    _logger.LogInformation("Successfully initialized fallback engine: {EngineType}", fallbackEngine.Value.engineType);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to initialize any speech recognition engine. Last error: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Speech Recognition Factory");
            throw;
        }
    }

    public async Task SwitchEngineAsync(SpeechEngineType engineType)
    {
        _logger.LogInformation("Switching to speech recognition engine: {EngineType}", engineType);
        
        var wasListening = _currentService?.IsListening == true;
        
        // Stop current service if listening
        if (wasListening)
        {
            await StopListeningAsync();
        }

        // Initialize new engine
        await InitializeAsync(engineType);

        // Resume listening if we were listening before
        if (wasListening && _currentService != null)
        {
            await _currentService.StartListeningAsync();
        }
    }

    public async Task<IEnumerable<SpeechEngineStatus>> GetAllEngineStatusAsync()
    {
        var statusTasks = _services.Select(async kvp =>
        {
            try
            {
                return await kvp.Value.GetStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for engine {EngineType}", kvp.Key);
                return new SpeechEngineStatus(
                    IsAvailable: false,
                    Engine: kvp.Key,
                    StatusMessage: $"Status check failed: {ex.Message}",
                    LastChecked: DateTime.UtcNow
                );
            }
        });

        return await Task.WhenAll(statusTasks);
    }

    public async Task<SpeechEngineType> GetBestAvailableEngineAsync()
    {
        var statuses = await GetAllEngineStatusAsync();
        
        // Priority order: Local Whisper -> OpenAI Whisper -> Azure Speech
        var priorityOrder = new[]
        {
            SpeechEngineType.WhisperLocal,
            SpeechEngineType.OpenAIWhisper,
            SpeechEngineType.AzureSpeech
        };

        foreach (var engineType in priorityOrder)
        {
            var status = statuses.FirstOrDefault(s => s.Engine == engineType);
            if (status?.IsAvailable == true)
            {
                return engineType;
            }
        }

        // If none are available, return the first available one
        var availableEngine = statuses.FirstOrDefault(s => s.IsAvailable);
        return availableEngine?.Engine ?? SpeechEngineType.WhisperLocal;
    }

    // Delegate methods to current service
    async Task ISpeechRecognitionService.InitializeAsync()
    {
        await InitializeAsync();
    }
    
    public async Task StartListeningAsync()
    {
        if (_currentService == null)
        {
            await InitializeAsync();
        }
        
        if (_currentService != null)
        {
            await _currentService.StartListeningAsync();
        }
        else
        {
            throw new InvalidOperationException("No speech recognition service available");
        }
    }

    public async Task StopListeningAsync()
    {
        if (_currentService != null)
        {
            await _currentService.StopListeningAsync();
        }
    }

    public async Task<SpeechRecognitionResult> TranscribeAsync(byte[] audioData)
    {
        if (_currentService == null)
        {
            return new SpeechRecognitionResult
            {
                Success = false,
                ErrorMessage = "No speech recognition service available",
                Engine = SpeechEngineType.Unspecified
            };
        }

        return await _currentService.TranscribeAsync(audioData);
    }

    public async Task<SpeechEngineStatus> GetStatusAsync()
    {
        if (_currentService == null)
        {
            return new SpeechEngineStatus(
                IsAvailable: false,
                Engine: SpeechEngineType.Unspecified,
                StatusMessage: "No engine selected",
                LastChecked: DateTime.UtcNow
            );
        }

        return await _currentService.GetStatusAsync();
    }

    private async Task<(ISpeechRecognitionService service, SpeechEngineType engineType)?> TryFallbackEnginesAsync(SpeechEngineType excludeEngine)
    {
        var fallbackOrder = new[]
        {
            SpeechEngineType.WhisperLocal,
            SpeechEngineType.OpenAIWhisper,
            SpeechEngineType.AzureSpeech
        }.Where(e => e != excludeEngine);

        foreach (var engineType in fallbackOrder)
        {
            if (!_services.TryGetValue(engineType, out var service))
                continue;

            try
            {
                await service.InitializeAsync();
                _logger.LogInformation("Successfully initialized fallback engine: {EngineType}", engineType);
                return (service, engineType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback engine {EngineType} also failed to initialize", engineType);
            }
        }

        return null;
    }
}

/// <summary>
/// Interface for the Speech Recognition Service Factory
/// </summary>
public interface ISpeechRecognitionServiceFactory : ISpeechRecognitionService
{
    /// <summary>
    /// Gets the currently active engine type
    /// </summary>
    SpeechEngineType CurrentEngineType { get; }

    /// <summary>
    /// Gets the currently active service instance
    /// </summary>
    ISpeechRecognitionService? CurrentService { get; }

    /// <summary>
    /// Initializes the factory with a specific engine type
    /// </summary>
    Task InitializeAsync(SpeechEngineType? engineType = null);

    /// <summary>
    /// Switches to a different speech recognition engine
    /// </summary>
    Task SwitchEngineAsync(SpeechEngineType engineType);

    /// <summary>
    /// Gets the status of all available engines
    /// </summary>
    Task<IEnumerable<SpeechEngineStatus>> GetAllEngineStatusAsync();

    /// <summary>
    /// Determines the best available engine based on availability and priority
    /// </summary>
    Task<SpeechEngineType> GetBestAvailableEngineAsync();
}