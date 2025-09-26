using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Interface for speech recognition settings
/// </summary>
public interface ISpeechRecognitionSettings
{
    /// <summary>
    /// Preferred speech recognition engine
    /// </summary>
    SpeechEngineType? PreferredEngine { get; }

    /// <summary>
    /// Language code for speech recognition (e.g., "en-US", "fr-FR")
    /// </summary>
    string? Language { get; }

    /// <summary>
    /// Azure Speech Service subscription key
    /// </summary>
    string? AzureSubscriptionKey { get; }

    /// <summary>
    /// Azure Speech Service region
    /// </summary>
    string? AzureRegion { get; }

    /// <summary>
    /// OpenAI API key for Whisper service
    /// </summary>
    string? OpenAIApiKey { get; }

    /// <summary>
    /// Recognition quality level
    /// </summary>
    RecognitionQuality Quality { get; }

    /// <summary>
    /// Enable continuous recognition
    /// </summary>
    bool ContinuousRecognition { get; }

    /// <summary>
    /// Enable interim results during recognition
    /// </summary>
    bool EnableInterimResults { get; }

    /// <summary>
    /// Enable profanity filtering
    /// </summary>
    bool EnableProfanityFilter { get; }

    /// <summary>
    /// Recognition timeout in seconds
    /// </summary>
    int TimeoutSeconds { get; }

    /// <summary>
    /// Custom vocabulary words to improve recognition accuracy
    /// </summary>
    string[]? CustomVocabulary { get; }

    /// <summary>
    /// Audio preprocessing settings
    /// </summary>
    bool EnableNoiseReduction { get; }

    /// <summary>
    /// Enable echo cancellation
    /// </summary>
    bool EnableEchoCancellation { get; }

    /// <summary>
    /// Auto-detect language (if supported by engine)
    /// </summary>
    bool AutoDetectLanguage { get; }
}