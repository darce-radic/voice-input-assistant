namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

/// <summary>
/// Registration request model
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Organization { get; set; }
}

/// <summary>
/// Refresh token request model
/// </summary>
public class RefreshTokenRequest
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}

/// <summary>
/// Create user request model
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    public CreateUserRequest() { }

    public CreateUserRequest(string username, string email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }
}

/// <summary>
/// Update user request model
/// </summary>
public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}

/// <summary>
/// Authentication result model
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }

    public AuthenticationResult(bool isSuccess, User? user = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        User = user;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Authentication response model
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

/// <summary>
/// User data transfer object
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Organization { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsEmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

/// <summary>
/// Update profile request model
/// </summary>
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
}

/// <summary>
/// Change password request model
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

/// <summary>
/// Reset password request model
/// </summary>
public class ResetPasswordRequest
{
    public string Email { get; set; } = "";
}

/// <summary>
/// Reset password confirmation request model
/// </summary>
public class ResetPasswordConfirmRequest
{
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>
/// Confirm email request model
/// </summary>
public class ConfirmEmailRequest
{
    public string Token { get; set; } = "";
    public Guid UserId { get; set; }
}

/// <summary>
/// Resend confirmation request model
/// </summary>
public class ResendConfirmationRequest
{
    public string Email { get; set; } = "";
}

/// <summary>
/// Create API key request model
/// </summary>
public class CreateApiKeyRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Scopes { get; set; } = new();
}

/// <summary>
/// API key response model
/// </summary>
public class ApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Key { get; set; } = "";
    public string KeyPrefix { get; set; } = "";
    public List<string> Scopes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}
