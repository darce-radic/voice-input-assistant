using System;
using System.Collections.Generic;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Database entity for API keys
/// </summary>
public class ApiKeyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Key { get; set; } = "";
    public string KeyPrefix { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Scopes as JSON array
    /// </summary>
    public List<string> Scopes { get; set; } = new();
    
    /// <summary>
    /// Foreign key to User
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Navigation property to User
    /// </summary>
    public virtual UserEntity User { get; set; } = null!;
}