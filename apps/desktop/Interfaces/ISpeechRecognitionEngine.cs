using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Common interface for all speech recognition engines
/// </summary>
public interface ISpeechRecognitionEngine
{
    /// <summary>
    /// The type of speech engine
    /// </summary>
    SpeechEngine EngineType { get; }
    
    /// <summary>
    /// Display name for the engine
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Whether the engine is currently available and configured
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Whether the engine requires internet connectivity
    /// </summary>
    bool RequiresInternet { get; }
    
    /// <summary>
    /// Supported languages for this engine
    /// </summary>
    string[] SupportedLanguages { get; }
    
    /// <summary>
    /// Initialize the engine with configuration
    /// </summary>
    /// <param name="configuration">Engine-specific configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if initialization successful</returns>
    Task<bool> InitializeAsync(EngineConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Perform speech recognition on audio data
    /// </summary>
    /// <param name="request">Recognition request with audio and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recognition result</returns>
    Task<SpeechRecognitionResult> RecognizeAsync(SpeechRecognitionRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Test the engine configuration
    /// </summary>
    /// <param name="configuration">Configuration to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result with success status and any error messages</returns>
    Task<EngineTestResult> TestConfigurationAsync(EngineConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get engine-specific configuration options
    /// </summary>
    /// <returns>Configuration schema for this engine</returns>
    EngineConfigurationSchema GetConfigurationSchema();
    
    /// <summary>
    /// Cleanup and dispose resources
    /// </summary>
    void Dispose();
    
    /// <summary>
    /// Event fired when engine status changes
    /// </summary>
    event EventHandler<EngineStatusChangedEventArgs> StatusChanged;
}

/// <summary>
/// Interface for engines that support streaming recognition
/// </summary>
public interface IStreamingSpeechRecognitionEngine : ISpeechRecognitionEngine
{
    /// <summary>
    /// Start streaming recognition session
    /// </summary>
    /// <param name="configuration">Stream configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream session</returns>
    Task<IRecognitionStream> StartStreamingAsync(StreamingConfiguration configuration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for real-time streaming recognition
/// </summary>
public interface IRecognitionStream : IDisposable
{
    /// <summary>
    /// Stream ID
    /// </summary>
    Guid StreamId { get; }
    
    /// <summary>
    /// Whether the stream is active
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Send audio data to the stream
    /// </summary>
    /// <param name="audioData">Raw audio bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Signal end of audio input
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EndStreamAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event for partial recognition results
    /// </summary>
    event EventHandler<PartialRecognitionEventArgs> PartialResult;
    
    /// <summary>
    /// Event for final recognition results
    /// </summary>
    event EventHandler<SpeechRecognitionResult> FinalResult;
    
    /// <summary>
    /// Event for stream errors
    /// </summary>
    event EventHandler<StreamErrorEventArgs> Error;
}

/// <summary>
/// Interface for engines that support voice activity detection
/// </summary>
public interface IVoiceActivityDetection
{
    /// <summary>
    /// Detect if the audio contains speech
    /// </summary>
    /// <param name="audioData">Audio data to analyze</param>
    /// <param name="sampleRate">Audio sample rate</param>
    /// <returns>Voice activity result</returns>
    Task<VoiceActivityResult> DetectVoiceActivityAsync(byte[] audioData, int sampleRate);
    
    /// <summary>
    /// Configure VAD sensitivity
    /// </summary>
    /// <param name="sensitivity">Sensitivity level (0.0 to 1.0)</param>
    void ConfigureVAD(double sensitivity);
}

/// <summary>
/// Engine test result
/// </summary>
public class EngineTestResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> TestResults { get; set; } = new();
}


/// <summary>
/// Configuration for streaming recognition
/// </summary>
public class StreamingConfiguration
{
    public string Language { get; set; } = "en-US";
    public RecognitionQuality Quality { get; set; } = RecognitionQuality.Balanced;
    public bool EnablePartialResults { get; set; } = true;
    public bool EnableVAD { get; set; } = true;
    public double VADSensitivity { get; set; } = 0.5;
    public TimeSpan MaxSilence { get; set; } = TimeSpan.FromSeconds(3);
    public Dictionary<string, object> EngineOptions { get; set; } = new();
}

/// <summary>
/// Engine configuration schema
/// </summary>
public class EngineConfigurationSchema
{
    public string EngineName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ConfigurationField> Fields { get; set; } = new();
    public Dictionary<string, object> DefaultValues { get; set; } = new();
}

/// <summary>
/// Configuration field definition
/// </summary>
public class ConfigurationField
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ConfigurationFieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSecret { get; set; }
    public string? DefaultValue { get; set; }
    public string[]? Options { get; set; }
    public string? ValidationPattern { get; set; }
}

/// <summary>
/// Configuration field types
/// </summary>
public enum ConfigurationFieldType
{
    Text,
    Password,
    Number,
    Boolean,
    Dropdown,
    FilePath,
    Url
}

/// <summary>
/// Event arguments for engine status changes
/// </summary>
public class EngineStatusChangedEventArgs : EventArgs
{
    public SpeechEngine Engine { get; set; }
    public bool IsAvailable { get; set; }
    public string? StatusMessage { get; set; }
    public Exception? Error { get; set; }
}


/// <summary>
/// Event arguments for stream errors
/// </summary>
public class StreamErrorEventArgs : EventArgs
{
    public Guid StreamId { get; set; }
    public Exception Exception { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public bool IsRecoverable { get; set; }
}