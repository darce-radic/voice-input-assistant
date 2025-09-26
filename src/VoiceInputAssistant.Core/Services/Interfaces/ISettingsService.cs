using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for managing application-wide settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Get current user settings
    /// </summary>
    Task<UserSettings> GetSettingsAsync();
    
    /// <summary>
    /// Save user settings
    /// </summary>
Task SaveSettingsAsync(UserSettings settings);
    
    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    Task ResetSettingsAsync();
    
    /// <summary>
    /// Import settings from file
    /// </summary>
    /// <returns>Imported settings</returns>
    Task<UserSettings> ImportSettingsAsync(string filePath);
    
    /// <summary>
    /// Export settings to file
    /// </summary>
    Task ExportSettingsAsync(string filePath);
    
    /// <summary>
    /// Validate settings
    /// </summary>
    /// <returns>True if settings are valid</returns>
    Task<bool> ValidateSettingsAsync(UserSettings settings);
    
    /// <summary>
    /// Backup current settings
    /// </summary>
    Task BackupSettingsAsync();
    
    /// <summary>
    /// Event raised when settings change
    /// </summary>
    event EventHandler<UserSettings> SettingsChanged;
}
