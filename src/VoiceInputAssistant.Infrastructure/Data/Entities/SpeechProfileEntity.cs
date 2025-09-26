using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Entity for storing user-specific speech profiles and engine preferences
/// </summary>
public class SpeechProfileEntity
{
    /// <summary>
    /// Unique identifier for the speech profile
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user this speech profile belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Name of the profile (e.g., "Work", "Personal", "Meetings")
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the profile
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Preferred speech recognition engine for this profile
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PreferredEngine { get; set; } = string.Empty;

    /// <summary>
    /// Language code for this profile
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Application contexts where this profile should be used
    /// </summary>
    public List<string> ApplicationContexts { get; set; } = new();

    /// <summary>
    /// Engine-specific settings for this profile
    /// </summary>
    public Dictionary<string, object>? EngineSettings { get; set; }

    /// <summary>
    /// Accuracy metrics for this profile
    /// </summary>
    public Dictionary<string, double> AccuracyMetrics { get; set; } = new();

    /// <summary>
    /// Whether this is the default profile for the user
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Whether this profile is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this profile was last used
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata about the profile
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public UserEntity User { get; set; } = null!;
}