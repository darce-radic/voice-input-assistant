namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Post-processing modes for recognized text
/// </summary>
public enum PostProcessingMode
{
    None = 0,
    BasicCorrection = 1,
    SmartCorrection = 2,
    AIEnhanced = 3
}