using System;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for audio processing functionality
/// </summary>
public interface IAudioProcessingService
{
    /// <summary>
    /// Event fired when voice activity is detected
    /// </summary>
    event EventHandler<VoiceActivityEventArgs> VoiceActivityDetected;
    
    /// <summary>
    /// Event fired when audio level changes
    /// </summary>
    event EventHandler<AudioLevelEventArgs> AudioLevelChanged;
    
    /// <summary>
    /// Process raw audio data for voice activity detection
    /// </summary>
    /// <param name="audioData">Raw audio data</param>
    /// <param name="sampleRate">Audio sample rate</param>
    /// <returns>Voice activity result</returns>
    Task<VoiceActivityResult> ProcessAudioAsync(byte[] audioData, int sampleRate);
    
    /// <summary>
    /// Apply noise reduction to audio data
    /// </summary>
    /// <param name="audioData">Raw audio data</param>
    /// <param name="sampleRate">Audio sample rate</param>
    /// <returns>Processed audio data</returns>
    Task<byte[]> ApplyNoiseReductionAsync(byte[] audioData, int sampleRate);
    
    /// <summary>
    /// Normalize audio volume
    /// </summary>
    /// <param name="audioData">Raw audio data</param>
    /// <param name="targetLevel">Target volume level (0.0 to 1.0)</param>
    /// <returns>Normalized audio data</returns>
    Task<byte[]> NormalizeVolumeAsync(byte[] audioData, float targetLevel = 0.8f);
    
    /// <summary>
    /// Get the current audio input level
    /// </summary>
    /// <returns>Audio level (0.0 to 1.0)</returns>
    double GetCurrentAudioLevel();
    
    /// <summary>
    /// Configure voice activity detection sensitivity
    /// </summary>
    /// <param name="sensitivity">Sensitivity level (0.0 to 1.0)</param>
    void ConfigureVAD(double sensitivity);
}

