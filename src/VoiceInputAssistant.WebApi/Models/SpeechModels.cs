using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.WebApi.Models;

/// <summary>
/// Request model for speech transcription
/// </summary>
public class TranscriptionRequest
{
    /// <summary>
    /// Base64 encoded audio data
    /// </summary>
    [Required]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>
    /// Audio format (wav, mp3, etc.)
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Expected language (optional, for engine optimization)
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Application context for adaptive learning
    /// </summary>
    public string? ApplicationContext { get; set; }
}

/// <summary>
/// Response model for speech transcription
/// </summary>
public class TranscriptionResponse
{
    /// <summary>
    /// Transcribed text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level (0.0 to 1.0)
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Detected language
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Speech engine used
    /// </summary>
    public string? Engine { get; set; }

    /// <summary>
    /// Whether transcription was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if transcription failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Transcription timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Session ID for tracking
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Word count
    /// </summary>
    public int WordCount { get; set; }
}

/// <summary>
/// Request model for transcription correction feedback
/// </summary>
public class TranscriptionCorrectionRequest
{
    /// <summary>
    /// Session ID from original transcription
    /// </summary>
    [Required]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Original transcribed text
    /// </summary>
    [Required]
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// User-corrected text
    /// </summary>
    [Required]
    public string CorrectedText { get; set; } = string.Empty;

    /// <summary>
    /// Application context where correction occurred
    /// </summary>
    public string? ApplicationContext { get; set; }

    /// <summary>
    /// User ID for personalized learning
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Response model for transcription correction feedback
/// </summary>
public class TranscriptionCorrectionResponse
{
    /// <summary>
    /// Whether the correction was successfully processed
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message about the correction processing
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Learning updates that were applied
    /// </summary>
    public List<string> LearningUpdates { get; set; } = new();
}

/// <summary>
/// Request model for user feedback on transcription quality
/// </summary>
public class TranscriptionFeedbackRequest
{
    /// <summary>
    /// Session ID from original transcription
    /// </summary>
    [Required]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// User rating (1-5 scale)
    /// </summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Optional feedback text
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// User ID for feedback tracking
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Response model for transcription feedback submission
/// </summary>
public class TranscriptionFeedbackResponse
{
    /// <summary>
    /// Whether feedback was successfully recorded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}