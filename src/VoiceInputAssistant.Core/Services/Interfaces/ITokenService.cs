using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for JWT token management
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the user
    /// </summary>
    Task<string> GenerateTokenAsync(Guid userId, IEnumerable<Claim>? additionalClaims = null);
    
    /// <summary>
    /// Validates a JWT token
    /// </summary>
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Refreshes an existing token
    /// </summary>
    Task<string> RefreshTokenAsync(string expiredToken);
    
    /// <summary>
    /// Revokes a token
    /// </summary>
    Task<bool> RevokeTokenAsync(string token);
    
    /// <summary>
    /// Generates an access token for the user
    /// </summary>
    Task<string> GenerateAccessTokenAsync(Models.User user);
    
    /// <summary>
    /// Generates a refresh token for the user
    /// </summary>
    Task<string> GenerateRefreshTokenAsync(Models.User user);
    
    /// <summary>
    /// Validates a refresh token
    /// </summary>
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    Task RevokeRefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Revokes all refresh tokens for a user
    /// </summary>
    Task RevokeAllUserRefreshTokensAsync(Guid userId);
    
    /// <summary>
    /// Gets principal from an expired token
    /// </summary>
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    
    /// <summary>
    /// Generates an email confirmation token
    /// </summary>
    Task<string> GenerateEmailConfirmationTokenAsync(Models.User user);
    
    /// <summary>
    /// Validates an email confirmation token
    /// </summary>
    Task<bool> ValidateEmailConfirmationTokenAsync(string token, Guid userId);
    
    /// <summary>
    /// Generates a password reset token
    /// </summary>
    Task<string> GeneratePasswordResetTokenAsync(Models.User user);
    
    /// <summary>
    /// Validates a password reset token
    /// </summary>
    Task<bool> ValidatePasswordResetTokenAsync(string token, Guid userId);
}

/// <summary>
/// Token validation result
/// </summary>
public record TokenValidationResult(
    bool IsValid,
    Guid? UserId = null,
    IEnumerable<Claim>? Claims = null,
    string? ErrorMessage = null
);