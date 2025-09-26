using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Interfaces;

/// <summary>
/// Service for collecting and processing user feedback to improve transcription accuracy
/// </summary>
public interface IUserFeedbackService
{
    /// <summary>
    /// Records a user correction of a transcription
    /// </summary>
    Task RecordCorrectionAsync(Guid userId, string originalText, string correctedText, string engine, double? confidenceScore = null, string? applicationContext = null, Guid? transcriptionEventId = null);

    /// <summary>
    /// Records user satisfaction rating for a transcription
    /// </summary>
    Task RecordRatingAsync(Guid userId, string transcribedText, int rating, string engine, string? applicationContext = null, Guid? transcriptionEventId = null);

    /// <summary>
    /// Gets recent corrections for the user
    /// </summary>
    Task<IEnumerable<TranscriptionCorrection>> GetRecentCorrectionsAsync(Guid userId, int limit = 50);

    /// <summary>
    /// Gets user's most commonly corrected words/phrases
    /// </summary>
    Task<IEnumerable<TranscriptionCorrection>> GetCommonCorrectionsAsync(Guid userId, string? applicationContext = null, int limit = 100);

    /// <summary>
    /// Gets accuracy metrics for a user with a specific engine
    /// </summary>
    Task<double> GetAccuracyMetricAsync(Guid userId, string engine, string? applicationContext = null, DateTime? since = null);

    /// <summary>
    /// Suggests corrections based on user history and context
    /// </summary>
    Task<IEnumerable<string>> SuggestCorrectionsAsync(Guid userId, string text, string? applicationContext = null);
}

/// <summary>
/// Represents a user correction of a transcription
/// </summary>
public class TranscriptionCorrection
{
    public string OriginalText { get; set; } = string.Empty;
    public string CorrectedText { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string? ApplicationContext { get; set; }
    public DateTime Timestamp { get; set; }
}