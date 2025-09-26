using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Entity for storing user feedback on transcriptions including corrections and ratings
/// </summary>
public class UserFeedbackEntity
{
    /// <summary>
    /// Unique identifier for the feedback record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who provided the feedback
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Reference to the transcription event that was corrected
    /// </summary>
    public Guid? TranscriptionEventId { get; set; }

    /// <summary>
    /// The original transcribed text
    /// </summary>
    [Required]
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// The corrected text provided by the user
    /// </summary>
    [Required]
    public string CorrectedText { get; set; } = string.Empty;

    /// <summary>
    /// User rating of the transcription quality (1-5 scale)
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// The speech recognition engine that produced the original transcription
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Engine { get; set; } = string.Empty;

    /// <summary>
    /// The confidence score of the original transcription (0.0-1.0)
    /// </summary>
    public double? ConfidenceScore { get; set; }

    /// <summary>
    /// The application context where the transcription occurred
    /// </summary>
    [MaxLength(256)]
    public string? ApplicationContext { get; set; }

    /// <summary>
    /// Type of feedback (Correction, Rating, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FeedbackType { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata about the feedback
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// When the feedback was provided
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this feedback has been processed by the learning system
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public UserEntity User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the transcription event
    /// </summary>
    public TranscriptionEventEntity? TranscriptionEvent { get; set; }
}