namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Types of speech recognition engines supported by the system
/// </summary>
public enum SpeechEngineType
{
    Unspecified = 0,
    WhisperLocal = 1,
    AzureSpeech = 2,
    OpenAIWhisper = 3,
    GoogleSpeech = 4,
    WindowsSpeech = 5
}