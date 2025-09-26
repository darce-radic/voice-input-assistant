using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.WebApi.Models
{
    /// <summary>
    /// User login request model
    /// </summary>
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// User registration request model
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string? Organization { get; set; }
    }

    /// <summary>
    /// Password change request model
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Password reset request model
    /// </summary>
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Password reset confirmation model
    /// </summary>
    public class ResetPasswordConfirmRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string ResetToken { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Authentication response model
    /// </summary>
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
    }

    /// <summary>
    /// Token refresh request model
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// User data transfer object
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Organization { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsEmailConfirmed { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
    }

    /// <summary>
    /// User profile update request
    /// </summary>
    public class UpdateProfileRequest
    {
        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string? Organization { get; set; }
    }

    /// <summary>
    /// API key request model
    /// </summary>
    public class CreateApiKeyRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public List<string> Scopes { get; set; } = new();
    }

    /// <summary>
    /// API key response model
    /// </summary>
    public class ApiKeyResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Key { get; set; } = string.Empty; // Only returned on creation
        public string KeyPrefix { get; set; } = string.Empty; // Always returned for identification
        public List<string> Scopes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// User roles enumeration
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string ApiUser = "ApiUser";
        public const string ReadOnly = "ReadOnly";
    }

    /// <summary>
    /// API key scopes enumeration
    /// </summary>
    public static class ApiKeyScopes
    {
        public const string ReadStatus = "read:status";
        public const string ReadAnalytics = "read:analytics";
        public const string ReadHistory = "read:history";
        public const string ReadProfiles = "read:profiles";
        public const string WriteProfiles = "write:profiles";
        public const string ReadSettings = "read:settings";
        public const string WriteSettings = "write:settings";
        public const string ControlRecognition = "control:recognition";
        public const string ProcessText = "process:text";
        public const string InjectText = "inject:text";
        public const string FullAccess = "full:access";
    }

    /// <summary>
    /// Email confirmation request
    /// </summary>
    public class ConfirmEmailRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resend email confirmation request
    /// </summary>
    public class ResendConfirmationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}