namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Represents the result of a speech-to-text transcription
/// </summary>
public class TranscriptionResult
{
    /// <summary>
    /// Unique identifier for this transcription result
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The transcribed text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score of the transcription (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// The speech engine that produced this result
    /// </summary>
    public string Engine { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to process the transcription
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Language of the transcribed text
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Whether the transcription was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if transcription failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata about the transcription
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// When the transcription was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}