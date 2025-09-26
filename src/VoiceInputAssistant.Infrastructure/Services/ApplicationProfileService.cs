using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Infrastructure.Data;
using VoiceInputAssistant.Infrastructure.Data.Entities;

namespace VoiceInputAssistant.Infrastructure.Services;

/// <summary>
/// Service for managing application profiles with database storage and caching
/// </summary>
public class ApplicationProfileService : IApplicationProfileService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ApplicationProfileService> _logger;
    private readonly IMemoryCache _cache;
    private readonly System.Timers.Timer _monitoringTimer;
    private ApplicationProfile? _activeProfile;
    private string _lastFocusedProcessName = string.Empty;
    private const string CacheKeyPrefix = "AppProfile_";

    public event EventHandler<ApplicationProfile>? ActiveProfileChanged;

    public bool IsMonitoringActive { get; private set; }

    public ApplicationProfileService(
        ApplicationDbContext dbContext,
        ILogger<ApplicationProfileService> logger,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _monitoringTimer = new System.Timers.Timer(1000); // Check every second
        _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
    }

    public async Task<ApplicationProfile> GetActiveProfileAsync()
    {
        return _activeProfile ?? await GetDefaultProfileAsync();
    }

    public async Task<IEnumerable<ApplicationProfile>> GetAllProfilesAsync()
    {
        try
        {
            var entities = await _dbContext.ApplicationProfiles
                .AsNoTracking()
                .ToListAsync();

            return entities.Select(MapToModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all application profiles");
            throw;
        }
    }

    public async Task<ApplicationProfile> GetProfileByIdAsync(Guid id)
    {
        try
        {
            var entity = await _dbContext.ApplicationProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity == null)
            {
                throw new InvalidOperationException($"Profile with ID {id} not found");
            }

            return MapToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile by ID {ProfileId}", id);
            throw;
        }
    }

    public async Task<ApplicationProfile> GetProfileForApplicationAsync(string applicationName)
    {
        var profile = await GetProfileAsync(applicationName);
        return profile ?? await GetDefaultProfileAsync();
    }

    public async Task<ApplicationProfile?> GetProfileAsync(string applicationName)
    {
        var cacheKey = $"{CacheKeyPrefix}{applicationName}";

        // Try to get from cache
        if (_cache.TryGetValue<ApplicationProfile>(cacheKey, out var cachedProfile))
        {
            return cachedProfile;
        }

        try
        {
            var entity = await _dbContext.ApplicationProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => 
                    p.ApplicationExecutables.Contains(applicationName) ||
                    p.ProcessNamePatterns.Any(pattern => applicationName.Contains(pattern)) ||
                    p.WindowTitlePatterns.Any(pattern => applicationName.Contains(pattern)));

            if (entity == null)
            {
                return null;
            }

            var profile = MapToModel(entity);

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            _cache.Set(cacheKey, profile, cacheOptions);

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile for application {ApplicationName}", applicationName);
            throw;
        }
    }

    public async Task<ApplicationProfile> SaveProfileAsync(ApplicationProfile profile)
    {
        try
        {
            var entity = await _dbContext.ApplicationProfiles
                .FirstOrDefaultAsync(p => p.Id == profile.Id);

            if (entity == null)
            {
                entity = MapToEntity(profile);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                await _dbContext.ApplicationProfiles.AddAsync(entity);
            }
            else
            {
                UpdateEntity(entity, profile);
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            // Remove from cache
            ClearProfileCache(profile);

            _logger.LogInformation("Saved profile {ProfileName}", profile.Name);
            
            return MapToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile {ProfileName}", profile.Name);
            throw;
        }
    }

    public async Task<bool> DeleteProfileAsync(string applicationName)
    {
        var profile = await GetProfileAsync(applicationName);
        if (profile == null) return false;
        
        return await DeleteProfileAsync(profile.Id);
    }

    public async Task<SpeechEngineType?> GetPreferredEngineAsync(string applicationName)
    {
        var profile = await GetProfileAsync(applicationName);
        return profile?.SpeechRecognitionSettings?.PreferredEngine;
    }

    public async Task<LanguageSettings?> GetLanguageSettingsAsync(string applicationName)
    {
        var profile = await GetProfileAsync(applicationName);
        return new LanguageSettings
        {
            PrimaryLanguage = profile?.SpeechRecognitionSettings?.PrimaryLanguage ?? "en-US",
            SecondaryLanguages = profile?.SpeechRecognitionSettings?.SecondaryLanguages ?? new List<string>(),
            AutoDetectLanguage = profile?.SpeechRecognitionSettings?.AutoDetectLanguage ?? false,
            LanguageWeights = profile?.SpeechRecognitionSettings?.LanguageWeights ?? new Dictionary<string, float>()
        };
    }

    public async Task<ApplicationProfile> CreateProfileAsync(ApplicationProfile profile)
    {
        profile.Id = Guid.NewGuid();
        return await SaveProfileAsync(profile);
    }

    public async Task<ApplicationProfile> UpdateProfileAsync(ApplicationProfile profile)
    {
        return await SaveProfileAsync(profile);
    }

    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        try
        {
            var entity = await _dbContext.ApplicationProfiles
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity != null)
            {
                _dbContext.ApplicationProfiles.Remove(entity);
                await _dbContext.SaveChangesAsync();

                // Remove from cache
                var profile = MapToModel(entity);
                ClearProfileCache(profile);

                _logger.LogInformation("Deleted profile {ProfileName}", entity.Name);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile with ID {ProfileId}", id);
            throw;
        }
    }

    public Task<ApplicationProfile> SetActiveProfileForCurrentApplicationAsync()
    {
        // This would need platform-specific implementation to get the current foreground application
        // For now, return the default profile
        return GetDefaultProfileAsync();
    }

    public async Task<bool> SetActiveProfileAsync(Guid profileId)
    {
        try
        {
            var profile = await GetProfileByIdAsync(profileId);
            _activeProfile = profile;
            ActiveProfileChanged?.Invoke(this, profile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ApplicationProfile> GetDefaultProfileAsync()
    {
        try
        {
            var entity = await _dbContext.ApplicationProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsDefault);

            if (entity != null)
            {
                return MapToModel(entity);
            }

            // Create a default profile if none exists
            var defaultProfile = new ApplicationProfile
            {
                Id = Guid.NewGuid(),
                Name = "Default",
                Description = "Default application profile",
                IsDefault = true,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SpeechRecognitionSettings = new SpeechRecognitionSettings
                {
                    EnableTranscription = true,
                    PreferredEngine = SpeechEngineType.WindowsSpeech,
                    PrimaryLanguage = "en-US"
                }
            };

            return await SaveProfileAsync(defaultProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default profile");
            throw;
        }
    }

    public async Task<bool> SetDefaultProfileAsync(ApplicationProfile profile)
    {
        try
        {
            // Remove default flag from all profiles
            var defaultProfiles = await _dbContext.ApplicationProfiles
                .Where(p => p.IsDefault)
                .ToListAsync();

            foreach (var p in defaultProfiles)
            {
                p.IsDefault = false;
            }

            // Set new default
            var entity = await _dbContext.ApplicationProfiles
                .FirstOrDefaultAsync(p => p.Id == profile.Id);

            if (entity != null)
            {
                entity.IsDefault = true;
                await _dbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default profile {ProfileName}", profile.Name);
            throw;
        }
    }

    public Task StartApplicationMonitoringAsync()
    {
        if (!IsMonitoringActive)
        {
            _monitoringTimer.Start();
            IsMonitoringActive = true;
            _logger.LogInformation("Started application monitoring");
        }
        return Task.CompletedTask;
    }

    public Task StopApplicationMonitoringAsync()
    {
        if (IsMonitoringActive)
        {
            _monitoringTimer.Stop();
            IsMonitoringActive = false;
            _logger.LogInformation("Stopped application monitoring");
        }
        return Task.CompletedTask;
    }

    private void OnMonitoringTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // This would need platform-specific implementation to detect foreground application changes
        // For now, this is a placeholder
    }

    private static ApplicationProfile MapToModel(ApplicationProfileEntity entity)
    {
        return new ApplicationProfile
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsDefault = entity.IsDefault,
            IsEnabled = entity.IsEnabled,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ApplicationExecutables = entity.ApplicationExecutables,
            WindowTitlePatterns = entity.WindowTitlePatterns,
            ProcessNamePatterns = entity.ProcessNamePatterns,
            SpeechRecognitionSettings = entity.SpeechRecognitionSettings,
            HotkeyConfigs = entity.HotkeyConfigs,
            TextProcessingRules = entity.TextProcessingRules
        };
    }

    private static ApplicationProfileEntity MapToEntity(ApplicationProfile profile)
    {
        return new ApplicationProfileEntity
        {
            Id = profile.Id,
            Name = profile.Name,
            Description = profile.Description,
            IsDefault = profile.IsDefault,
            IsEnabled = profile.IsEnabled,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            ApplicationExecutables = profile.ApplicationExecutables,
            WindowTitlePatterns = profile.WindowTitlePatterns,
            ProcessNamePatterns = profile.ProcessNamePatterns,
            SpeechRecognitionSettings = profile.SpeechRecognitionSettings,
            HotkeyConfigs = profile.HotkeyConfigs,
            TextProcessingRules = profile.TextProcessingRules
        };
    }

    private static void UpdateEntity(ApplicationProfileEntity entity, ApplicationProfile profile)
    {
        entity.Name = profile.Name;
        entity.Description = profile.Description;
        entity.IsDefault = profile.IsDefault;
        entity.IsEnabled = profile.IsEnabled;
        entity.ApplicationExecutables = profile.ApplicationExecutables;
        entity.WindowTitlePatterns = profile.WindowTitlePatterns;
        entity.ProcessNamePatterns = profile.ProcessNamePatterns;
        entity.SpeechRecognitionSettings = profile.SpeechRecognitionSettings;
        entity.HotkeyConfigs = profile.HotkeyConfigs;
        entity.TextProcessingRules = profile.TextProcessingRules;
    }

    private void ClearProfileCache(ApplicationProfile profile)
    {
        foreach (var executable in profile.ApplicationExecutables)
        {
            _cache.Remove($"{CacheKeyPrefix}{executable}");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}