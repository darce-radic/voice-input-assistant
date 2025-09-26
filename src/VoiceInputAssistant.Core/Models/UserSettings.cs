namespace VoiceInputAssistant.Core.Models;

public class UserSettings
{
    // Core Settings
    public bool EnableAnalytics { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    
    // Speech Recognition Settings
    public SpeechEngineType SpeechEngine { get; set; } = SpeechEngineType.WhisperLocal;
    public string RecognitionQuality { get; set; } = "Standard";
    public string VoiceActivationMode { get; set; } = "PushToTalk";
    public string PostProcessingMode { get; set; } = "Basic";
    
    // Audio Settings
    public AudioSettings AudioSettings { get; set; } = new();
    public float VadSensitivity { get; set; } = 0.5f;
    public float RecordingVolume { get; set; } = 1.0f;
    public bool EnableNoiseSuppression { get; set; } = true;
    public bool EnableEchoCancellation { get; set; } = false;
    
    // Hotkey Settings
    public bool EnableGlobalHotkeys { get; set; } = true;
    public Dictionary<string, HotkeyConfig> Hotkeys { get; set; } = new();
    public HotkeyConfig StartStopHotkey { get; set; } = new();
    public HotkeyConfig PushToTalkHotkey { get; set; } = new();
    public HotkeyConfig CancelHotkey { get; set; } = new();
    public HotkeyConfig ShowHideHotkey { get; set; } = new();
    
    // Text Processing Settings
    public List<TextProcessingRule> TextProcessingRules { get; set; } = new();
    public bool AutoCapitalize { get; set; } = true;
    public bool AutoCorrect { get; set; } = true;
    public bool AutoPunctuation { get; set; } = true;
    
    // API Keys and External Services
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string AzureSubscriptionKey { get; set; } = string.Empty;
    public string AzureRegion { get; set; } = "eastus";
    public string GoogleServiceAccountJson { get; set; } = string.Empty;
    
    // UI Settings
    public Theme Theme { get; set; } = Theme.System;
    public string LogLevel { get; set; } = "Information";
    
    /// <summary>
    /// Creates a deep copy of the UserSettings
    /// </summary>
    public UserSettings Clone()
    {
        var clone = new UserSettings
        {
            EnableAnalytics = EnableAnalytics,
            StartWithWindows = StartWithWindows,
            MinimizeToTray = MinimizeToTray,
            ShowNotifications = ShowNotifications,
            StartMinimized = StartMinimized,
            SpeechEngine = SpeechEngine,
            RecognitionQuality = RecognitionQuality,
            VoiceActivationMode = VoiceActivationMode,
            PostProcessingMode = PostProcessingMode,
            VadSensitivity = VadSensitivity,
            RecordingVolume = RecordingVolume,
            EnableNoiseSuppression = EnableNoiseSuppression,
            EnableEchoCancellation = EnableEchoCancellation,
            EnableGlobalHotkeys = EnableGlobalHotkeys,
            AutoCapitalize = AutoCapitalize,
            AutoCorrect = AutoCorrect,
            AutoPunctuation = AutoPunctuation,
            OpenAIApiKey = OpenAIApiKey,
            AzureSubscriptionKey = AzureSubscriptionKey,
            AzureRegion = AzureRegion,
            GoogleServiceAccountJson = GoogleServiceAccountJson,
            Theme = Theme,
            LogLevel = LogLevel
        };
        
        // Deep copy collections
        clone.Hotkeys = new Dictionary<string, HotkeyConfig>(Hotkeys);
        clone.TextProcessingRules = new List<TextProcessingRule>(TextProcessingRules);
        
        // Deep copy hotkey configs
        clone.StartStopHotkey = StartStopHotkey?.Clone() ?? new HotkeyConfig();
        clone.PushToTalkHotkey = PushToTalkHotkey?.Clone() ?? new HotkeyConfig();
        clone.CancelHotkey = CancelHotkey?.Clone() ?? new HotkeyConfig();
        clone.ShowHideHotkey = ShowHideHotkey?.Clone() ?? new HotkeyConfig();
        
        // Clone AudioSettings
        clone.AudioSettings = AudioSettings?.Clone() ?? new AudioSettings();
        
        return clone;
    }
}
