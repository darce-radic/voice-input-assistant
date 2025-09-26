using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;
using VoiceInputAssistant.Interfaces;
using ModelEngine = VoiceInputAssistant.Models.SpeechEngine;
using ModelResult = VoiceInputAssistant.Models.SpeechRecognitionResult;
using VoiceInputAssistant.Services.SpeechEngines;
using VoiceInputAssistant.Services.SpeechEngines.Adapters;

namespace VoiceInputAssistant.Services;

public class SpeechRecognitionService : ISpeechRecognitionService
{
    private readonly ILogger<SpeechRecognitionService> _logger;
    private readonly SpeechSettings _settings;
    private readonly Dictionary<ModelEngine, ISpeechEngineAdapter> _engines;
    private WaveInEvent? _waveIn;
    private ISpeechEngineAdapter? _currentEngineAdapter;
    private bool _isListening;
    private bool _voiceActivityDetected;
    private bool _isInitialized;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public SpeechRecognitionService(
        ILogger<SpeechRecognitionService> logger,
        IOptions<SpeechSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _engines = new Dictionary<ModelEngine, ISpeechEngineAdapter>();
        _cancellationTokenSource = new CancellationTokenSource();
        
        InitializeEngines();
    }

    public IEnumerable<ModelEngine> AvailableEngines => _engines.Keys;

    public ModelEngine CurrentEngine { get; private set; } = ModelEngine.WhisperLocal;

    public bool IsListening => _isListening;

    public bool VoiceActivityDetected => _voiceActivityDetected;

    public bool IsInitialized => _isInitialized;

    public event EventHandler<SpeechRecognitionEventArgs>? InterimResultReceived;
    public event EventHandler<SpeechRecognitionEventArgs>? FinalResultReceived;
    public event EventHandler<SpeechRecognitionErrorEventArgs>? ErrorOccurred;
    public event EventHandler<VoiceActivityEventArgs>? VoiceActivityChanged;

    public async Task InitializeAsync(ModelEngine engine, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing speech recognition with engine: {Engine}", engine);

            if (!_engines.ContainsKey(engine))
            {
                throw new NotSupportedException($"Speech engine {engine} is not available");
            }

            _currentEngineAdapter = _engines[engine];
            CurrentEngine = engine;

            // Create configuration for the engine
            var config = new SpeechEngineConfig
            {
                Engine = engine,
                Language = _settings.DefaultLanguage,
                Quality = RecognitionQuality.Balanced,
                PostProcessing = PostProcessingMode.BasicCorrection,
                ActivationMode = VoiceActivationMode.PushToTalk,
                EnablePartialResults = _settings.EnableInterimResults,
                EnableVoiceActivityDetection = _settings.EnableVoiceActivityDetection,
                SampleRate = _settings.AudioSampleRate,
                Channels = _settings.AudioChannels
            };

            await _currentEngineAdapter.InitializeAsync(config, cancellationToken);
            _isInitialized = true;

            _logger.LogInformation("Speech recognition initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize speech recognition with engine: {Engine}", engine);
            OnErrorOccurred(new SpeechRecognitionErrorEventArgs
            {
                Message = $"Failed to initialize speech engine: {ex.Message}",
                Exception = ex,
                Engine = engine,
                IsRecoverable = false
            });
            throw;
        }
    }

    public async Task SwitchEngineAsync(ModelEngine engine, CancellationToken cancellationToken = default)
    {
        if (CurrentEngine == engine)
        {
            _logger.LogDebug("Engine {Engine} is already active", engine);
            return;
        }

        var wasListening = _isListening;
        
        if (wasListening)
        {
            await StopListeningAsync(cancellationToken);
        }

        await InitializeAsync(engine, cancellationToken);

        if (wasListening)
        {
            await StartListeningAsync(cancellationToken);
        }

        _logger.LogInformation("Switched to speech engine: {Engine}", engine);
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_isListening)
            {
                _logger.LogWarning("Speech recognition is already listening");
                return;
            }

            if (_currentEngineAdapter == null)
            {
                throw new InvalidOperationException("Speech engine not initialized");
            }

            _logger.LogInformation("Starting speech recognition");

            // Initialize audio capture
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(_settings.AudioSampleRate, _settings.AudioChannels)
            };

            _waveIn.DataAvailable += OnAudioDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            // Start the current engine
            await _currentEngineAdapter.StartAsync(cancellationToken);
            
            // Start audio capture
            _waveIn.StartRecording();
            
            _isListening = true;
            
            _logger.LogInformation("Speech recognition started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start speech recognition");
            OnErrorOccurred(new SpeechRecognitionErrorEventArgs
            {
                Message = $"Failed to start speech recognition: {ex.Message}",
                Exception = ex,
                Engine = CurrentEngine,
                IsRecoverable = true
            });
            throw;
        }
    }

    public async Task StopListeningAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isListening)
            {
                _logger.LogWarning("Speech recognition is not listening");
                return;
            }

            _logger.LogInformation("Stopping speech recognition");

            _isListening = false;

            // Stop audio capture
            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _waveIn = null;

            // Stop the current engine
            if (_currentEngineAdapter != null)
            {
                await _currentEngineAdapter.StopAsync(cancellationToken);
            }

            _logger.LogInformation("Speech recognition stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop speech recognition");
            OnErrorOccurred(new SpeechRecognitionErrorEventArgs
            {
                Message = $"Failed to stop speech recognition: {ex.Message}",
                Exception = ex,
                Engine = CurrentEngine,
                IsRecoverable = true
            });
            throw;
        }
    }

    public async Task<ModelResult> ProcessAudioFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentEngineAdapter == null)
            {
                throw new InvalidOperationException("Speech engine not initialized");
            }

            _logger.LogInformation("Processing audio file: {FilePath}", filePath);

            var result = await _currentEngineAdapter.ProcessAudioFileAsync(filePath, cancellationToken);

            _logger.LogDebug("Audio file processed. Text: {Text}, Confidence: {Confidence}", 
                result.Text, result.Confidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process audio file: {FilePath}", filePath);
            OnErrorOccurred(new SpeechRecognitionErrorEventArgs
            {
                Message = $"Failed to process audio file: {ex.Message}",
                Exception = ex,
                Engine = CurrentEngine,
                IsRecoverable = true
            });
            throw;
        }
    }

    public async Task ConfigureEngineAsync(SpeechEngineConfig config, CancellationToken cancellationToken = default)
    {
        if (_engines.TryGetValue(config.Engine, out var adapter))
        {
            await adapter.ConfigureAsync(config, cancellationToken);
            _logger.LogInformation("Engine {Engine} configured successfully", config.Engine);
        }
        else
        {
            _logger.LogWarning("Engine {Engine} not found for configuration", config.Engine);
        }
    }

    public async Task UpdateVocabularyAsync(IEnumerable<string> phrases, CancellationToken cancellationToken = default)
    {
        if (_currentEngineAdapter != null)
        {
            await _currentEngineAdapter.UpdateVocabularyAsync(phrases, cancellationToken);
            _logger.LogDebug("Vocabulary updated with {Count} phrases", phrases.Count());
        }
    }

    public float GetConfidenceThreshold()
    {
        return _settings.ConfidenceThreshold;
    }

    public void SetConfidenceThreshold(float threshold)
    {
        // Update settings and notify current engine
        // Note: In a full implementation, this would update the configuration
        _logger.LogDebug("Confidence threshold set to: {Threshold}", threshold);
    }

    private void InitializeEngines()
    {
        try
        {
            // Initialize available speech engines
            _engines[SpeechEngine.WhisperLocal] = new WhisperLocalAdapter(_logger);
            _engines[SpeechEngine.WindowsSpeech] = new WindowsSpeechAdapter(_logger);
            
            // Add cloud engines if configured (using basic logger for now)
            _engines[SpeechEngine.AzureSpeech] = new AzureSpeechAdapter(_logger);
            
            // Note: OpenAIWhisperAdapter not implemented yet
            // _engines[SpeechEngine.OpenAIWhisper] = new OpenAIWhisperAdapter(_logger);

            _logger.LogInformation("Initialized {Count} speech engines", _engines.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize speech engines");
            throw;
        }
    }

    private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            if (_currentEngineAdapter != null && _isListening)
            {
                // Process audio data through the current engine
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _currentEngineAdapter.ProcessAudioDataAsync(e.Buffer, 0, e.BytesRecorded);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing audio data");
                        OnErrorOccurred(new SpeechRecognitionErrorEventArgs
                        {
                            Message = "Error processing audio data",
                            Exception = ex,
                            Engine = CurrentEngine,
                            IsRecoverable = true
                        });
                    }
                });

                // Basic voice activity detection
                DetectVoiceActivity(e.Buffer, e.BytesRecorded);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audio data handler");
        }
    }

    private void DetectVoiceActivity(byte[] buffer, int bytesRecorded)
    {
        // Simple energy-based VAD
        double energy = 0;
        for (int i = 0; i < bytesRecorded; i += 2)
        {
            if (i + 1 < bytesRecorded)
            {
                short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                energy += sample * sample;
            }
        }

        energy = Math.Sqrt(energy / (bytesRecorded / 2));
        var isActive = energy > 1000; // Threshold for voice activity

        if (_voiceActivityDetected != isActive)
        {
            _voiceActivityDetected = isActive;
            OnVoiceActivityChanged(new VoiceActivityEventArgs
            {
                HasVoice = isActive,
                EnergyLevel = energy / 32768.0, // Normalize to 0-1
                Confidence = isActive ? 0.8 : 0.2,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            _logger.LogError(e.Exception, "Audio recording stopped with exception");
            OnErrorOccurred(new SpeechRecognitionErrorEventArgs
            {
                Message = "Audio recording error",
                Exception = e.Exception,
                Engine = CurrentEngine,
                IsRecoverable = true
            });
        }
    }

    protected virtual void OnInterimResultReceived(SpeechRecognitionEventArgs e)
    {
        InterimResultReceived?.Invoke(this, e);
    }

    protected virtual void OnFinalResultReceived(SpeechRecognitionEventArgs e)
    {
        FinalResultReceived?.Invoke(this, e);
    }

    protected virtual void OnErrorOccurred(SpeechRecognitionErrorEventArgs e)
    {
        ErrorOccurred?.Invoke(this, e);
    }

    protected virtual void OnVoiceActivityChanged(VoiceActivityEventArgs e)
    {
        VoiceActivityChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        
        _waveIn?.Dispose();
        
        foreach (var engine in _engines.Values)
        {
            if (engine is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        _cancellationTokenSource.Dispose();
        
        _logger.LogInformation("SpeechRecognitionService disposed");
    }
}