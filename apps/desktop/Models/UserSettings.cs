using System;

namespace VoiceInputAssistant.Models;

/// <summary>
/// User settings model for the application
/// </summary>
public class UserSettings
{
    public SpeechEngine PreferredEngine { get; set; } = SpeechEngine.WhisperLocal;
    public RecognitionQuality Quality { get; set; } = RecognitionQuality.Balanced;
    public VoiceActivationMode VoiceActivationMode { get; set; } = VoiceActivationMode.PushToTalk;
    public PostProcessingMode PostProcessing { get; set; } = PostProcessingMode.BasicCorrection;
    public string Language { get; set; } = "en-US";
    public float ConfidenceThreshold { get; set; } = 0.7f;
    public bool EnableLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public string Theme { get; set; } = "Auto";
    public AudioDevice? InputDevice { get; set; }
    
    /// <summary>
    /// Create a deep copy of the settings
    /// </summary>
    /// <returns>Cloned settings</returns>
    public UserSettings Clone()
    {
        return new UserSettings
        {
            PreferredEngine = PreferredEngine,
            Quality = Quality,
            VoiceActivationMode = VoiceActivationMode,
            PostProcessing = PostProcessing,
            Language = Language,
            ConfidenceThreshold = ConfidenceThreshold,
            EnableLogging = EnableLogging,
            LogLevel = LogLevel,
            StartWithWindows = StartWithWindows,
            MinimizeToTray = MinimizeToTray,
            Theme = Theme,
            InputDevice = InputDevice
        };
    }
}