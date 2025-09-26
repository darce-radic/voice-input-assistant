using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// Interface for audio processing operations
/// </summary>
public interface IAudioProcessor
{
    /// <summary>
    /// Initializes the audio processor with settings
    /// </summary>
    /// <param name="settings">Audio processing settings</param>
    Task InitializeAsync(AudioSettings settings);
    
    /// <summary>
    /// Captures audio from the specified device
    /// </summary>
    /// <param name="device">Audio input device to use</param>
    /// <param name="durationMs">Duration in milliseconds to capture, or 0 for continuous</param>
    /// <returns>Audio data bytes</returns>
    Task<byte[]> CaptureAudioAsync(AudioDevice device, int durationMs = 0);
    
    /// <summary>
    /// Preprocesses raw audio data for optimal speech recognition
    /// </summary>
    /// <param name="audioData">Raw audio data</param>
    /// <param name="settings">Audio processing settings</param>
    /// <returns>Preprocessed audio data</returns>
    Task<byte[]> PreprocessAudioAsync(byte[] audioData, AudioSettings settings);
    
    /// <summary>
    /// Reduces noise in audio data
    /// </summary>
    /// <param name="audioData">Raw audio data</param>
    /// <param name="noiseReductionLevel">Level of noise reduction (0-1)</param>
    /// <returns>Noise-reduced audio data</returns>
    Task<byte[]> ReduceNoiseAsync(byte[] audioData, float noiseReductionLevel);
    
    /// <summary>
    /// Normalizes audio volume levels
    /// </summary>
    /// <param name="audioData">Audio data</param>
    /// <param name="targetDbLevel">Target dB level</param>
    /// <returns>Volume-normalized audio data</returns>
    Task<byte[]> NormalizeVolumeAsync(byte[] audioData, float targetDbLevel = -16.0f);
    
    /// <summary>
    /// Gets the current audio input level (for UI visualization)
    /// </summary>
    /// <returns>Current audio input level (0-1)</returns>
    Task<float> GetAudioInputLevelAsync();
    
    /// <summary>
    /// Converts audio data to the required format for a specific speech engine
    /// </summary>
    /// <param name="audioData">Source audio data</param>
    /// <param name="targetFormat">Target audio format</param>
    /// <param name="engine">Speech engine type</param>
    /// <returns>Converted audio data</returns>
    Task<byte[]> ConvertAudioFormatAsync(byte[] audioData, AudioFormat targetFormat, SpeechEngineType engine);
    
    /// <summary>
    /// Gets available audio input devices
    /// </summary>
    /// <returns>List of available audio devices</returns>
    Task<IEnumerable<AudioDevice>> GetAudioInputDevicesAsync();
}