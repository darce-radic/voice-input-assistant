using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Entity for storing learned context patterns for improved speech recognition accuracy
/// </summary>
public class ContextPatternEntity
{
    /// <summary>
    /// Unique identifier for the context pattern
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user this context pattern belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The preceding context (words that come before)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string PrecedingContext { get; set; } = string.Empty;

    /// <summary>
    /// The following context (words that come after)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string FollowingContext { get; set; } = string.Empty;

    /// <summary>
    /// The expected word or phrase in this context
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ExpectedText { get; set; } = string.Empty;

    /// <summary>
    /// The application context where this pattern is relevant
    /// </summary>
    [MaxLength(256)]
    public string? ApplicationContext { get; set; }

    /// <summary>
    /// How many times this pattern has been observed
    /// </summary>
    public int ObservationCount { get; set; } = 1;

    /// <summary>
    /// The confidence level in this pattern (0.0-1.0)
    /// </summary>
    public double ConfidenceLevel { get; set; } = 1.0;

    /// <summary>
    /// Pattern type (e.g., "phrase_completion", "word_prediction", "context_correction")
    /// </summary>
    [MaxLength(100)]
    public string PatternType { get; set; } = string.Empty;

    /// <summary>
    /// Language code for this pattern
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Additional metadata about the pattern
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// When this pattern was first learned
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this pattern was last observed/used
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this pattern is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public UserEntity User { get; set; } = null!;
}