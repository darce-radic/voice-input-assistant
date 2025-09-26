using CoreModels = VoiceInputAssistant.Core.Models;
using DesktopModels = VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Models;

/// <summary>
/// Helper class to convert between Core and Desktop model types
/// </summary>
public static class ModelConverters
{
    public static DesktopModels.UserSettings AdaptUserSettings(CoreModels.UserSettings coreSettings)
    {
        if (coreSettings == null) return new DesktopModels.UserSettings();
        
        return new DesktopModels.UserSettings
        {
            PreferredEngine = ConvertSpeechEngine(coreSettings.SpeechEngine),
            Quality = ConvertQuality(coreSettings.RecognitionQuality),
            VoiceActivationMode = ConvertActivationMode(coreSettings.VoiceActivationMode),
            PostProcessing = ConvertPostProcessingMode(coreSettings.PostProcessingMode),
            Language = "en-US", // Core doesn't have language setting yet
            ConfidenceThreshold = 0.7f, // Core doesn't have this setting yet
            EnableLogging = true, // Core always has logging enabled
            LogLevel = coreSettings.LogLevel,
            StartWithWindows = coreSettings.StartWithWindows,
            MinimizeToTray = coreSettings.MinimizeToTray,
            Theme = ConvertTheme(coreSettings.Theme)
            // TODO: Handle InputDevice conversion once audio device service is ready
        };
    }

    public static CoreModels.UserSettings AdaptUserSettingsBack(DesktopModels.UserSettings desktopSettings)
    {
        if (desktopSettings == null) return new CoreModels.UserSettings();
        
        return new CoreModels.UserSettings
        {
            EnableAnalytics = true, // Desktop doesn't have this setting
            StartWithWindows = desktopSettings.StartWithWindows,
            MinimizeToTray = desktopSettings.MinimizeToTray,
            ShowNotifications = true, // Desktop doesn't have this setting
            StartMinimized = false, // Desktop doesn't have this setting
            SpeechEngine = ConvertSpeechEngineBack(desktopSettings.PreferredEngine),
            RecognitionQuality = ConvertQualityBack(desktopSettings.Quality),
            VoiceActivationMode = ConvertActivationModeBack(desktopSettings.VoiceActivationMode),
            PostProcessingMode = ConvertPostProcessingModeBack(desktopSettings.PostProcessing),
            AudioSettings = new CoreModels.AudioSettings(), // Using defaults for now
            VadSensitivity = 0.5f, // Using default for now
            RecordingVolume = 1.0f, // Using default for now
            EnableNoiseSuppression = true, // Using default for now
            EnableEchoCancellation = false, // Using default for now
            EnableGlobalHotkeys = true, // Using default for now
            AutoCapitalize = true, // Using default for now
            AutoCorrect = true, // Using default for now
            AutoPunctuation = true, // Using default for now
            Theme = ConvertThemeBack(desktopSettings.Theme),
            LogLevel = desktopSettings.LogLevel
            // API keys and services are not part of desktop settings
        };
    }

    private static DesktopModels.SpeechEngine ConvertSpeechEngine(CoreModels.SpeechEngineType engine)
    {
        return engine switch
        {
            CoreModels.SpeechEngineType.WhisperLocal => DesktopModels.SpeechEngine.WhisperLocal,
            CoreModels.SpeechEngineType.OpenAIWhisper => DesktopModels.SpeechEngine.WhisperOpenAI,
            CoreModels.SpeechEngineType.AzureSpeech => DesktopModels.SpeechEngine.AzureSpeech,
            CoreModels.SpeechEngineType.WindowsSpeech => DesktopModels.SpeechEngine.WindowsSpeech,
            _ => DesktopModels.SpeechEngine.WhisperLocal
        };
    }

    private static CoreModels.SpeechEngineType ConvertSpeechEngineBack(DesktopModels.SpeechEngine engine)
    {
        return engine switch
        {
            DesktopModels.SpeechEngine.WhisperLocal => CoreModels.SpeechEngineType.WhisperLocal,
            DesktopModels.SpeechEngine.WhisperOpenAI => CoreModels.SpeechEngineType.OpenAIWhisper,
            DesktopModels.SpeechEngine.AzureSpeech => CoreModels.SpeechEngineType.AzureSpeech,
            DesktopModels.SpeechEngine.WindowsSpeech => CoreModels.SpeechEngineType.WindowsSpeech,
            _ => CoreModels.SpeechEngineType.WhisperLocal
        };
    }

    private static DesktopModels.RecognitionQuality ConvertQuality(string quality)
    {
        return quality switch
        {
            "Fast" => DesktopModels.RecognitionQuality.Fast,
            "HighAccuracy" => DesktopModels.RecognitionQuality.HighAccuracy,
            _ => DesktopModels.RecognitionQuality.Balanced
        };
    }

    private static string ConvertQualityBack(DesktopModels.RecognitionQuality quality)
    {
        return quality switch
        {
            DesktopModels.RecognitionQuality.Fast => "Fast",
            DesktopModels.RecognitionQuality.HighAccuracy => "HighAccuracy",
            _ => "Standard"
        };
    }

    private static DesktopModels.VoiceActivationMode ConvertActivationMode(string mode)
    {
        return mode switch
        {
            "VoiceActivated" => DesktopModels.VoiceActivationMode.VoiceActivated,
            "ToggleMode" => DesktopModels.VoiceActivationMode.ToggleMode,
            "Continuous" => DesktopModels.VoiceActivationMode.Continuous,
            _ => DesktopModels.VoiceActivationMode.PushToTalk
        };
    }

    private static string ConvertActivationModeBack(DesktopModels.VoiceActivationMode mode)
    {
        return mode switch
        {
            DesktopModels.VoiceActivationMode.VoiceActivated => "VoiceActivated",
            DesktopModels.VoiceActivationMode.ToggleMode => "ToggleMode",
            DesktopModels.VoiceActivationMode.Continuous => "Continuous",
            _ => "PushToTalk"
        };
    }

    private static DesktopModels.PostProcessingMode ConvertPostProcessingMode(string mode)
    {
        return mode switch
        {
            "None" => DesktopModels.PostProcessingMode.None,
            "Advanced" => DesktopModels.PostProcessingMode.Advanced,
            "Custom" => DesktopModels.PostProcessingMode.Custom,
            _ => DesktopModels.PostProcessingMode.BasicCorrection
        };
    }

    private static string ConvertPostProcessingModeBack(DesktopModels.PostProcessingMode mode)
    {
        return mode switch
        {
            DesktopModels.PostProcessingMode.None => "None",
            DesktopModels.PostProcessingMode.Advanced => "Advanced",
            DesktopModels.PostProcessingMode.Custom => "Custom",
            _ => "Basic"
        };
    }

    private static string ConvertTheme(CoreModels.Theme theme)
    {
        return theme switch
        {
            CoreModels.Theme.Light => "Light",
            CoreModels.Theme.Dark => "Dark",
            _ => "Auto"
        };
    }

    private static CoreModels.Theme ConvertThemeBack(string theme)
    {
        return theme switch
        {
            "Light" => CoreModels.Theme.Light,
            "Dark" => CoreModels.Theme.Dark,
            _ => CoreModels.Theme.System
        };
    }
    
    /// <summary>
    /// Convert from Core AudioDevice to Desktop AudioDevice
    /// </summary>
    public static DesktopModels.AudioDevice ConvertAudioDevice(CoreModels.AudioDevice coreDevice)
    {
        if (coreDevice == null)
            return new DesktopModels.AudioDevice();
            
        return new DesktopModels.AudioDevice
        {
            // Map only basic properties that are likely to exist in both types
            Id = Guid.Parse(coreDevice.Id), // Core uses string, Desktop uses Guid
            Name = coreDevice.Name,
            IsDefault = coreDevice.IsDefault,
            IsEnabled = coreDevice.IsEnabled
            // Other properties will use defaults if they don't exist in Core
        };
    }
    
    /// <summary>
    /// Convert from Desktop AudioDevice to Core AudioDevice
    /// </summary>
    public static CoreModels.AudioDevice ConvertAudioDeviceBack(DesktopModels.AudioDevice desktopDevice)
    {
        if (desktopDevice == null)
            return new CoreModels.AudioDevice();
            
        return new CoreModels.AudioDevice
        {
            // Map only basic properties that are likely to exist in both types
            Id = desktopDevice.Id.ToString(), // Desktop uses Guid, Core uses string
            Name = desktopDevice.Name,
            IsDefault = desktopDevice.IsDefault,
            IsEnabled = desktopDevice.IsEnabled
            // Other properties will use defaults if they don't exist in Core
        };
    }
    
}
