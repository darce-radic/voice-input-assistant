using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Entity for storing user-specific vocabulary entries for personalized speech recognition
/// </summary>
public class UserVocabularyEntity
{
    /// <summary>
    /// Unique identifier for the vocabulary entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user this vocabulary entry belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The original word as it was transcribed
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string OriginalWord { get; set; } = string.Empty;

    /// <summary>
    /// The corrected/preferred word
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string CorrectedWord { get; set; } = string.Empty;

    /// <summary>
    /// How many times this correction has been applied
    /// </summary>
    public int UsageCount { get; set; } = 1;

    /// <summary>
    /// The confidence level in this correction (0.0-1.0)
    /// </summary>
    public double ConfidenceLevel { get; set; } = 1.0;

    /// <summary>
    /// The application context where this vocabulary is most relevant
    /// </summary>
    [MaxLength(256)]
    public string? ApplicationContext { get; set; }

    /// <summary>
    /// Category of the vocabulary entry (e.g., "technical_term", "proper_noun", "abbreviation")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Language code for this vocabulary entry
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Additional metadata about the vocabulary entry
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// When this vocabulary entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this vocabulary entry was last used/updated
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this vocabulary entry is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public UserEntity User { get; set; } = null!;
}