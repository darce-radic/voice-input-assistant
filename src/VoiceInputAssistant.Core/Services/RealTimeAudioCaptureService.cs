using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Collections.Concurrent;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// Real-time audio capture service for continuous microphone input processing
/// </summary>
public class RealTimeAudioCaptureService : IRealTimeAudioCaptureService, IDisposable
{
    private readonly ILogger<RealTimeAudioCaptureService> _logger;
    private readonly IAudioProcessor _audioProcessor;
    private readonly IVoiceActivityDetector _voiceActivityDetector;
    
    private WaveInEvent? _waveIn;
    private readonly ConcurrentQueue<AudioChunk> _audioQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _processingTask;
    
    private bool _isCapturing;
    private bool _isDisposed;
    private AudioSettings? _currentSettings;

    // Configuration
    private const int DefaultSampleRate = 16000;
    private const int DefaultBitsPerSample = 16;
    private const int DefaultChannels = 1;
    private const int DefaultBufferDuration = 50; // 50ms chunks

    public RealTimeAudioCaptureService(
        ILogger<RealTimeAudioCaptureService> logger,
        IAudioProcessor audioProcessor,
        IVoiceActivityDetector voiceActivityDetector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioProcessor = audioProcessor ?? throw new ArgumentNullException(nameof(audioProcessor));
        _voiceActivityDetector = voiceActivityDetector ?? throw new ArgumentNullException(nameof(voiceActivityDetector));
        
        _audioQueue = new ConcurrentQueue<AudioChunk>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public event EventHandler<AudioChunkEventArgs>? AudioChunkReceived;
    public event EventHandler<VoiceActivityEventArgs>? VoiceActivityDetected;
    public event EventHandler<SpeechSegmentEventArgs>? SpeechSegmentCompleted;
    public event EventHandler<AudioErrorEventArgs>? AudioError;

    public bool IsCapturing => _isCapturing && !_isDisposed;

    public async Task<bool> StartCaptureAsync(AudioSettings settings)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(RealTimeAudioCaptureService));

        if (_isCapturing)
        {
            _logger.LogWarning("Audio capture is already running");
            return false;
        }

        try
        {
            _logger.LogInformation("Starting real-time audio capture...");
            
            // Initialize audio processor and VAD
            await _audioProcessor.InitializeAsync(settings);
            await _voiceActivityDetector.InitializeAsync();
            
            _currentSettings = settings;
            
            // Set up audio capture
            await SetupAudioCaptureAsync(settings);
            
            // Start processing task
            _processingTask = Task.Run(() => ProcessAudioQueueAsync(_cancellationTokenSource.Token));
            
            // Start audio capture
            _waveIn?.StartRecording();
            _isCapturing = true;
            
            _logger.LogInformation("Real-time audio capture started successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start real-time audio capture");
            await StopCaptureAsync();
            return false;
        }
    }

    public async Task StopCaptureAsync()
    {
        if (!_isCapturing)
            return;

        try
        {
            _logger.LogInformation("Stopping real-time audio capture...");
            
            _isCapturing = false;
            
            // Stop audio capture
            _waveIn?.StopRecording();
            
            // Cancel processing
            _cancellationTokenSource.Cancel();
            
            // Wait for processing to complete
            if (_processingTask != null)
            {
                await _processingTask;
                _processingTask = null;
            }
            
            _logger.LogInformation("Real-time audio capture stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping real-time audio capture");
        }
    }

    public async Task<IEnumerable<AudioDevice>> GetAvailableInputDevicesAsync()
    {
        try
        {
            return await _audioProcessor.GetAudioInputDevicesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate audio input devices");
            return Enumerable.Empty<AudioDevice>();
        }
    }

    public async Task<float> GetCurrentAudioLevelAsync()
    {
        try
        {
            return await _audioProcessor.GetAudioInputLevelAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current audio level");
            return 0f;
        }
    }

    public async Task CalibrateBackgroundNoiseAsync()
    {
        if (!_isCapturing)
        {
            _logger.LogWarning("Cannot calibrate background noise - capture not running");
            return;
        }

        try
        {
            _logger.LogInformation("Starting background noise calibration...");
            
            // Collect background noise samples for 2 seconds
            var backgroundSamples = new List<byte[]>();
            var startTime = DateTime.UtcNow;
            var calibrationDuration = TimeSpan.FromSeconds(2);
            
            while (DateTime.UtcNow - startTime < calibrationDuration)
            {
                if (_audioQueue.TryDequeue(out var chunk))
                {
                    backgroundSamples.Add(chunk.Data);
                }
                await Task.Delay(10); // Small delay
            }
            
            if (backgroundSamples.Count > 0)
            {
                var combinedBackground = CombineAudioChunks(backgroundSamples);
                await _voiceActivityDetector.CalibrateAsync(combinedBackground);
                _logger.LogInformation("Background noise calibration completed");
            }
            else
            {
                _logger.LogWarning("No background noise samples collected for calibration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calibrate background noise");
        }
    }

    private async Task SetupAudioCaptureAsync(AudioSettings settings)
    {
        try
        {
            // Dispose existing capture device
            _waveIn?.Dispose();
            
            // Create new capture device
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(DefaultSampleRate, DefaultBitsPerSample, DefaultChannels),
                BufferMilliseconds = DefaultBufferDuration
            };

            // Set device if specified
            if (!string.IsNullOrEmpty(settings.PreferredInputDevice))
            {
                var deviceCount = WaveIn.DeviceCount;
                for (int i = 0; i < deviceCount; i++)
                {
                    var capabilities = WaveIn.GetCapabilities(i);
                    if (capabilities.ProductName.Contains(settings.PreferredInputDevice, StringComparison.OrdinalIgnoreCase))
                    {
                        _waveIn.DeviceNumber = i;
                        break;
                    }
                }
            }

            // Set up event handlers
            _waveIn.DataAvailable += OnAudioDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            
            _logger.LogDebug("Audio capture device configured: {DeviceNumber}, Format: {WaveFormat}", 
                _waveIn.DeviceNumber, _waveIn.WaveFormat);
                
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup audio capture device");
            throw;
        }
    }

    private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            if (e.BytesRecorded > 0 && _isCapturing)
            {
                var audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, 0, audioData, 0, e.BytesRecorded);
                
                var chunk = new AudioChunk
                {
                    Data = audioData,
                    Timestamp = DateTime.UtcNow,
                    SampleRate = DefaultSampleRate,
                    BitsPerSample = DefaultBitsPerSample,
                    Channels = DefaultChannels,
                    DurationMs = (audioData.Length * 1000) / (DefaultSampleRate * DefaultChannels * (DefaultBitsPerSample / 8))
                };
                
                _audioQueue.Enqueue(chunk);
                AudioChunkReceived?.Invoke(this, new AudioChunkEventArgs(chunk));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data");
            AudioError?.Invoke(this, new AudioErrorEventArgs(ex, "Error processing audio data"));
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            _logger.LogError(e.Exception, "Audio recording stopped due to error");
            AudioError?.Invoke(this, new AudioErrorEventArgs(e.Exception, "Audio recording stopped due to error"));
        }
        else
        {
            _logger.LogDebug("Audio recording stopped normally");
        }
    }

    private async Task ProcessAudioQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting audio processing queue...");
        
        var speechBuffer = new List<AudioChunk>();
        var lastVoiceActivity = false;
        var speechStartTime = DateTime.MinValue;
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_audioQueue.TryDequeue(out var chunk))
                {
                    try
                    {
                        // Preprocess audio
                        var preprocessedData = await _audioProcessor.PreprocessAudioAsync(chunk.Data, _currentSettings!);
                        
                        // Detect voice activity
                        var hasVoiceActivity = await _voiceActivityDetector.IsSpeechDetectedAsync(preprocessedData);
                        var energyLevel = await _voiceActivityDetector.GetAudioEnergyLevelAsync(preprocessedData);
                        
                        // Emit voice activity event
                        VoiceActivityDetected?.Invoke(this, new VoiceActivityEventArgs(hasVoiceActivity, energyLevel, chunk.Timestamp));
                        
                        // Handle speech segmentation
                        if (hasVoiceActivity && !lastVoiceActivity)
                        {
                            // Speech started
                            speechStartTime = chunk.Timestamp;
                            speechBuffer.Clear();
                            _logger.LogDebug("Speech segment started at {Timestamp}", speechStartTime);
                        }
                        
                        if (hasVoiceActivity)
                        {
                            speechBuffer.Add(chunk);
                        }
                        else if (lastVoiceActivity && speechBuffer.Count > 0)
                        {
                            // Speech ended - emit completed segment
                            var speechEndTime = chunk.Timestamp;
                            var combinedAudio = CombineAudioChunks(speechBuffer.Select(c => c.Data));
                            var duration = speechEndTime - speechStartTime;
                            
                            var segment = new SpeechSegment
                            {
                                StartTime = TimeSpan.FromTicks(speechStartTime.Ticks),
                                EndTime = TimeSpan.FromTicks(speechEndTime.Ticks),
                                Confidence = energyLevel,
                                AverageEnergyLevel = energyLevel,
                                AudioDataStartIndex = 0,
                                AudioDataEndIndex = combinedAudio.Length
                            };
                            
                            SpeechSegmentCompleted?.Invoke(this, new SpeechSegmentEventArgs(segment, combinedAudio));
                            
                            _logger.LogDebug("Speech segment completed: Duration={Duration:F2}s, Energy={Energy:F3}", 
                                duration.TotalSeconds, energyLevel);
                            
                            speechBuffer.Clear();
                        }
                        
                        lastVoiceActivity = hasVoiceActivity;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing audio chunk");
                        AudioError?.Invoke(this, new AudioErrorEventArgs(ex, "Error processing audio chunk"));
                    }
                }
                else
                {
                    // No audio chunks available, wait briefly
                    await Task.Delay(5, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Audio processing queue cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in audio processing queue");
        }
        
        _logger.LogDebug("Audio processing queue stopped");
    }

    private static byte[] CombineAudioChunks(IEnumerable<byte[]> chunks)
    {
        var totalLength = chunks.Sum(chunk => chunk.Length);
        var combined = new byte[totalLength];
        var offset = 0;
        
        foreach (var chunk in chunks)
        {
            Array.Copy(chunk, 0, combined, offset, chunk.Length);
            offset += chunk.Length;
        }
        
        return combined;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
            
        _logger.LogDebug("Disposing RealTimeAudioCaptureService");
        
        try
        {
            StopCaptureAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }
        
        _waveIn?.Dispose();
        _cancellationTokenSource.Dispose();
        
        _isDisposed = true;
    }
}

#region Event Args Classes

public class AudioChunkEventArgs : EventArgs
{
    public AudioChunk Chunk { get; }
    
    public AudioChunkEventArgs(AudioChunk chunk)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
    }
}

public class VoiceActivityEventArgs : EventArgs
{
    public bool HasVoiceActivity { get; }
    public float EnergyLevel { get; }
    public DateTime Timestamp { get; }
    
    public VoiceActivityEventArgs(bool hasVoiceActivity, float energyLevel, DateTime timestamp)
    {
        HasVoiceActivity = hasVoiceActivity;
        EnergyLevel = energyLevel;
        Timestamp = timestamp;
    }
}

public class SpeechSegmentEventArgs : EventArgs
{
    public SpeechSegment Segment { get; }
    public byte[] AudioData { get; }
    
    public SpeechSegmentEventArgs(SpeechSegment segment, byte[] audioData)
    {
        Segment = segment ?? throw new ArgumentNullException(nameof(segment));
        AudioData = audioData ?? throw new ArgumentNullException(nameof(audioData));
    }
}

public class AudioErrorEventArgs : EventArgs
{
    public Exception Exception { get; }
    public string Message { get; }
    
    public AudioErrorEventArgs(Exception exception, string message)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }
}

#endregion

#region Models

public class AudioChunk
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; }
    public int SampleRate { get; set; }
    public int BitsPerSample { get; set; }
    public int Channels { get; set; }
    public int DurationMs { get; set; }
}

#endregion
