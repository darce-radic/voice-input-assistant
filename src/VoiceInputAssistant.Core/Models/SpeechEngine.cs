namespace VoiceInputAssistant.Core.Models;

public enum SpeechEngine
{
    Unspecified = 0,
    WhisperLocal = 1,
    AzureSpeech = 2,
    OpenAIWhisper = 3,
    GoogleSpeech = 4,
    WindowsSpeech = 5
}