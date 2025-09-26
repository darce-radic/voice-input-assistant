using System;

namespace VoiceInputAssistant.Models;

/// <summary>
/// Available speech recognition engines
/// </summary>
public enum SpeechEngine
{
    /// <summary>Local Whisper implementation (offline)</summary>
    WhisperLocal,
    
    /// <summary>OpenAI Whisper API (cloud)</summary>
    WhisperOpenAI,
    
    /// <summary>Microsoft Azure Speech Services</summary>
    AzureSpeech,
    
    /// <summary>Google Cloud Speech-to-Text</summary>
    GoogleSpeech,
    
    /// <summary>Windows built-in Speech Recognition</summary>
    WindowsSpeech,
    
    /// <summary>Vosk offline speech recognition</summary>
    VoskLocal
}

/// <summary>
/// Speech recognition quality levels
/// </summary>
public enum RecognitionQuality
{
    /// <summary>Fast recognition with lower accuracy</summary>
    Fast,
    
    /// <summary>Balanced speed and accuracy</summary>
    Balanced,
    
    /// <summary>High accuracy with slower processing</summary>
    HighAccuracy,
    
    /// <summary>Maximum accuracy, slowest processing</summary>
    Maximum
}

/// <summary>
/// Audio input sources
/// </summary>
public enum AudioInputSource
{
    /// <summary>Default system microphone</summary>
    DefaultMicrophone,
    
    /// <summary>Specific microphone device</summary>
    SpecificMicrophone,
    
    /// <summary>System audio (what you hear)</summary>
    SystemAudio,
    
    /// <summary>Audio file input</summary>
    FileInput
}

/// <summary>
/// Voice activation modes
/// </summary>
public enum VoiceActivationMode
{
    /// <summary>Press and hold hotkey</summary>
    PushToTalk,
    
    /// <summary>Automatic voice activity detection</summary>
    VoiceActivated,
    
    /// <summary>Toggle on/off with hotkey</summary>
    ToggleMode,
    
    /// <summary>Always listening (continuous)</summary>
    Continuous
}

/// <summary>
/// Text post-processing options
/// </summary>
public enum PostProcessingMode
{
    /// <summary>No post-processing</summary>
    None,
    
    /// <summary>Basic grammar and punctuation correction</summary>
    BasicCorrection,
    
    /// <summary>Advanced grammar and style improvements</summary>
    Advanced,
    
    /// <summary>Custom processing with user-defined rules</summary>
    Custom
}

/// <summary>
/// Tone adjustment options for AI post-processing
/// </summary>
public enum ToneStyle
{
    /// <summary>Keep original tone</summary>
    Original,
    
    /// <summary>Professional business tone</summary>
    Professional,
    
    /// <summary>Friendly and casual tone</summary>
    Casual,
    
    /// <summary>Formal and academic tone</summary>
    Formal,
    
    /// <summary>Creative and expressive tone</summary>
    Creative,
    
    /// <summary>Technical and precise tone</summary>
    Technical
}

/// <summary>
/// Application detection methods
/// </summary>
public enum AppDetectionMethod
{
    /// <summary>Detect by window title</summary>
    WindowTitle,
    
    /// <summary>Detect by process name</summary>
    ProcessName,
    
    /// <summary>Detect by executable path</summary>
    ExecutablePath,
    
    /// <summary>Detect by window class name</summary>
    WindowClass,
    
    /// <summary>Combination of multiple methods</summary>
    Combination
}

/// <summary>
/// Text insertion methods
/// </summary>
public enum TextInsertionMethod
{
    /// <summary>Simulate keyboard typing</summary>
    SendKeys,
    
    /// <summary>Use clipboard paste</summary>
    ClipboardPaste,
    
    /// <summary>Text Services Framework</summary>
    TextServicesFramework,
    
    /// <summary>UI Automation framework</summary>
    UIAutomation,
    
    /// <summary>Direct Win32 messaging</summary>
    Win32Messages,
    
    /// <summary>Automatic method selection</summary>
    Automatic
}

/// <summary>
/// Service health status levels
/// </summary>
public enum ServiceStatus
{
    /// <summary>Service is healthy and operational</summary>
    Healthy,
    
    /// <summary>Service has minor issues but functional</summary>
    Degraded,
    
    /// <summary>Service is not operational</summary>
    Unhealthy,
    
    /// <summary>Service status is unknown</summary>
    Unknown
}

/// <summary>
/// Speech recognition operation states
/// </summary>
public enum RecognitionState
{
    /// <summary>Ready to start recognition</summary>
    Ready,
    
    /// <summary>Currently listening for audio</summary>
    Listening,
    
    /// <summary>Processing captured audio</summary>
    Processing,
    
    /// <summary>Recognition completed successfully</summary>
    Completed,
    
    /// <summary>Recognition failed with error</summary>
    Failed,
    
    /// <summary>Operation was cancelled</summary>
    Cancelled,
    
    /// <summary>Recognition timed out</summary>
    TimedOut
}

/// <summary>
/// User notification types
/// </summary>
public enum NotificationType
{
    /// <summary>Informational message</summary>
    Information,
    
    /// <summary>Warning message</summary>
    Warning,
    
    /// <summary>Error message</summary>
    Error,
    
    /// <summary>Success confirmation</summary>
    Success,
    
    /// <summary>Update available notification</summary>
    UpdateAvailable,
    
    /// <summary>Recognition result preview</summary>
    RecognitionResult
}

/// <summary>
/// Logging levels for application events
/// </summary>
public enum LogLevel
{
    /// <summary>Detailed debug information</summary>
    Debug,
    
    /// <summary>General information</summary>
    Information,
    
    /// <summary>Warning conditions</summary>
    Warning,
    
    /// <summary>Error conditions</summary>
    Error,
    
    /// <summary>Fatal error conditions</summary>
    Fatal
}

/// <summary>
/// Update installation modes
/// </summary>
public enum UpdateMode
{
    /// <summary>Automatically download and install updates</summary>
    Automatic,
    
    /// <summary>Download updates but ask before installing</summary>
    DownloadAndNotify,
    
    /// <summary>Only notify about available updates</summary>
    NotifyOnly,
    
    /// <summary>Disable automatic update checks</summary>
    Disabled
}

/// <summary>
/// Privacy modes for data handling
/// </summary>
public enum PrivacyMode
{
    /// <summary>Maximum privacy - all local processing</summary>
    MaximumPrivacy,
    
    /// <summary>Balanced privacy with some cloud features</summary>
    Balanced,
    
    /// <summary>Allow cloud processing for better features</summary>
    CloudEnhanced,
    
    /// <summary>Custom privacy settings</summary>
    Custom
}

/// <summary>
/// Keyboard shortcut modifiers
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// Application startup modes
/// </summary>
public enum StartupMode
{
    /// <summary>Start with Windows login</summary>
    WithWindows,
    
    /// <summary>Start manually by user</summary>
    Manual,
    
    /// <summary>Start minimized to system tray</summary>
    MinimizedToTray,
    
    /// <summary>Start and show main window</summary>
    ShowWindow
}