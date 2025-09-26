using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Models;
using CoreResult = VoiceInputAssistant.Core.Services.Interfaces.TokenValidationResult;

namespace VoiceInputAssistant.Infrastructure.Services;

/// <summary>
/// JWT-based token service implementation
/// </summary>
public class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;
    private readonly TokenSettings _tokenSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    // In-memory storage for refresh tokens - in production, use database
    private readonly Dictionary<string, RefreshTokenData> _refreshTokens = new();
    private readonly Dictionary<string, EmailConfirmationTokenData> _emailTokens = new();
    private readonly Dictionary<string, PasswordResetTokenData> _passwordResetTokens = new();

    public TokenService(
        ILogger<TokenService> logger,
        IOptions<TokenSettings> tokenSettings)
    {
        _logger = logger;
        _tokenSettings = tokenSettings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _tokenSettings.Issuer,
            ValidAudience = _tokenSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero // Remove delay of token when expire
        };
    }

    public async Task<string> GenerateTokenAsync(Guid userId, IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _tokenSettings.Issuer,
            audience: _tokenSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        var tokenString = _tokenHandler.WriteToken(token);
        _logger.LogDebug("Generated access token for user {UserId}", userId);

        return await Task.FromResult(tokenString);
    }

public async Task<CoreResult> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out SecurityToken validatedToken);
            
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return await Task.FromResult(new CoreResult(
                    IsValid: true,
                    UserId: userId,
                    Claims: principal.Claims
                ));
            }

            return await Task.FromResult(new CoreResult(IsValid: false, ErrorMessage: "Invalid user ID in token"));
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return await Task.FromResult(new CoreResult(IsValid: false, ErrorMessage: ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return await Task.FromResult(new CoreResult(IsValid: false, ErrorMessage: "Token validation failed"));
        }
    }

    public async Task<string> RefreshTokenAsync(string expiredToken)
    {
        var principal = GetPrincipalFromExpiredToken(expiredToken);
        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
        
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return await GenerateTokenAsync(userId, principal.Claims.Where(c => c.Type != JwtRegisteredClaimNames.Sub));
        }

        throw new SecurityTokenException("Invalid token");
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        // For JWT tokens, we can add them to a blacklist
        // For now, we'll just log the revocation
        _logger.LogInformation("Token revoked");
        return await Task.FromResult(true);
    }

public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("userId", user.Id.ToString())
        };

        // Add role claims
        if (user.Roles != null)
        {
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        return await GenerateTokenAsync(user.Id, claims);
    }

public async Task<string> GenerateRefreshTokenAsync(User user)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        // Store refresh token data
        _refreshTokens[refreshToken] = new RefreshTokenData(
            user.Id,
            DateTime.UtcNow.AddDays(_tokenSettings.RefreshTokenExpirationDays),
            user.Email
        );

        _logger.LogDebug("Generated refresh token for user {UserId}", user.Id);
        return await Task.FromResult(refreshToken);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var tokenData))
        {
            var isValid = tokenData.ExpiresAt > DateTime.UtcNow;
            if (!isValid)
            {
                _refreshTokens.Remove(refreshToken);
            }
            return await Task.FromResult(isValid);
        }

        return await Task.FromResult(false);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        _refreshTokens.Remove(refreshToken);
        _logger.LogDebug("Revoked refresh token");
        await Task.CompletedTask;
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId)
    {
        var tokensToRemove = _refreshTokens
            .Where(kvp => kvp.Value.UserId == userId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in tokensToRemove)
        {
            _refreshTokens.Remove(token);
        }

        _logger.LogDebug("Revoked all refresh tokens for user {UserId}", userId);
        await Task.CompletedTask;
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var validationParameters = _validationParameters.Clone();
        validationParameters.ValidateLifetime = false; // Don't validate expiration

        try
        {
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get principal from expired token");
            throw new SecurityTokenException("Invalid token");
        }
    }

public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        var token = Guid.NewGuid().ToString();
        _emailTokens[token] = new EmailConfirmationTokenData(
            user.Id,
            DateTime.UtcNow.AddHours(24), // 24 hours expiration
            user.Email
        );

        _logger.LogDebug("Generated email confirmation token for user {UserId}", user.Id);
        return await Task.FromResult(token);
    }

    public async Task<bool> ValidateEmailConfirmationTokenAsync(string token, Guid userId)
    {
        if (_emailTokens.TryGetValue(token, out var tokenData))
        {
            var isValid = tokenData.UserId == userId && tokenData.ExpiresAt > DateTime.UtcNow;
            if (isValid)
            {
                _emailTokens.Remove(token); // One-time use
            }
            return await Task.FromResult(isValid);
        }

        return await Task.FromResult(false);
    }

public async Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        var token = Guid.NewGuid().ToString();
        _passwordResetTokens[token] = new PasswordResetTokenData(
            user.Id,
            DateTime.UtcNow.AddHours(2), // 2 hours expiration
            user.Email
        );

        _logger.LogDebug("Generated password reset token for user {UserId}", user.Id);
        return await Task.FromResult(token);
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token, Guid userId)
    {
        if (_passwordResetTokens.TryGetValue(token, out var tokenData))
        {
            var isValid = tokenData.UserId == userId && tokenData.ExpiresAt > DateTime.UtcNow;
            if (isValid)
            {
                _passwordResetTokens.Remove(token); // One-time use
            }
            return await Task.FromResult(isValid);
        }

        return await Task.FromResult(false);
    }
}

/// <summary>
/// Token service configuration settings
/// </summary>
public class TokenSettings
{
    public string SecretKey { get; set; } = "VoiceInputAssistant_SuperSecretKey_2024_MinimumLength32Characters!";
    public string Issuer { get; set; } = "VoiceInputAssistant";
    public string Audience { get; set; } = "VoiceInputAssistant";
    public int AccessTokenExpirationMinutes { get; set; } = 60; // 1 hour
    public int RefreshTokenExpirationDays { get; set; } = 30; // 30 days
}

/// <summary>
/// Refresh token data structure
/// </summary>
internal record RefreshTokenData(Guid UserId, DateTime ExpiresAt, string UserEmail);

/// <summary>
/// Email confirmation token data structure
/// </summary>
internal record EmailConfirmationTokenData(Guid UserId, DateTime ExpiresAt, string UserEmail);

/// <summary>
/// Password reset token data structure
/// </summary>
internal record PasswordResetTokenData(Guid UserId, DateTime ExpiresAt, string UserEmail);