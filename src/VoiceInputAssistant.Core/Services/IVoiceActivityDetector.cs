using System.Threading.Tasks;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// Interface for voice activity detection (VAD)
/// </summary>
public interface IVoiceActivityDetector
{
    /// <summary>
    /// Initializes the voice activity detector
    /// </summary>
    /// <param name="sensitivity">Detection sensitivity (0-1, where 1 is most sensitive)</param>
    /// <param name="minSilenceDurationMs">Minimum silence duration in ms to end speech detection</param>
    Task InitializeAsync(float sensitivity = 0.7f, int minSilenceDurationMs = 1000);
    
    /// <summary>
    /// Detects whether the audio data contains speech
    /// </summary>
    /// <param name="audioData">Audio data to analyze</param>
    /// <returns>True if speech is detected, false otherwise</returns>
    Task<bool> IsSpeechDetectedAsync(byte[] audioData);
    
    /// <summary>
    /// Detects speech segments in the audio data
    /// </summary>
    /// <param name="audioData">Audio data to analyze</param>
    /// <param name="sampleRate">Audio sample rate in Hz</param>
    /// <returns>List of speech segments with start and end times</returns>
    Task<IEnumerable<SpeechSegment>> DetectSpeechSegmentsAsync(byte[] audioData, int sampleRate);
    
    /// <summary>
    /// Gets the current audio energy level (for debugging/visualization)
    /// </summary>
    /// <param name="audioData">Audio data to analyze</param>
    /// <returns>Audio energy level (0-1)</returns>
    Task<float> GetAudioEnergyLevelAsync(byte[] audioData);
    
    /// <summary>
    /// Determines if the current audio indicates the end of speech
    /// </summary>
    /// <param name="audioData">Current audio buffer</param>
    /// <returns>True if speech has ended</returns>
    Task<bool> IsSpeechEndedAsync(byte[] audioData);
    
    /// <summary>
    /// Calibrates the detector based on background noise level
    /// </summary>
    /// <param name="backgroundAudio">Background noise audio sample</param>
    /// <returns>Calibrated sensitivity level</returns>
    Task<float> CalibrateAsync(byte[] backgroundAudio);
}

/// <summary>
/// Represents a speech segment detected in audio
/// </summary>
public record SpeechSegment
{
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public float Confidence { get; init; }
    public float AverageEnergyLevel { get; init; }
    public int AudioDataStartIndex { get; init; }
    public int AudioDataEndIndex { get; init; }
}