using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Represents a user in the system
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public bool IsEmailConfirmed { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// User's role in the system
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;
    
    /// <summary>
    /// User's organization/company
    /// </summary>
    public string Organization { get; set; } = string.Empty;
    
    /// <summary>
    /// User's roles (for multi-role support)
    /// </summary>
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// User's preferences and settings
    /// </summary>
    public Dictionary<string, object> Preferences { get; set; } = new();
    
    /// <summary>
    /// API keys associated with this user
    /// </summary>
    public List<ApiKey> ApiKeys { get; set; } = new();
}

/// <summary>
/// User roles in the system
/// </summary>
public enum UserRole
{
    User,
    Admin,
    SuperAdmin
}