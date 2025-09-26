using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Represents an API key for authentication
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Key prefix for identification (e.g., "sk_test_", "pk_live_")
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    
    public User? User { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Scopes/permissions for this API key
    /// </summary>
    public List<string> Scopes { get; set; } = new();
    
    /// <summary>
    /// Usage statistics for this API key
    /// </summary>
    public ApiKeyUsageStats UsageStats { get; set; } = new();
}

/// <summary>
/// Usage statistics for an API key
/// </summary>
public class ApiKeyUsageStats
{
    public int TotalRequests { get; set; }
    
    public int RequestsToday { get; set; }
    
    public int RequestsThisMonth { get; set; }
    
    public DateTime? LastRequestAt { get; set; }
    
    public Dictionary<string, int> RequestsByEndpoint { get; set; } = new();
}