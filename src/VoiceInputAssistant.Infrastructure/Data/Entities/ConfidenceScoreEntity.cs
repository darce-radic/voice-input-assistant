using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Entity for storing confidence analysis results for transcriptions
/// </summary>
public class ConfidenceScoreEntity
{
    /// <summary>
    /// Unique identifier for the confidence score record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the transcription event
    /// </summary>
    public Guid TranscriptionEventId { get; set; }

    /// <summary>
    /// The user who performed the transcription
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// The transcribed text that was analyzed
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Overall confidence score for the entire transcription (0.0-1.0)
    /// </summary>
    public double OverallConfidence { get; set; }

    /// <summary>
    /// Word-level confidence scores in JSON format
    /// </summary>
    public Dictionary<string, double> WordConfidences { get; set; } = new();

    /// <summary>
    /// Words identified as uncertain (below confidence threshold)
    /// </summary>
    public List<string> UncertainWords { get; set; } = new();

    /// <summary>
    /// The speech recognition engine that produced the transcription
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Engine { get; set; } = string.Empty;

    /// <summary>
    /// The application context where the transcription occurred
    /// </summary>
    [MaxLength(256)]
    public string? ApplicationContext { get; set; }

    /// <summary>
    /// The confidence threshold used for analysis
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// Whether user feedback was requested based on this analysis
    /// </summary>
    public bool FeedbackRequested { get; set; } = false;

    /// <summary>
    /// Whether the user provided feedback for this transcription
    /// </summary>
    public bool FeedbackReceived { get; set; } = false;

    /// <summary>
    /// Analysis metadata (e.g., audio quality indicators, noise levels)
    /// </summary>
    public Dictionary<string, object>? AnalysisMetadata { get; set; }

    /// <summary>
    /// When the confidence analysis was performed
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the transcription event
    /// </summary>
    public TranscriptionEventEntity TranscriptionEvent { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public UserEntity? User { get; set; }
}