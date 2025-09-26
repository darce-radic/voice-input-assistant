using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for managing application profiles
    /// </summary>
    public interface IApplicationProfileService
    {
        /// <summary>
        /// Event raised when the active profile changes
        /// </summary>
        event EventHandler<ApplicationProfile> ActiveProfileChanged;

        /// <summary>
        /// Gets the currently active profile
        /// </summary>
        /// <returns>The active profile or null if none is set</returns>
        Task<ApplicationProfile> GetActiveProfileAsync();

        /// <summary>
        /// Gets all available application profiles
        /// </summary>
        /// <returns>Collection of all profiles</returns>
        Task<IEnumerable<ApplicationProfile>> GetAllProfilesAsync();

        /// <summary>
        /// Gets a profile by its ID
        /// </summary>
        /// <param name="id">Profile ID</param>
        /// <returns>The profile or null if not found</returns>
        Task<ApplicationProfile> GetProfileByIdAsync(Guid id);

        /// <summary>
        /// Gets a profile that matches the specified application
        /// </summary>
        /// <param name="applicationName">Name or path of the application</param>
        /// <returns>The matching profile or null if not found</returns>
        Task<ApplicationProfile> GetProfileForApplicationAsync(string applicationName);

        /// <summary>
        /// Gets a profile by application name (helper)
        /// </summary>
        Task<ApplicationProfile?> GetProfileAsync(string applicationName);

        /// <summary>
        /// Save a profile (create or update based on Id)
        /// </summary>
        Task<ApplicationProfile> SaveProfileAsync(ApplicationProfile profile);

        /// <summary>
        /// Delete a profile by application name
        /// </summary>
        Task<bool> DeleteProfileAsync(string applicationName);

        /// <summary>
        /// Get preferred engine for an application
        /// </summary>
        Task<SpeechEngineType?> GetPreferredEngineAsync(string applicationName);

        /// <summary>
        /// Get language settings for an application
        /// </summary>
        Task<LanguageSettings?> GetLanguageSettingsAsync(string applicationName);

        /// <summary>
        /// Creates a new application profile
        /// </summary>
        /// <param name="profile">Profile to create</param>
        /// <returns>The created profile with assigned ID</returns>
        Task<ApplicationProfile> CreateProfileAsync(ApplicationProfile profile);

        /// <summary>
        /// Updates an existing application profile
        /// </summary>
        /// <param name="profile">Profile to update</param>
        /// <returns>The updated profile</returns>
        Task<ApplicationProfile> UpdateProfileAsync(ApplicationProfile profile);

        /// <summary>
        /// Deletes an application profile
        /// </summary>
        /// <param name="id">ID of the profile to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteProfileAsync(Guid id);

        /// <summary>
        /// Sets the active profile based on the currently focused application
        /// </summary>
        /// <returns>The profile that was activated, or null if no matching profile</returns>
        Task<ApplicationProfile> SetActiveProfileForCurrentApplicationAsync();

        /// <summary>
        /// Manually sets the active profile
        /// </summary>
        /// <param name="profileId">ID of the profile to activate</param>
        /// <returns>True if the profile was activated successfully</returns>
        Task<bool> SetActiveProfileAsync(Guid profileId);

        /// <summary>
        /// Gets the default profile used when no specific profile matches
        /// </summary>
        /// <returns>The default profile</returns>
        Task<ApplicationProfile> GetDefaultProfileAsync();

        /// <summary>
        /// Sets the default profile
        /// </summary>
        /// <param name="profile">Profile to use as default</param>
        /// <returns>True if set successfully</returns>
        Task<bool> SetDefaultProfileAsync(ApplicationProfile profile);

        /// <summary>
        /// Starts monitoring for application focus changes to automatically switch profiles
        /// </summary>
        Task StartApplicationMonitoringAsync();

        /// <summary>
        /// Stops monitoring for application focus changes
        /// </summary>
        Task StopApplicationMonitoringAsync();

        /// <summary>
        /// Gets whether application monitoring is currently active
        /// </summary>
        bool IsMonitoringActive { get; }
    }
}