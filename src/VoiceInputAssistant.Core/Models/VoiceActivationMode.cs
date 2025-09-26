namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Voice activation modes for speech recognition
/// </summary>
public enum VoiceActivationMode
{
    Manual = 0,
    PushToTalk = 1,
    VoiceActivityDetection = 2,
    Continuous = 3
}