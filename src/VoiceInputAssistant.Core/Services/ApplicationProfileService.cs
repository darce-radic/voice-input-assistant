using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
namespace VoiceInputAssistant.Core.Services
{
    /// <summary>
    /// Service for managing application profiles with automatic application detection and switching
    /// </summary>
public class ApplicationProfileService : Core.Services.Interfaces.IApplicationProfileService, IDisposable
    {
        /// <summary>
        /// Gets a profile by application name
        /// </summary>
        public Task<ApplicationProfile?> GetProfileAsync(string applicationName)
        {
            return GetProfileForApplicationAsync(applicationName);
        }

        /// <summary>
        /// Saves a profile
        /// </summary>
        public async Task<ApplicationProfile> SaveProfileAsync(ApplicationProfile profile)
        {
            if (profile.Id == Guid.Empty)
            {
                return await CreateProfileAsync(profile);
            }
            else
            {
                return await UpdateProfileAsync(profile);
            }
        }

        /// <summary>
        /// Deletes a profile by application name
        /// </summary>
        public async Task<bool> DeleteProfileAsync(string applicationName)
        {
            var profile = await GetProfileForApplicationAsync(applicationName);
            if (profile != null)
            {
                return await DeleteProfileAsync(profile.Id);
            }
            return false;
        }

        /// <summary>
        /// Gets the preferred engine for an application
        /// </summary>
        public async Task<SpeechEngineType?> GetPreferredEngineAsync(string applicationName)
        {
            var profile = await GetProfileForApplicationAsync(applicationName);
            return profile?.SpeechRecognitionSettings?.PreferredEngine;
        }

        /// <summary>
        /// Gets language settings for an application
        /// </summary>
        public async Task<LanguageSettings?> GetLanguageSettingsAsync(string applicationName)
        {
            var profile = await GetProfileForApplicationAsync(applicationName);
            if (profile?.SpeechRecognitionSettings == null)
            {
                return null;
            }

            return new LanguageSettings
            {
                PrimaryLanguage = profile.SpeechRecognitionSettings.PrimaryLanguage,
                SecondaryLanguages = profile.SpeechRecognitionSettings.SecondaryLanguages,
                AutoDetectLanguage = profile.SpeechRecognitionSettings.AutoDetectLanguage,
                LanguageWeights = profile.SpeechRecognitionSettings.LanguageWeights
            };
        }
        private readonly ILogger<ApplicationProfileService> _logger;
        private readonly ISettingsService _settingsService;
        private readonly string _profilesDirectory;
        private readonly ConcurrentDictionary<Guid, ApplicationProfile> _profiles;
private readonly System.Timers.Timer _monitoringTimer;
        private readonly SemaphoreSlim _profileLock;
        
        // Win32 API for window monitoring
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // State tracking
        private bool _isDisposed;
        private bool _isMonitoringActive;
        private ApplicationProfile _activeProfile;
        private ApplicationProfile _defaultProfile;
        private string _lastFocusedProcessName;
        private DateTime _lastProfileSwitch;

        public event EventHandler<ApplicationProfile> ActiveProfileChanged;

        public bool IsMonitoringActive => _isMonitoringActive;

        public ApplicationProfileService(
            ILogger<ApplicationProfileService> logger, 
            ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            
            _profiles = new ConcurrentDictionary<Guid, ApplicationProfile>();
            _profileLock = new SemaphoreSlim(1, 1);
            
            // Set up profiles directory
            _profilesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VoiceInputAssistant", "Profiles");
            Directory.CreateDirectory(_profilesDirectory);
            
            // Initialize monitoring timer (check every 1 second)
            _monitoringTimer = new System.Timers.Timer(1000);
            _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
            _monitoringTimer.AutoReset = true;
            
            // Initialize default profile
            _defaultProfile = CreateDefaultProfile();
            
            _logger.LogDebug("ApplicationProfileService initialized");
        }

        public async Task<ApplicationProfile> GetActiveProfileAsync()
        {
            await _profileLock.WaitAsync();
            try
            {
                return _activeProfile?.Clone() ?? _defaultProfile.Clone();
            }
            finally
            {
                _profileLock.Release();
            }
        }

        public async Task<IEnumerable<ApplicationProfile>> GetAllProfilesAsync()
        {
            try
            {
                await LoadProfilesFromDiskAsync();
                return _profiles.Values.Select(p => p.Clone()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all profiles");
                return new List<ApplicationProfile> { _defaultProfile };
            }
        }

        public async Task<ApplicationProfile> GetProfileByIdAsync(Guid id)
        {
            try
            {
                await LoadProfilesFromDiskAsync();
                return _profiles.TryGetValue(id, out var profile) ? profile.Clone() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get profile by ID {ProfileId}", id);
                return null;
            }
        }

        public async Task<ApplicationProfile> GetProfileForApplicationAsync(string applicationName)
        {
            if (string.IsNullOrEmpty(applicationName))
                return _defaultProfile.Clone();

            try
            {
                await LoadProfilesFromDiskAsync();
                
                // Normalize application name for comparison
                var normalizedAppName = NormalizeApplicationName(applicationName);
                
                // Find exact match first
                var exactMatch = _profiles.Values.FirstOrDefault(p => 
                    p.ApplicationExecutables.Any(exe => 
                        string.Equals(NormalizeApplicationName(exe), normalizedAppName, StringComparison.OrdinalIgnoreCase)));
                
                if (exactMatch != null)
                    return exactMatch.Clone();
                
                // Find partial match
                var partialMatch = _profiles.Values.FirstOrDefault(p => 
                    p.ApplicationExecutables.Any(exe => 
                        normalizedAppName.Contains(NormalizeApplicationName(exe)) || 
                        NormalizeApplicationName(exe).Contains(normalizedAppName)));
                
                return partialMatch?.Clone() ?? _defaultProfile.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get profile for application {ApplicationName}", applicationName);
                return _defaultProfile.Clone();
            }
        }

        public async Task<ApplicationProfile> CreateProfileAsync(ApplicationProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                // Assign new ID if not set
                if (profile.Id == Guid.Empty)
                    profile.Id = Guid.NewGuid();

                profile.CreatedAt = DateTime.UtcNow;
                profile.UpdatedAt = DateTime.UtcNow;

                // Validate profile
                ValidateProfile(profile);

                // Add to memory cache
                _profiles.AddOrUpdate(profile.Id, profile, (key, existing) => profile);

                // Save to disk
                await SaveProfileToDiskAsync(profile);

                _logger.LogInformation("Created application profile: {ProfileName} ({ProfileId})", 
                    profile.Name, profile.Id);

                return profile.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create profile {ProfileName}", profile?.Name);
                throw;
            }
        }

        public async Task<ApplicationProfile> UpdateProfileAsync(ApplicationProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                profile.UpdatedAt = DateTime.UtcNow;

                // Validate profile
                ValidateProfile(profile);

                // Update in memory cache
                _profiles.AddOrUpdate(profile.Id, profile, (key, existing) => profile);

                // Save to disk
                await SaveProfileToDiskAsync(profile);

                _logger.LogInformation("Updated application profile: {ProfileName} ({ProfileId})", 
                    profile.Name, profile.Id);

                return profile.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update profile {ProfileName} ({ProfileId})", 
                    profile?.Name, profile?.Id);
                throw;
            }
        }

        public async Task<bool> DeleteProfileAsync(Guid id)
        {
            try
            {
                if (_profiles.TryRemove(id, out var profile))
                {
                    // Remove from disk
                    var filePath = GetProfileFilePath(id);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // If this was the active profile, switch to default
                    if (_activeProfile?.Id == id)
                    {
                        await SetActiveProfileAsync(_defaultProfile.Id);
                    }

                    _logger.LogInformation("Deleted application profile: {ProfileName} ({ProfileId})", 
                        profile.Name, id);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete profile {ProfileId}", id);
                return false;
            }
        }

        public async Task<ApplicationProfile> SetActiveProfileForCurrentApplicationAsync()
        {
            try
            {
                var currentApp = await GetCurrentForegroundApplicationAsync();
                if (currentApp != null)
                {
                    var profile = await GetProfileForApplicationAsync(currentApp.ProcessName);
                    await SetActiveProfileInternalAsync(profile);
                    return profile;
                }

                return _defaultProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set active profile for current application");
                return _defaultProfile;
            }
        }

        public async Task<bool> SetActiveProfileAsync(Guid profileId)
        {
            try
            {
                ApplicationProfile profile;
                
                if (profileId == Guid.Empty)
                {
                    profile = _defaultProfile;
                }
                else
                {
                    profile = await GetProfileByIdAsync(profileId);
                    if (profile == null)
                        return false;
                }

                await SetActiveProfileInternalAsync(profile);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set active profile {ProfileId}", profileId);
                return false;
            }
        }

        public async Task<ApplicationProfile> GetDefaultProfileAsync()
        {
            return _defaultProfile.Clone();
        }

        public async Task<bool> SetDefaultProfileAsync(ApplicationProfile profile)
        {
            if (profile == null)
                return false;

            try
            {
                _defaultProfile = profile.Clone();
                _defaultProfile.Name = "Default";
                _defaultProfile.Description = "Default profile used when no application-specific profile is found";
                
                await SaveProfileToDiskAsync(_defaultProfile);
                _logger.LogInformation("Default profile updated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set default profile");
                return false;
            }
        }

        public async Task StartApplicationMonitoringAsync()
        {
            if (_isMonitoringActive)
                return;

            try
            {
                // Load existing profiles
                await LoadProfilesFromDiskAsync();

                // Start monitoring
                _monitoringTimer.Start();
                _isMonitoringActive = true;

                // Set initial profile
                await SetActiveProfileForCurrentApplicationAsync();

                _logger.LogInformation("Application monitoring started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start application monitoring");
            }
        }

        public async Task StopApplicationMonitoringAsync()
        {
            if (!_isMonitoringActive)
                return;

            try
            {
                _monitoringTimer.Stop();
                _isMonitoringActive = false;
                
                _logger.LogInformation("Application monitoring stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping application monitoring");
            }
        }

        private async void OnMonitoringTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isDisposed)
                return;

            try
            {
                var currentApp = await GetCurrentForegroundApplicationAsync();
                if (currentApp == null)
                    return;

                // Check if we need to switch profiles
                if (currentApp.ProcessName != _lastFocusedProcessName)
                {
                    // Prevent rapid profile switching
                    if (DateTime.UtcNow - _lastProfileSwitch < TimeSpan.FromSeconds(2))
                        return;

                    var profile = await GetProfileForApplicationAsync(currentApp.ProcessName);
                    
                    // Only switch if it's a different profile
                    if (profile.Id != _activeProfile?.Id)
                    {
                        await SetActiveProfileInternalAsync(profile);
                        _lastFocusedProcessName = currentApp.ProcessName;
                        _lastProfileSwitch = DateTime.UtcNow;
                        
                        _logger.LogDebug("Switched to profile '{ProfileName}' for application '{AppName}'", 
                            profile.Name, currentApp.ProcessName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in application monitoring timer");
            }
        }

        private async Task SetActiveProfileInternalAsync(ApplicationProfile profile)
        {
            if (profile == null)
                return;

            await _profileLock.WaitAsync();
            try
            {
                var previousProfile = _activeProfile;
                _activeProfile = profile;

                // Notify listeners of profile change
                if (previousProfile?.Id != profile.Id)
                {
                    ActiveProfileChanged?.Invoke(this, profile.Clone());
                }
            }
            finally
            {
                _profileLock.Release();
            }
        }

        private async Task<ApplicationInfo> GetCurrentForegroundApplicationAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    var hwnd = GetForegroundWindow();
                    if (hwnd == IntPtr.Zero)
                        return null;

                    GetWindowThreadProcessId(hwnd, out uint processId);
                    
                    try
                    {
                        var process = Process.GetProcessById((int)processId);
                        
                        var titleBuilder = new StringBuilder(256);
                        GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
                        
                        return new ApplicationInfo
                        {
                            ProcessId = (int)processId,
                            ProcessName = process.ProcessName,
                            WindowTitle = titleBuilder.ToString(),
                            ExecutablePath = process.MainModule?.FileName,
                            WindowHandle = hwnd
                        };
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current foreground application");
                return null;
            }
        }

        private async Task LoadProfilesFromDiskAsync()
        {
            try
            {
                if (!Directory.Exists(_profilesDirectory))
                    return;

                var profileFiles = Directory.GetFiles(_profilesDirectory, "*.json");
                
                foreach (var filePath in profileFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath);
                        var profile = JsonSerializer.Deserialize<ApplicationProfile>(json, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        });

                        if (profile != null && profile.Id != Guid.Empty)
                        {
                            _profiles.AddOrUpdate(profile.Id, profile, (key, existing) => profile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load profile from {FilePath}", filePath);
                    }
                }

                _logger.LogDebug("Loaded {Count} profiles from disk", _profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profiles from disk");
            }
        }

        private async Task SaveProfileToDiskAsync(ApplicationProfile profile)
        {
            try
            {
                var filePath = GetProfileFilePath(profile.Id);
                var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save profile {ProfileId} to disk", profile.Id);
                throw;
            }
        }

        private string GetProfileFilePath(Guid profileId)
        {
            return Path.Combine(_profilesDirectory, $"{profileId}.json");
        }

        private static ApplicationProfile CreateDefaultProfile()
        {
            return new ApplicationProfile
            {
                Id = Guid.Empty,
                Name = "Default",
                Description = "Default profile used when no application-specific profile is found",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApplicationExecutables = new List<string>(),
                WindowTitlePatterns = new List<string>(),
                ProcessNamePatterns = new List<string>(),
                SpeechRecognitionSettings = new SpeechRecognitionSettings(),
                HotkeyConfigs = new List<HotkeyConfig>(),
                TextProcessingRules = new List<TextProcessingRule>(),
                IsEnabled = true
            };
        }

        private static void ValidateProfile(ApplicationProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
                throw new ArgumentException("Profile name cannot be empty");

            if (profile.ApplicationExecutables == null)
                profile.ApplicationExecutables = new List<string>();

            if (profile.WindowTitlePatterns == null)
                profile.WindowTitlePatterns = new List<string>();

            if (profile.ProcessNamePatterns == null)
                profile.ProcessNamePatterns = new List<string>();

            if (profile.HotkeyConfigs == null)
                profile.HotkeyConfigs = new List<HotkeyConfig>();

            if (profile.TextProcessingRules == null)
                profile.TextProcessingRules = new List<TextProcessingRule>();
        }

        private static string NormalizeApplicationName(string appName)
        {
            if (string.IsNullOrEmpty(appName))
                return string.Empty;

            // Remove file extension and normalize case
            return Path.GetFileNameWithoutExtension(appName).ToLowerInvariant();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                StopApplicationMonitoringAsync().GetAwaiter().GetResult();
                _monitoringTimer?.Dispose();
                _profileLock?.Dispose();
                
                _isDisposed = true;
                _logger?.LogDebug("ApplicationProfileService disposed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing ApplicationProfileService");
            }
        }

        /// <summary>
        /// Information about a currently running application
        /// </summary>
        private class ApplicationInfo
        {
            public int ProcessId { get; set; }
            public string ProcessName { get; set; }
            public string WindowTitle { get; set; }
            public string ExecutablePath { get; set; }
            public IntPtr WindowHandle { get; set; }
        }
    }
}