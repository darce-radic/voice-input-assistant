using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services;

/// <summary>
/// Implementation of application profile service
/// </summary>
public class ApplicationProfileService : IApplicationProfileService
{
    private readonly ILogger<ApplicationProfileService> _logger;
    private readonly List<ApplicationProfile> _profiles;
    private ApplicationProfile? _activeProfile;

    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
    public event EventHandler<ApplicationProfile>? ActiveProfileChanged;

    public ApplicationProfileService(ILogger<ApplicationProfileService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profiles = new List<ApplicationProfile>();
        
        // Add default profile
        var defaultProfile = new ApplicationProfile 
        { 
            Id = Guid.NewGuid(), 
            Name = "Default", 
            Description = "Default application profile" 
        };
        _profiles.Add(defaultProfile);
        _activeProfile = defaultProfile; // Set as active by default
    }

    public async Task<ApplicationProfile?> GetActiveProfileAsync()
    {
        await Task.Delay(10);
        return _activeProfile;
    }

    public async Task SetActiveProfileAsync(ApplicationProfile profile)
    {
        await Task.Delay(10);
        _activeProfile = profile;
        OnProfileChanged(profile, ProfileChangeType.Updated);
        ActiveProfileChanged?.Invoke(this, profile);
    }

    public async Task<List<ApplicationProfile>> GetAllProfilesAsync()
    {
        await Task.Delay(10);
        return _profiles.ToList();
    }

    public async Task<ApplicationProfile?> GetProfileByIdAsync(Guid id)
    {
        await Task.Delay(10);
        return _profiles.FirstOrDefault(p => p.Id == id);
    }

    public async Task<ApplicationProfile?> FindMatchingProfileAsync(ActiveWindowInfo windowInfo)
    {
        await Task.Delay(10);
        
        // Simple matching logic - can be enhanced later
        var matchingProfile = _profiles.FirstOrDefault(p => 
            p.Name.Contains(windowInfo.ProcessName, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains(windowInfo.Title, StringComparison.OrdinalIgnoreCase));

        return matchingProfile ?? _profiles.FirstOrDefault(); // Return default if no match
    }

    public async Task<ApplicationProfile> SaveProfileAsync(ApplicationProfile profile)
    {
        await Task.Delay(10);
        
        var existingProfile = _profiles.FirstOrDefault(p => p.Id == profile.Id);
        if (existingProfile != null)
        {
            // Update existing
            var index = _profiles.IndexOf(existingProfile);
            _profiles[index] = profile;
        }
        else
        {
            // Add new
            _profiles.Add(profile);
        }

        OnProfileChanged(profile, ProfileChangeType.Updated);
        return profile;
    }

    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        await Task.Delay(10);
        
        var profile = _profiles.FirstOrDefault(p => p.Id == id);
        if (profile != null)
        {
            _profiles.Remove(profile);
            OnProfileChanged(profile, ProfileChangeType.Deleted);
            return true;
        }
        return false;
    }

    public async Task<bool> ExportProfilesAsync(string filePath, Guid[]? profileIds = null)
    {
        await Task.Delay(100);
        _logger.LogInformation("Exporting profiles to: {FilePath}", filePath);
        
        // TODO: Implement actual export to file
        return true;
    }

    public async Task<int> ImportProfilesAsync(string filePath, ProfileMergeStrategy mergeStrategy = ProfileMergeStrategy.Skip)
    {
        await Task.Delay(100);
        _logger.LogInformation("Importing profiles from: {FilePath}", filePath);
        
        // TODO: Implement actual import from file
        return 0;
    }

    private void OnProfileChanged(ApplicationProfile profile, ProfileChangeType changeType)
    {
        try
        {
            var eventArgs = new ProfileChangedEventArgs
            {
                Profile = profile,
                ChangeType = changeType
            };

            ProfileChanged?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing profile changed event");
        }
    }
}