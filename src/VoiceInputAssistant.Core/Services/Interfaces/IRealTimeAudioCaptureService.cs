using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Interface for real-time audio capture and processing service
/// </summary>
public interface IRealTimeAudioCaptureService
{
    /// <summary>
    /// Event fired when a new audio chunk is received from the microphone
    /// </summary>
    event EventHandler<AudioChunkEventArgs>? AudioChunkReceived;
    
    /// <summary>
    /// Event fired when voice activity is detected or voice activity stops
    /// </summary>
    event EventHandler<VoiceActivityEventArgs>? VoiceActivityDetected;
    
    /// <summary>
    /// Event fired when a complete speech segment is identified
    /// </summary>
    event EventHandler<SpeechSegmentEventArgs>? SpeechSegmentCompleted;
    
    /// <summary>
    /// Event fired when an audio processing error occurs
    /// </summary>
    event EventHandler<AudioErrorEventArgs>? AudioError;
    
    /// <summary>
    /// Gets whether audio capture is currently running
    /// </summary>
    bool IsCapturing { get; }
    
    /// <summary>
    /// Starts real-time audio capture with the specified settings
    /// </summary>
    /// <param name="settings">Audio capture and processing settings</param>
    /// <returns>True if capture started successfully, false otherwise</returns>
    Task<bool> StartCaptureAsync(AudioSettings settings);
    
    /// <summary>
    /// Stops real-time audio capture
    /// </summary>
    Task StopCaptureAsync();
    
    /// <summary>
    /// Gets a list of available audio input devices
    /// </summary>
    /// <returns>Collection of available audio input devices</returns>
    Task<IEnumerable<AudioDevice>> GetAvailableInputDevicesAsync();
    
    /// <summary>
    /// Gets the current real-time audio input level (0.0 to 1.0)
    /// </summary>
    /// <returns>Current audio input level</returns>
    Task<float> GetCurrentAudioLevelAsync();
    
    /// <summary>
    /// Calibrates the voice activity detector using current background noise
    /// Must be called while capture is running
    /// </summary>
    Task CalibrateBackgroundNoiseAsync();
}