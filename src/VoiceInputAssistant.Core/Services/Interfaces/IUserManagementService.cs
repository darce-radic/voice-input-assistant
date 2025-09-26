using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for user management and authentication
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);
    
    /// <summary>
    /// Creates a new user account
    /// </summary>
    Task<User> CreateUserAsync(CreateUserRequest request);
    
    /// <summary>
    /// Gets user by ID
    /// </summary>
    Task<User?> GetUserAsync(Guid userId);
    
    /// <summary>
    /// Updates user information
    /// </summary>
    Task<User> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    
    /// <summary>
    /// Resets user password
    /// </summary>
    Task<bool> ResetPasswordAsync(string email);
    
    /// <summary>
    /// Validates user credentials for authentication
    /// </summary>
    Task<Models.User?> ValidateUserCredentialsAsync(string email, string password);
    
    /// <summary>
    /// Gets user by email address
    /// </summary>
    Task<Models.User?> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// Gets user by ID
    /// </summary>
    Task<Models.User?> GetUserByIdAsync(Guid userId);
    
    /// <summary>
    /// Updates user's last login time
    /// </summary>
    Task UpdateLastLoginAsync(Guid userId);
    
    /// <summary>
    /// Updates user's password
    /// </summary>
    Task UpdatePasswordAsync(Guid userId, string newPasswordHash);
    
    /// <summary>
    /// Confirms user's email address
    /// </summary>
    Task ConfirmEmailAsync(Guid userId);
    
    /// <summary>
    /// Gets user's API keys
    /// </summary>
    Task<List<ApiKey>> GetUserApiKeysAsync(Guid userId);
    
    /// <summary>
    /// Creates a new API key for user
    /// </summary>
    Task<ApiKey> CreateApiKeyAsync(Guid userId, string name, string description, List<string> scopes);
    
    /// <summary>
    /// Revokes an API key
    /// </summary>
    Task RevokeApiKeyAsync(Guid apiKeyId);
}

/// <summary>
/// Authentication result
/// </summary>
public record AuthenticationResult(
    bool Success,
    Models.User? User = null,
    string? ErrorMessage = null
);

/// <summary>
/// Create user request
/// </summary>
public record CreateUserRequest(
    string Username,
    string Email,
    string Password
);

/// <summary>
/// Update user request
/// </summary>
public record UpdateUserRequest(
    string? Email = null,
    string? NewPassword = null
);
