using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;

#pragma warning disable CS0067 // Event is declared but never used
namespace VoiceInputAssistant.Services.SpeechEngines.Adapters
{
public class AzureSpeechAdapter : ISpeechEngineAdapter
{
    private readonly ILogger _logger;
    private readonly AzureSpeechEngine _engine;

    private bool _isInitialized;

    public event EventHandler<SpeechRecognitionEventArgs>? SpeechRecognized;
    public event EventHandler<SpeechRecognitionErrorEventArgs>? RecognitionError;
    public event EventHandler<VoiceActivityEventArgs>? VoiceActivityDetected;
    public event EventHandler<PartialRecognitionEventArgs>? PartialRecognitionReceived;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelChanged;

    public bool IsInitialized => _isInitialized;
    public bool IsListening { get; private set; }
    public SpeechEngine Engine => SpeechEngine.AzureSpeech;

    public AzureSpeechAdapter(ILogger logger)
    {
        _logger = logger;
        _engine = new AzureSpeechEngine(
            logger is ILogger<AzureSpeechEngine> engineLogger ? engineLogger 
            : LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AzureSpeechEngine>(),
            new MetricsService(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MetricsService>())
        );

        // Wire up engine events
        _engine.StatusChanged += OnEngineStatusChanged;
    }

    public async Task InitializeAsync(SpeechEngineConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Initializing Azure Speech adapter...");

            var engineConfig = new EngineConfiguration
            {
                ApiKey = config.ApiKey,
                Region = config.Region,
                DefaultQuality = config.Quality,
                EnablePartialResults = config.EnablePartialResults,
                Settings = new Dictionary<string, object>
                {
                    ["EnableProfanityFilter"] = false // Make configurable later
                }
            };

            var success = await _engine.InitializeAsync(engineConfig, cancellationToken);
            _isInitialized = success;

            _logger.LogDebug("Azure Speech adapter initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Speech adapter");
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Adapter not initialized");

        IsListening = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsListening = false;
    }

    public async Task<SpeechRecognitionResult> ProcessAudioFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var request = new SpeechRecognitionRequest
        {
            Id = Guid.NewGuid(),
            FilePath = filePath
        };

        return await _engine.RecognizeAsync(request, cancellationToken);
    }

    public async Task ProcessAudioDataAsync(byte[] audioData, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (!IsListening) return;

        try
        {
            // Copy the relevant portion of the buffer
            var buffer = new byte[count];
            Array.Copy(audioData, offset, buffer, 0, count);

            // Create recognition request
            var request = new SpeechRecognitionRequest
            {
                Id = Guid.NewGuid(),
                AudioData = buffer,
                EnablePartialResults = true,
                Language = "en-US"  // TODO: Make configurable
            };

            // Process the audio
            var result = await _engine.RecognizeAsync(request, cancellationToken);

            // Notify success
            if (result.IsSuccess)
            {
                SpeechRecognized?.Invoke(this, new SpeechRecognitionEventArgs
                {
                    Result = new SpeechRecognitionResult
                    {
                        Text = result.Text,
                        Confidence = result.Confidence,
                        Engine = Engine,
                        RequestId = result.RequestId,
                        IsSuccess = result.IsSuccess
                    }
                });
            }
            else
            {
                RecognitionError?.Invoke(this, new SpeechRecognitionErrorEventArgs
                {
                    Message = result.ErrorMessage ?? "Unknown error",
                    Engine = Engine,
                    IsRecoverable = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data in Azure Speech adapter");
            RecognitionError?.Invoke(this, new SpeechRecognitionErrorEventArgs
            {
                Message = ex.Message,
                Exception = ex,
                Engine = Engine,
                IsRecoverable = true
            });
        }
    }

    public async Task ConfigureAsync(SpeechEngineConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var engineConfig = new EngineConfiguration
            {
                ApiKey = config.ApiKey,
                Region = config.Region,
                DefaultQuality = config.Quality,
                EnablePartialResults = config.EnablePartialResults,
                Settings = new Dictionary<string, object>
                {
                    ["EnableProfanityFilter"] = false // Make configurable later
                }
            };

            await _engine.TestConfigurationAsync(engineConfig, cancellationToken);
            _engine.UpdateConfiguration(engineConfig);
            
            _logger.LogDebug("Azure Speech adapter configuration updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure Azure Speech adapter");
            throw;
        }
    }

    public async Task UpdateVocabularyAsync(IEnumerable<string> phrases, CancellationToken cancellationToken = default)
    {
        // Azure Speech doesn't support direct vocabulary updates
        await Task.CompletedTask;
    }

    public void UpdateConfiguration(SpeechEngineConfig config)
    {
        var engineConfig = new EngineConfiguration
        {
            ApiKey = config.ApiKey,
            Region = config.Region,
            DefaultQuality = config.Quality,
            EnablePartialResults = config.EnablePartialResults,
            Settings = new Dictionary<string, object>
            {
                ["EnableProfanityFilter"] = false // Make configurable later
            }
        };

        _engine.UpdateConfiguration(engineConfig);
    }

    private void OnEngineStatusChanged(object? sender, EngineStatusChangedEventArgs e)
    {
        if (!e.IsAvailable)
        {
            RecognitionError?.Invoke(this, new SpeechRecognitionErrorEventArgs
            {
                Message = e.StatusMessage ?? "Engine unavailable",
                Exception = e.Error,
                Engine = Engine,
                IsRecoverable = e.Error == null
            });
        }
    }

    public void Dispose()
    {
        _engine.Dispose();
    }
}
}
#pragma warning restore CS0067
