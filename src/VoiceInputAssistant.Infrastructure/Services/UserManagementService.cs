using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Models;
using CoreAuthResult = VoiceInputAssistant.Core.Services.Interfaces.AuthenticationResult;
using CoreCreateUserRequest = VoiceInputAssistant.Core.Services.Interfaces.CreateUserRequest;
using CoreUpdateUserRequest = VoiceInputAssistant.Core.Services.Interfaces.UpdateUserRequest;
using VoiceInputAssistant.Infrastructure.Data;
using VoiceInputAssistant.Infrastructure.Data.Entities;

namespace VoiceInputAssistant.Infrastructure.Services;

/// <summary>
/// User management service implementation using Entity Framework
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext dbContext,
        ILogger<UserManagementService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CoreAuthResult> AuthenticateAsync(string username, string password)
    {
        try
        {
            var user = await GetUserByEmailAsync(username);
            if (user == null)
            {
                return new CoreAuthResult(false, ErrorMessage: "User not found");
            }

            var validatedUser = await ValidateUserCredentialsAsync(username, password);
            if (validatedUser == null)
            {
                return new CoreAuthResult(false, ErrorMessage: "Invalid credentials");
            }

            return new CoreAuthResult(true, validatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {Username}", username);
            return new CoreAuthResult(false, ErrorMessage: "Authentication failed");
        }
    }

public async Task<User> CreateUserAsync(CoreCreateUserRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var userEntity = new UserEntity
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.Username, // Using username as first name for now
                LastName = "",
                PasswordHash = HashPassword(request.Password),
                IsActive = true,
                IsEmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                Roles = new List<string> { "User" }
            };

            _dbContext.Users.Add(userEntity);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created user with email {Email}", request.Email);
            return MapToUser(userEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email {Email}", request.Email);
            throw;
        }
    }

public async Task<User?> GetUserAsync(Guid userId)
    {
        return await GetUserByIdAsync(userId);
    }

public async Task<User> UpdateUserAsync(Guid userId, CoreUpdateUserRequest request)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                user.PasswordHash = HashPassword(request.NewPassword);
            }

            user.UpdatedAt = DateTime.UtcNow;

            // In a real implementation, you would update in database here
            _logger.LogInformation("Updated user {UserId}", userId);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null)
            {
                // Return true even if user doesn't exist to prevent email enumeration
                _logger.LogInformation("Password reset requested for non-existent email {Email}", email);
                return true;
            }

            // Generate reset token and send email
            // This would typically involve the token and email services
            _logger.LogInformation("Password reset initiated for user {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate password reset for {Email}", email);
            return false;
        }
    }

public async Task<User?> ValidateUserCredentialsAsync(string email, string password)
    {
        try
        {
            var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (userEntity == null || !userEntity.IsActive)
            {
                return null;
            }

            var hashedPassword = HashPassword(password);
            if (userEntity.PasswordHash != hashedPassword)
            {
                return null;
            }

            return MapToUser(userEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate credentials for user {Email}", email);
            return null;
        }
    }

public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            return userEntity != null ? MapToUser(userEntity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by email {Email}", email);
            return null;
        }
    }

public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return userEntity != null ? MapToUser(userEntity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        try
        {
            var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userEntity != null)
            {
                userEntity.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("Updated last login time for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update last login for user {UserId}", userId);
        }
    }

    public async Task UpdatePasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userEntity != null)
            {
                userEntity.PasswordHash = HashPassword(newPassword);
                userEntity.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Password updated for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update password for user {UserId}", userId);
        }
    }

    public async Task ConfirmEmailAsync(Guid userId)
    {
        try
        {
            var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userEntity != null)
            {
                userEntity.IsEmailConfirmed = true;
                userEntity.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Email confirmed for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm email for user {UserId}", userId);
        }
    }

    public async Task<List<ApiKey>> GetUserApiKeysAsync(Guid userId)
    {
        try
        {
            var apiKeyEntities = await _dbContext.ApiKeys
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();
            
            return apiKeyEntities.Select(MapToApiKey).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API keys for user {UserId}", userId);
            return new List<ApiKey>();
        }
    }

    public async Task<ApiKey> CreateApiKeyAsync(Guid userId, string name, string description, List<string> scopes)
    {
        try
        {
            var apiKeyEntity = new ApiKeyEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                Description = description,
                Key = GenerateApiKey(),
                KeyPrefix = "vai_",
                Scopes = scopes ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                IsActive = true
            };

            _dbContext.ApiKeys.Add(apiKeyEntity);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Created API key {Name} for user {UserId}", name, userId);
            return MapToApiKey(apiKeyEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API key for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeApiKeyAsync(Guid apiKeyId)
    {
        try
        {
            var apiKeyEntity = await _dbContext.ApiKeys.FirstOrDefaultAsync(a => a.Id == apiKeyId);
            if (apiKeyEntity != null)
            {
                apiKeyEntity.IsActive = false;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Revoked API key {ApiKeyId}", apiKeyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke API key {ApiKeyId}", apiKeyId);
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }

    private static string GenerateApiKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Maps UserEntity to User domain model
    /// </summary>
    private static User MapToUser(UserEntity userEntity)
    {
        return new User
        {
            Id = userEntity.Id,
            Email = userEntity.Email,
            FirstName = userEntity.FirstName,
            LastName = userEntity.LastName,
            Organization = userEntity.Organization,
            PasswordHash = userEntity.PasswordHash,
            IsActive = userEntity.IsActive,
            IsEmailConfirmed = userEntity.IsEmailConfirmed,
            CreatedAt = userEntity.CreatedAt,
            UpdatedAt = userEntity.UpdatedAt,
            LastLoginAt = userEntity.LastLoginAt,
            Roles = userEntity.Roles
        };
    }

    /// <summary>
    /// Maps ApiKeyEntity to ApiKey domain model
    /// </summary>
    private static ApiKey MapToApiKey(ApiKeyEntity apiKeyEntity)
    {
        return new ApiKey
        {
            Id = apiKeyEntity.Id,
            Name = apiKeyEntity.Name,
            Description = apiKeyEntity.Description,
            Key = apiKeyEntity.Key,
            KeyPrefix = apiKeyEntity.KeyPrefix,
            Scopes = apiKeyEntity.Scopes,
            CreatedAt = apiKeyEntity.CreatedAt,
            ExpiresAt = apiKeyEntity.ExpiresAt,
            LastUsedAt = apiKeyEntity.LastUsedAt,
            IsActive = apiKeyEntity.IsActive
        };
    }
}
