using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for text insertion into applications
/// </summary>
public interface ITextInjectionService
{
    /// <summary>
    /// Insert text into the currently active application
    /// </summary>
    /// <param name="text">Text to insert</param>
    /// <param name="method">Insertion method to use</param>
    /// <param name="targetWindow">Optional target window handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if insertion was successful</returns>
    Task<bool> InsertTextAsync(string text, TextInsertionMethod method = TextInsertionMethod.Automatic, IntPtr? targetWindow = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the current window is a protected field (like password input)
    /// </summary>
    /// <param name="windowHandle">Window to check</param>
    /// <returns>True if the window/control is protected</returns>
    Task<bool> IsProtectedFieldAsync(IntPtr? windowHandle = null);
    
    /// <summary>
    /// Get the best insertion method for the current context
    /// </summary>
    /// <param name="windowHandle">Target window handle</param>
    /// <returns>Recommended insertion method</returns>
    Task<TextInsertionMethod> GetBestInsertionMethodAsync(IntPtr? windowHandle = null);
    
    /// <summary>
    /// Test text insertion capability
    /// </summary>
    /// <param name="method">Method to test</param>
    /// <param name="windowHandle">Optional window handle</param>
    /// <returns>Test result</returns>
    Task<TextInsertionTestResult> TestInsertionMethodAsync(TextInsertionMethod method, IntPtr? windowHandle = null);
}

/// <summary>
/// Interface for application profile management
/// </summary>
public interface IApplicationProfileService
{
    /// <summary>
    /// Get all application profiles
    /// </summary>
    /// <returns>List of all profiles</returns>
    Task<List<ApplicationProfile>> GetAllProfilesAsync();
    
    /// <summary>
    /// Get the currently active profile
    /// </summary>
    /// <returns>Active profile or null if none set</returns>
    Task<ApplicationProfile?> GetActiveProfileAsync();
    
    /// <summary>
    /// Set the active profile
    /// </summary>
    /// <param name="profile">Profile to activate</param>
    Task SetActiveProfileAsync(ApplicationProfile profile);
    
    /// <summary>
    /// Get profile by ID
    /// </summary>
    /// <param name="id">Profile ID</param>
    /// <returns>Profile or null if not found</returns>
    Task<ApplicationProfile?> GetProfileByIdAsync(Guid id);
    
    /// <summary>
    /// Find matching profile for the current active window
    /// </summary>
    /// <param name="windowInfo">Active window information</param>
    /// <returns>Matching profile or null</returns>
    Task<ApplicationProfile?> FindMatchingProfileAsync(ActiveWindowInfo windowInfo);
    
    /// <summary>
    /// Save or update a profile
    /// </summary>
    /// <param name="profile">Profile to save</param>
    /// <returns>Saved profile</returns>
    Task<ApplicationProfile> SaveProfileAsync(ApplicationProfile profile);
    
    /// <summary>
    /// Delete a profile
    /// </summary>
    /// <param name="id">Profile ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteProfileAsync(Guid id);
    
    /// <summary>
    /// Export profiles to file
    /// </summary>
    /// <param name="filePath">Export file path</param>
    /// <param name="profileIds">Optional specific profile IDs to export</param>
    /// <returns>True if export successful</returns>
    Task<bool> ExportProfilesAsync(string filePath, Guid[]? profileIds = null);
    
    /// <summary>
    /// Import profiles from file
    /// </summary>
    /// <param name="filePath">Import file path</param>
    /// <param name="mergeStrategy">How to handle conflicts</param>
    /// <returns>Number of profiles imported</returns>
    Task<int> ImportProfilesAsync(string filePath, ProfileMergeStrategy mergeStrategy = ProfileMergeStrategy.Skip);
    
    /// <summary>
    /// Event fired when profiles are changed
    /// </summary>
    event EventHandler<ProfileChangedEventArgs> ProfileChanged;
    
    /// <summary>
    /// Event fired when the active profile is changed
    /// </summary>
    event EventHandler<ApplicationProfile> ActiveProfileChanged;
}

/// <summary>
/// Interface for active window detection and monitoring
/// </summary>
public interface IActiveWindowService
{
    /// <summary>
    /// Get information about the currently active window
    /// </summary>
    /// <returns>Active window information</returns>
    Task<ActiveWindowInfo> GetActiveWindowAsync();
    
    /// <summary>
    /// Start monitoring active window changes
    /// </summary>
    void StartMonitoring();
    
    /// <summary>
    /// Stop monitoring active window changes
    /// </summary>
    void StopMonitoring();
    
    /// <summary>
    /// Whether monitoring is currently active
    /// </summary>
    bool IsMonitoring { get; }
    
    /// <summary>
    /// Event fired when the active window changes
    /// </summary>
    event EventHandler<ActiveWindowChangedEventArgs> ActiveWindowChanged;
}

/// <summary>
/// Interface for audio capture and processing
/// </summary>
public interface IAudioCaptureService
{
    /// <summary>
    /// Get available audio input devices
    /// </summary>
    /// <returns>List of available devices</returns>
    Task<List<AudioDevice>> GetAvailableDevicesAsync();
    
    /// <summary>
    /// Start audio capture
    /// </summary>
    /// <param name="device">Device to capture from</param>
    /// <param name="format">Audio format settings</param>
    /// <returns>True if capture started successfully</returns>
    Task<bool> StartCaptureAsync(AudioDevice device, AudioFormat format);
    
    /// <summary>
    /// Stop audio capture
    /// </summary>
    Task StopCaptureAsync();
    
    /// <summary>
    /// Whether audio capture is currently active
    /// </summary>
    bool IsCapturing { get; }
    
    /// <summary>
    /// Current audio input level (0.0 to 1.0)
    /// </summary>
    double InputLevel { get; }
    
    /// <summary>
    /// Event fired when audio data is captured
    /// </summary>
    event EventHandler<AudioCapturedEventArgs> AudioCaptured;
    
    /// <summary>
    /// Event fired when voice activity is detected
    /// </summary>
    event EventHandler<VoiceActivityEventArgs> VoiceActivityDetected;
}

/// <summary>
/// Interface for global hotkey management
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// Register a global hotkey
    /// </summary>
    /// <param name="hotkey">Hotkey configuration</param>
    /// <returns>True if registered successfully</returns>
    Task<bool> RegisterHotkeyAsync(HotkeyConfiguration hotkey);
    
    /// <summary>
    /// Unregister a hotkey
    /// </summary>
    /// <param name="hotkeyId">Hotkey ID to unregister</param>
    /// <returns>True if unregistered successfully</returns>
    Task<bool> UnregisterHotkeyAsync(Guid hotkeyId);
    
    /// <summary>
    /// Unregister all hotkeys
    /// </summary>
    Task UnregisterAllHotkeysAsync();
    
    /// <summary>
    /// Get all registered hotkeys
    /// </summary>
    /// <returns>List of registered hotkeys</returns>
    Task<List<HotkeyConfiguration>> GetRegisteredHotkeysAsync();
    
    /// <summary>
    /// Check if a hotkey combination is available
    /// </summary>
    /// <param name="modifiers">Modifier keys</param>
    /// <param name="key">Main key</param>
    /// <returns>True if available</returns>
    Task<bool> IsHotkeyAvailableAsync(KeyModifiers modifiers, System.Windows.Forms.Keys key);
    
    /// <summary>
    /// Event fired when a registered hotkey is pressed
    /// </summary>
    event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;
}

/// <summary>
/// Interface for configuration and settings management
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Get all application settings
    /// </summary>
    /// <returns>User settings object</returns>
    Task<UserSettings> GetSettingsAsync();
    
    /// <summary>
    /// Get a setting value
    /// </summary>
    /// <typeparam name="T">Setting type</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Setting value</returns>
    Task<T> GetSettingAsync<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// Set a setting value
    /// </summary>
    /// <typeparam name="T">Setting type</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="value">Value to set</param>
    Task SetSettingAsync<T>(string key, T value);
    
    /// <summary>
    /// Delete a setting
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <returns>True if deleted</returns>
    Task<bool> DeleteSettingAsync(string key);
    
    /// <summary>
    /// Get all settings with a key prefix
    /// </summary>
    /// <param name="prefix">Key prefix</param>
    /// <returns>Dictionary of matching settings</returns>
    Task<Dictionary<string, object>> GetSettingsByPrefixAsync(string prefix);
    
    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    Task ResetAllSettingsAsync();
    
    /// <summary>
    /// Export settings to file
    /// </summary>
    /// <param name="filePath">Export file path</param>
    /// <param name="includeSecrets">Whether to include encrypted secrets</param>
    /// <returns>True if export successful</returns>
    Task<bool> ExportSettingsAsync(string filePath, bool includeSecrets = false);
    
    /// <summary>
    /// Import settings from file
    /// </summary>
    /// <param name="filePath">Import file path</param>
    /// <param name="overwriteExisting">Whether to overwrite existing settings</param>
    /// <returns>Number of settings imported</returns>
    Task<int> ImportSettingsAsync(string filePath, bool overwriteExisting = false);
    
    /// <summary>
    /// Event fired when settings are changed
    /// </summary>
    event EventHandler<SettingChangedEventArgs> SettingChanged;
}

// Supporting classes and enums

/// <summary>
/// Audio format configuration
/// </summary>
public class AudioFormat
{
    public int SampleRate { get; set; } = 16000;
    public int Channels { get; set; } = 1;
    public int BitsPerSample { get; set; } = 16;
    public int BufferSize { get; set; } = 1024;
}

/// <summary>
/// Text insertion test result
/// </summary>
public class TextInsertionTestResult
{
    public bool IsSupported { get; set; }
    public bool IsReliable { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// Profile merge strategies for import operations
/// </summary>
public enum ProfileMergeStrategy
{
    Skip,
    Overwrite,
    Merge,
    CreateNew
}

// Event argument classes

public class ProfileChangedEventArgs : EventArgs
{
    public ApplicationProfile Profile { get; set; } = null!;
    public ProfileChangeType ChangeType { get; set; }
}

public enum ProfileChangeType
{
    Created,
    Updated,
    Deleted,
    Imported
}

public class ActiveWindowChangedEventArgs : EventArgs
{
    public ActiveWindowInfo? PreviousWindow { get; set; }
    public ActiveWindowInfo CurrentWindow { get; set; } = null!;
}

public class AudioCapturedEventArgs : EventArgs
{
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public AudioFormat Format { get; set; } = null!;
    public double InputLevel { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class VoiceActivityEventArgs : EventArgs
{
    public bool HasVoice { get; set; }
    public double Confidence { get; set; }
    public double EnergyLevel { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyConfiguration Hotkey { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SettingChangedEventArgs : EventArgs
{
    public string Key { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}