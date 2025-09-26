using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Services
{
    /// <summary>
    /// Service for managing application settings and configuration
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;
        private readonly string _encryptionKey;
        private UserSettings _cachedSettings;
        private readonly object _settingsLock = new object();

        public event EventHandler<UserSettings> SettingsChanged;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Set up settings file paths
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VoiceInputAssistant");
            
            _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
            
            // Generate or load encryption key
            _encryptionKey = GetOrCreateEncryptionKey();
            
            // Ensure settings directory exists
            Directory.CreateDirectory(_settingsDirectory);
            
            _logger.LogDebug("SettingsService initialized with settings path: {SettingsPath}", _settingsFilePath);
        }

        public async Task<UserSettings> GetSettingsAsync()
        {
            try
            {
                // Return cached settings if available
                if (_cachedSettings != null)
                {
                    return _cachedSettings.Clone();
                }

                // Load settings from file
                if (File.Exists(_settingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    
                    // Decrypt sensitive settings if needed
                    var settingsData = JsonSerializer.Deserialize<SettingsData>(json);
                    
                    var settings = new UserSettings();
                    MapFromSettingsData(settingsData, settings);
                    
                    // Decrypt API keys and sensitive data
                    DecryptSensitiveSettings(settings);
                    
                    lock (_settingsLock)
                    {
                        _cachedSettings = settings.Clone();
                    }
                    
                    _logger.LogDebug("Settings loaded successfully from file");
                    return settings.Clone();
                }
                else
                {
                    // Return default settings if file doesn't exist
                    var defaultSettings = new UserSettings();
                    
                    lock (_settingsLock)
                    {
                        _cachedSettings = defaultSettings.Clone();
                    }
                    
                    // Save default settings to file
                    await SaveSettingsAsync(defaultSettings);
                    
                    _logger.LogInformation("Created default settings file");
                    return defaultSettings.Clone();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings, returning defaults");
                return new UserSettings();
            }
        }

        public async Task SaveSettingsAsync(UserSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                // Create a copy and encrypt sensitive data
                var settingsToSave = settings.Clone();
                EncryptSensitiveSettings(settingsToSave);
                
                // Convert to serializable format
                var settingsData = new SettingsData();
                MapToSettingsData(settingsToSave, settingsData);
                
                // Serialize to JSON
                var json = JsonSerializer.Serialize(settingsData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Write to file atomically
                var tempFilePath = _settingsFilePath + ".tmp";
                await File.WriteAllTextAsync(tempFilePath, json);
                
                // Atomic replace
                if (File.Exists(_settingsFilePath))
                {
                    File.Replace(tempFilePath, _settingsFilePath, null);
                }
                else
                {
                    File.Move(tempFilePath, _settingsFilePath);
                }

                // Update cached settings
                lock (_settingsLock)
                {
                    _cachedSettings = settings.Clone();
                }

                // Notify listeners
                SettingsChanged?.Invoke(this, settings.Clone());

                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                throw;
            }
        }

        public async Task ResetSettingsAsync()
        {
            try
            {
                var defaultSettings = new UserSettings();
                await SaveSettingsAsync(defaultSettings);
                
                _logger.LogInformation("Settings reset to defaults");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset settings");
                throw;
            }
        }

        public async Task ExportSettingsAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                var settings = await GetSettingsAsync();
                
                // Create export data without sensitive information
                var exportSettings = settings.Clone();
                ClearSensitiveData(exportSettings);
                
                var settingsData = new SettingsData();
                MapToSettingsData(exportSettings, settingsData);
                
                var json = JsonSerializer.Serialize(settingsData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogInformation("Settings exported to: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export settings to: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<UserSettings> ImportSettingsAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                throw new ArgumentException("Invalid file path", nameof(filePath));

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var settingsData = JsonSerializer.Deserialize<SettingsData>(json);
                
                var settings = new UserSettings();
                MapFromSettingsData(settingsData, settings);
                
                // Validate imported settings
                ValidateSettings(settings);
                
                _logger.LogInformation("Settings imported from: {FilePath}", filePath);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import settings from: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> ValidateSettingsAsync(UserSettings settings)
        {
            try
            {
                ValidateSettings(settings);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Settings validation failed");
                return false;
            }
        }

        public async Task BackupSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                    return;

                var backupDirectory = Path.Combine(_settingsDirectory, "Backups");
                Directory.CreateDirectory(backupDirectory);
                
                var backupFileName = $"settings-backup-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json";
                var backupFilePath = Path.Combine(backupDirectory, backupFileName);
                
                File.Copy(_settingsFilePath, backupFilePath);
                
                // Keep only the last 10 backups
                await CleanupOldBackups(backupDirectory, 10);
                
                _logger.LogDebug("Settings backed up to: {BackupPath}", backupFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup settings");
            }
        }

        private void ValidateSettings(UserSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Validate required fields
            if (settings.VadSensitivity < 0 || settings.VadSensitivity > 100)
                throw new ArgumentException("VadSensitivity must be between 0 and 100");

            if (settings.RecordingVolume < 0 || settings.RecordingVolume > 100)
                throw new ArgumentException("RecordingVolume must be between 0 and 100");

            // Validate hotkey configs
            if (settings.StartStopHotkey == null)
                settings.StartStopHotkey = new HotkeyConfig();

            if (settings.CancelHotkey == null)
                settings.CancelHotkey = new HotkeyConfig();
                
            if (settings.PushToTalkHotkey == null)
                settings.PushToTalkHotkey = new HotkeyConfig();
                
            if (settings.ShowHideHotkey == null)
                settings.ShowHideHotkey = new HotkeyConfig();

            // Additional validation can be added here
        }

        private void EncryptSensitiveSettings(UserSettings settings)
        {
            try
            {
                if (!string.IsNullOrEmpty(settings.OpenAIApiKey))
                    settings.OpenAIApiKey = EncryptString(settings.OpenAIApiKey);

                if (!string.IsNullOrEmpty(settings.AzureSubscriptionKey))
                    settings.AzureSubscriptionKey = EncryptString(settings.AzureSubscriptionKey);

                if (!string.IsNullOrEmpty(settings.GoogleServiceAccountJson))
                    settings.GoogleServiceAccountJson = EncryptString(settings.GoogleServiceAccountJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to encrypt sensitive settings");
            }
        }

        private void DecryptSensitiveSettings(UserSettings settings)
        {
            try
            {
                if (!string.IsNullOrEmpty(settings.OpenAIApiKey))
                    settings.OpenAIApiKey = DecryptString(settings.OpenAIApiKey);

                if (!string.IsNullOrEmpty(settings.AzureSubscriptionKey))
                    settings.AzureSubscriptionKey = DecryptString(settings.AzureSubscriptionKey);

                if (!string.IsNullOrEmpty(settings.GoogleServiceAccountJson))
                    settings.GoogleServiceAccountJson = DecryptString(settings.GoogleServiceAccountJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt sensitive settings");
                // Clear sensitive data if decryption fails
                ClearSensitiveData(settings);
            }
        }

        private void ClearSensitiveData(UserSettings settings)
        {
            settings.OpenAIApiKey = null;
            settings.AzureSubscriptionKey = null;
            settings.GoogleServiceAccountJson = null;
        }

        private string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var data = Encoding.UTF8.GetBytes(plainText);
                var encryptedData = ProtectedData.Protect(data, 
                    Encoding.UTF8.GetBytes(_encryptionKey), 
                    DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedData);
            }
            catch
            {
                // If encryption fails, return original (not ideal, but functional)
                return plainText;
            }
        }

        private string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                var encryptedData = Convert.FromBase64String(encryptedText);
                var data = ProtectedData.Unprotect(encryptedData, 
                    Encoding.UTF8.GetBytes(_encryptionKey), 
                    DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                // If decryption fails, return null to indicate corrupted data
                return null;
            }
        }

        private string GetOrCreateEncryptionKey()
        {
            var keyPath = Path.Combine(_settingsDirectory, ".key");
            
            try
            {
                if (File.Exists(keyPath))
                {
                    return File.ReadAllText(keyPath);
                }
                else
                {
                    // Generate a new encryption key
                    var key = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    Directory.CreateDirectory(_settingsDirectory);
                    File.WriteAllText(keyPath, key);
                    
                    // Hide the key file
                    File.SetAttributes(keyPath, FileAttributes.Hidden);
                    
                    return key;
                }
            }
            catch
            {
                // If we can't create a persistent key, use a session-based one
                return Environment.MachineName + Environment.UserName;
            }
        }

        private async Task CleanupOldBackups(string backupDirectory, int keepCount)
        {
            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "settings-backup-*.json");
                if (backupFiles.Length <= keepCount)
                    return;

                Array.Sort(backupFiles, (x, y) => File.GetCreationTime(y).CompareTo(File.GetCreationTime(x)));
                
                for (int i = keepCount; i < backupFiles.Length; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup old backup files");
            }
        }

        private void MapToSettingsData(UserSettings settings, SettingsData data)
        {
            // Map all properties from UserSettings to SettingsData
            // This is a simple mapping - in a real app you might use AutoMapper or similar
            data.SpeechEngine = settings.SpeechEngine.ToString();
            data.RecognitionQuality = settings.RecognitionQuality;
            data.VoiceActivationMode = settings.VoiceActivationMode;
            data.PostProcessingMode = settings.PostProcessingMode;
            data.Theme = settings.Theme.ToString();
            data.StartWithWindows = settings.StartWithWindows;
            data.StartMinimized = settings.StartMinimized;
            data.MinimizeToTray = settings.MinimizeToTray;
            data.ShowNotifications = settings.ShowNotifications;
            data.EnableGlobalHotkeys = settings.EnableGlobalHotkeys;
            data.StartStopHotkey = JsonSerializer.Serialize(settings.StartStopHotkey);
            data.PushToTalkHotkey = JsonSerializer.Serialize(settings.PushToTalkHotkey);
            data.CancelHotkey = JsonSerializer.Serialize(settings.CancelHotkey);
            data.ShowHideHotkey = JsonSerializer.Serialize(settings.ShowHideHotkey);
            data.VadSensitivity = (double)settings.VadSensitivity;
            data.EnableNoiseSuppression = settings.EnableNoiseSuppression;
            data.EnableEchoCancellation = settings.EnableEchoCancellation;
            data.RecordingVolume = (double)settings.RecordingVolume;
            data.AutoCapitalize = settings.AutoCapitalize;
            data.AutoCorrect = settings.AutoCorrect;
            data.AutoPunctuation = settings.AutoPunctuation;
            data.LogLevel = settings.LogLevel;
            data.EnableAnalytics = settings.EnableAnalytics;
            data.OpenAIApiKey = settings.OpenAIApiKey;
            data.AzureSubscriptionKey = settings.AzureSubscriptionKey;
            data.AzureRegion = settings.AzureRegion;
            data.GoogleServiceAccountJson = settings.GoogleServiceAccountJson;
            // InputDevice mapping would need special handling
        }

        private void MapFromSettingsData(SettingsData data, UserSettings settings)
        {
            // Map all properties from SettingsData to UserSettings
            if (Enum.TryParse<SpeechEngineType>(data.SpeechEngine, out var speechEngine))
                settings.SpeechEngine = speechEngine;
            
            // RecognitionQuality, VoiceActivationMode, PostProcessingMode are strings in UserSettings
            settings.RecognitionQuality = data.RecognitionQuality ?? "Standard";
            settings.VoiceActivationMode = data.VoiceActivationMode ?? "PushToTalk";
            settings.PostProcessingMode = data.PostProcessingMode ?? "Basic";

            if (Enum.TryParse<Theme>(data.Theme, out var theme))
                settings.Theme = theme;
            
            settings.StartWithWindows = data.StartWithWindows;
            settings.StartMinimized = data.StartMinimized;
            settings.MinimizeToTray = data.MinimizeToTray;
            settings.ShowNotifications = data.ShowNotifications;
            settings.EnableGlobalHotkeys = data.EnableGlobalHotkeys;
            
            // Deserialize hotkey configs from JSON strings
            try
            {
                settings.StartStopHotkey = !string.IsNullOrEmpty(data.StartStopHotkey) ? 
                    JsonSerializer.Deserialize<HotkeyConfig>(data.StartStopHotkey) ?? new HotkeyConfig() : new HotkeyConfig();
                settings.PushToTalkHotkey = !string.IsNullOrEmpty(data.PushToTalkHotkey) ? 
                    JsonSerializer.Deserialize<HotkeyConfig>(data.PushToTalkHotkey) ?? new HotkeyConfig() : new HotkeyConfig();
                settings.CancelHotkey = !string.IsNullOrEmpty(data.CancelHotkey) ? 
                    JsonSerializer.Deserialize<HotkeyConfig>(data.CancelHotkey) ?? new HotkeyConfig() : new HotkeyConfig();
                settings.ShowHideHotkey = !string.IsNullOrEmpty(data.ShowHideHotkey) ? 
                    JsonSerializer.Deserialize<HotkeyConfig>(data.ShowHideHotkey) ?? new HotkeyConfig() : new HotkeyConfig();
            }
            catch
            {
                // If deserialization fails, use default hotkey configs
                settings.StartStopHotkey = new HotkeyConfig();
                settings.PushToTalkHotkey = new HotkeyConfig();
                settings.CancelHotkey = new HotkeyConfig();
                settings.ShowHideHotkey = new HotkeyConfig();
            }
            
            settings.VadSensitivity = (float)data.VadSensitivity;
            settings.EnableNoiseSuppression = data.EnableNoiseSuppression;
            settings.EnableEchoCancellation = data.EnableEchoCancellation;
            settings.RecordingVolume = (float)data.RecordingVolume;
            settings.AutoCapitalize = data.AutoCapitalize;
            settings.AutoCorrect = data.AutoCorrect;
            settings.AutoPunctuation = data.AutoPunctuation;
            settings.LogLevel = data.LogLevel;
            settings.EnableAnalytics = data.EnableAnalytics;
            settings.OpenAIApiKey = data.OpenAIApiKey;
            settings.AzureSubscriptionKey = data.AzureSubscriptionKey;
            settings.AzureRegion = data.AzureRegion;
            settings.GoogleServiceAccountJson = data.GoogleServiceAccountJson;
        }

        /// <summary>
        /// Serializable settings data transfer object
        /// </summary>
        private class SettingsData
        {
            public string SpeechEngine { get; set; }
            public string RecognitionQuality { get; set; }
            public string VoiceActivationMode { get; set; }
            public string PostProcessingMode { get; set; }
            public string Theme { get; set; }
            public bool StartWithWindows { get; set; }
            public bool StartMinimized { get; set; }
            public bool MinimizeToTray { get; set; }
            public bool ShowNotifications { get; set; }
            public bool EnableGlobalHotkeys { get; set; }
            public string StartStopHotkey { get; set; }
            public string PushToTalkHotkey { get; set; }
            public string CancelHotkey { get; set; }
            public string ShowHideHotkey { get; set; }
            public double VadSensitivity { get; set; }
            public bool EnableNoiseSuppression { get; set; }
            public bool EnableEchoCancellation { get; set; }
            public double RecordingVolume { get; set; }
            public bool AutoCapitalize { get; set; }
            public bool AutoCorrect { get; set; }
            public bool AutoPunctuation { get; set; }
            public string LogLevel { get; set; }
            public bool EnableAnalytics { get; set; }
            public string OpenAIApiKey { get; set; }
            public string AzureSubscriptionKey { get; set; }
            public string AzureRegion { get; set; }
            public string GoogleServiceAccountJson { get; set; }
        }
    }
}