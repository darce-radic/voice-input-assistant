using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Interface for speech recognition services
/// </summary>
public interface ISpeechRecognitionService
{
    /// <summary>
    /// Gets a value indicating whether the service is currently listening for speech
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Gets a value indicating whether the service has been initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the current speech recognition engine being used
    /// </summary>
    SpeechEngineType? CurrentEngine { get; }
    /// <summary>
    /// Initialize the speech recognition service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Start listening for speech input
    /// </summary>
    Task StartListeningAsync();

    /// <summary>
    /// Stop listening for speech input
    /// </summary>
    Task StopListeningAsync();

    /// <summary>
    /// Transcribe audio data to text
    /// </summary>
    /// <param name="audioData">Audio data as byte array</param>
    /// <returns>Speech recognition result</returns>
    Task<SpeechRecognitionResult> TranscribeAsync(byte[] audioData);

    /// <summary>
    /// Get the current status of the speech engine
    /// </summary>
    /// <returns>Status information</returns>
    Task<VoiceInputAssistant.Core.Models.SpeechEngineStatus> GetStatusAsync();
}