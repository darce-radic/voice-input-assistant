using System;
using System.Collections.Generic;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Database entity for user information
/// </summary>
public class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Organization { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// User roles as JSON array
    /// </summary>
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// Navigation property for user's API keys
    /// </summary>
    public virtual ICollection<ApiKeyEntity> ApiKeys { get; set; } = new List<ApiKeyEntity>();
}