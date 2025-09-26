using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VoiceInputAssistant.Models;

/// <summary>
/// Represents a speech recognition request
/// </summary>
public class SpeechRecognitionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public string? FilePath { get; set; }
    public bool EnablePartialResults { get; set; } = true;
    public SpeechEngine Engine { get; set; }
    public RecognitionQuality Quality { get; set; } = RecognitionQuality.Balanced;
    public string Language { get; set; } = "en-US";
    public Dictionary<string, object> EngineOptions { get; set; } = new();
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public string? Context { get; set; }
    public ApplicationProfile? ApplicationProfile { get; set; }
}

/// <summary>
/// Represents a speech recognition result
/// </summary>
public class SpeechRecognitionResult
{
    public Guid RequestId { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public SpeechEngine Engine { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public DateTime CompletedTime { get; set; } = DateTime.UtcNow;
    public List<AlternativeTranscription> Alternatives { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? OriginalText { get; set; } // Before post-processing
    public bool WasPostProcessed { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Alternative transcription with confidence score
/// </summary>
public class AlternativeTranscription
{
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

/// <summary>
/// Application-specific configuration profile
/// </summary>
public class ApplicationProfile : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isEnabled = true;
    private SpeechEngine _preferredEngine = SpeechEngine.WhisperLocal;
    
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    public string Description { get; set; } = string.Empty;
    public AppDetectionMethod DetectionMethod { get; set; } = AppDetectionMethod.ProcessName;
    public List<string> DetectionCriteria { get; set; } = new();
    
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
    
    public SpeechEngine PreferredEngine
    {
        get => _preferredEngine;
        set => SetProperty(ref _preferredEngine, value);
    }
    
    public RecognitionQuality Quality { get; set; } = RecognitionQuality.Balanced;
    public string Language { get; set; } = "en-US";
    public VoiceActivationMode ActivationMode { get; set; } = VoiceActivationMode.PushToTalk;
    public PostProcessingMode PostProcessing { get; set; } = PostProcessingMode.BasicCorrection;
    public ToneStyle ToneStyle { get; set; } = ToneStyle.Original;
    public TextInsertionMethod InsertionMethod { get; set; } = TextInsertionMethod.Automatic;
    
    public HotkeyConfiguration? CustomHotkey { get; set; }
    public List<string> CustomVocabulary { get; set; } = new();
    public Dictionary<string, string> TextReplacements { get; set; } = new();
    public Dictionary<string, object> AdvancedSettings { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public int UsageCount { get; set; }
    public DateTime LastUsedAt { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        ModifiedAt = DateTime.UtcNow;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// Hotkey configuration for application shortcuts
/// </summary>
public class HotkeyConfiguration : INotifyPropertyChanged
{
    private KeyModifiers _modifiers = KeyModifiers.None;
    private System.Windows.Forms.Keys _key = System.Windows.Forms.Keys.None;
    private bool _isEnabled = true;
    
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public KeyModifiers Modifiers
    {
        get => _modifiers;
        set => SetProperty(ref _modifiers, value);
    }
    
    public System.Windows.Forms.Keys Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }
    
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
    
    public string Description { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsGlobal { get; set; } = true;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    public override string ToString()
    {
        var parts = new List<string>();
        
        if (Modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(KeyModifiers.Windows)) parts.Add("Win");
        
        if (Key != System.Windows.Forms.Keys.None)
            parts.Add(Key.ToString());
        
        return string.Join(" + ", parts);
    }
}

/// <summary>
/// Audio device information
/// </summary>
public class AudioDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public AudioInputSource Type { get; set; } = AudioInputSource.DefaultMicrophone;
    public int SampleRate { get; set; } = 16000;
    public int Channels { get; set; } = 1;
    public int BitsPerSample { get; set; } = 16;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Virtual key codes for Desktop applications
/// </summary>
public enum VirtualKey
{
    None = 0,
    F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75,
    F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
    D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34,
    D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,
    A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47,
    H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E,
    O = 0x4F, P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55,
    V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,
    Space = 0x20, Enter = 0x0D, Escape = 0x1B, Tab = 0x09,
    Backspace = 0x08, Delete = 0x2E, Insert = 0x2D,
    Home = 0x24, End = 0x23, PageUp = 0x21, PageDown = 0x22,
    Left = 0x25, Up = 0x26, Right = 0x27, Down = 0x28,
    Shift = 0x10, Control = 0x11, Alt = 0x12, Windows = 0x5B
}

/// <summary>
/// Modifier key flags for Desktop applications
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// Hotkey configuration for Desktop applications
/// </summary>
public class HotkeyConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public VirtualKey Key { get; set; }
    public ModifierKeys Modifiers { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Window information for Desktop applications
/// </summary>
public class WindowInfo
{
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public IntPtr Handle { get; set; }
    public bool IsVisible { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// System metrics and health information
/// </summary>
public class SystemMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double MemoryUsageMB { get; set; }
    public double AvailableMemoryMB { get; set; }
    public double TotalMemoryMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public double DiskFreeSpaceGB { get; set; }
    public double DiskTotalSpaceGB { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
}

/// <summary>
/// Health status for services and components
/// </summary>
public class HealthStatus
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ServiceStatus OverallStatus { get; set; } = ServiceStatus.Unknown;
    public Dictionary<string, ServiceStatus> ServiceChecks { get; set; } = new();
    public SystemMetrics SystemMetrics { get; set; } = new();
    public List<string> HealthMessages { get; set; } = new();
}

/// <summary>
/// User notification message
/// </summary>
public class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Information;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan? Duration { get; set; }
    public bool IsRead { get; set; }
    public bool IsPersistent { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Usage statistics and analytics
/// </summary>
public class UsageStatistics
{
    public DateTime Date { get; set; } = DateTime.Today;
    public int RecognitionAttempts { get; set; }
    public int SuccessfulRecognitions { get; set; }
    public double AverageConfidence { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public Dictionary<SpeechEngine, int> EngineUsage { get; set; } = new();
    public Dictionary<string, int> ApplicationUsage { get; set; } = new();
    public int WordsTranscribed { get; set; }
    public int CharactersTranscribed { get; set; }
    public TimeSpan ActiveTime { get; set; }
}

/// <summary>
/// Text processing rule for custom replacements
/// </summary>
public class TextProcessingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
    public bool IsRegex { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsCaseSensitive { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Active window information for application detection
/// </summary>
public class ActiveWindowInfo
{
    public IntPtr WindowHandle { get; set; }
    public string WindowTitle { get; set; } = string.Empty;
    public string Title => WindowTitle; // Alias for WindowTitle
    public string ProcessName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public ApplicationProfile? MatchedProfile { get; set; }
}

/// <summary>
/// Configuration for speech engine settings
/// </summary>
public class EngineConfiguration
{
    public SpeechEngine Engine { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsConfigured { get; set; }
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Region { get; set; }
    public string? ModelPath { get; set; }
    public RecognitionQuality DefaultQuality { get; set; } = RecognitionQuality.Balanced;
    public bool EnablePartialResults { get; set; } = true;
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of text post-processing operations
/// </summary>
public class PostProcessingResult
{
    public string OriginalText { get; set; } = string.Empty;
    public string ProcessedText { get; set; } = string.Empty;
    public PostProcessingMode Mode { get; set; }
    public ToneStyle ToneStyle { get; set; } = ToneStyle.Original;
    public TimeSpan ProcessingTime { get; set; }
    public bool WasModified { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the current application state
/// </summary>
public class ApplicationState : INotifyPropertyChanged
{
    private RecognitionState _recognitionState = RecognitionState.Ready;
    private bool _isListening;
    private ActiveWindowInfo? _activeWindow;
    private ApplicationProfile? _currentProfile;
    
    public RecognitionState RecognitionState
    {
        get => _recognitionState;
        set => SetProperty(ref _recognitionState, value);
    }
    
    public bool IsListening
    {
        get => _isListening;
        set => SetProperty(ref _isListening, value);
    }
    
    public ActiveWindowInfo? ActiveWindow
    {
        get => _activeWindow;
        set => SetProperty(ref _activeWindow, value);
    }
    
    public ApplicationProfile? CurrentProfile
    {
        get => _currentProfile;
        set => SetProperty(ref _currentProfile, value);
    }
    
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public HealthStatus HealthStatus { get; set; } = new();
    public List<NotificationMessage> RecentNotifications { get; set; } = new();
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        LastActivity = DateTime.UtcNow;
        OnPropertyChanged(propertyName);
        return true;
    }
}