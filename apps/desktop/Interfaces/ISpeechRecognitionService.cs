using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;
using CoreEngine = VoiceInputAssistant.Core.Models.SpeechEngine;
using ModelEngine = VoiceInputAssistant.Models.SpeechEngine;
using ModelResult = VoiceInputAssistant.Models.SpeechRecognitionResult;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Service interface for speech recognition functionality
/// </summary>
public interface ISpeechRecognitionService : IDisposable
{
    /// <summary>
    /// Gets the available speech recognition engines
    /// </summary>
    IEnumerable<ModelEngine> AvailableEngines { get; }

    /// <summary>
    /// Gets the currently active speech recognition engine
    /// </summary>
    ModelEngine CurrentEngine { get; }

    /// <summary>
    /// Gets a value indicating whether the service is currently listening for speech
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Gets a value indicating whether voice activity is currently detected
    /// </summary>
    bool VoiceActivityDetected { get; }

    /// <summary>
    /// Gets a value indicating whether the service is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Event raised when an interim recognition result is received
    /// </summary>
    event EventHandler<SpeechRecognitionEventArgs>? InterimResultReceived;

    /// <summary>
    /// Event raised when a final recognition result is received
    /// </summary>
    event EventHandler<SpeechRecognitionEventArgs>? FinalResultReceived;

    /// <summary>
    /// Event raised when an error occurs during speech recognition
    /// </summary>
    event EventHandler<SpeechRecognitionErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Event raised when voice activity status changes
    /// </summary>
    event EventHandler<VoiceActivityEventArgs>? VoiceActivityChanged;

    /// <summary>
    /// Initializes the speech recognition service with the specified engine
    /// </summary>
    /// <param name="engine">The speech recognition engine to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization operation</returns>
    Task InitializeAsync(ModelEngine engine, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a different speech recognition engine
    /// </summary>
    /// <param name="engine">The new engine to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the switch operation</returns>
    Task SwitchEngineAsync(ModelEngine engine, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts listening for speech input
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the start operation</returns>
    Task StartListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening for speech input
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the stop operation</returns>
    Task StopListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an audio file and returns the recognition result
    /// </summary>
    /// <param name="filePath">Path to the audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task containing the recognition result</returns>
    Task<ModelResult> ProcessAudioFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the specified speech recognition engine
    /// </summary>
    /// <param name="config">Engine configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the configuration operation</returns>
    Task ConfigureEngineAsync(SpeechEngineConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the vocabulary used by the current engine
    /// </summary>
    /// <param name="phrases">List of phrases to add to vocabulary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the update operation</returns>
    Task UpdateVocabularyAsync(IEnumerable<string> phrases, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current confidence threshold for speech recognition
    /// </summary>
    /// <returns>The confidence threshold</returns>
    float GetConfidenceThreshold();

    /// <summary>
    /// Sets the confidence threshold for speech recognition
    /// </summary>
    /// <param name="threshold">The new confidence threshold</param>
    void SetConfidenceThreshold(float threshold);
}

/// <summary>
/// Event arguments for speech recognition results
/// </summary>
public class SpeechRecognitionEventArgs : EventArgs
{
    public ModelResult Result { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for speech recognition errors
/// </summary>
public class SpeechRecognitionErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public ModelEngine Engine { get; set; }
    public bool IsRecoverable { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
