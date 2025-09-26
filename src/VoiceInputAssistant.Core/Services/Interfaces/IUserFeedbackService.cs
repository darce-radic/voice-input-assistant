using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for collecting and processing user feedback to improve transcription accuracy
/// </summary>
public interface IUserFeedbackService
{
    /// <summary>
    /// Records a user correction of a transcription
    /// </summary>
    Task<bool> RecordCorrectionAsync(TranscriptionCorrection correction);

    /// <summary>
    /// Records user satisfaction rating for a transcription
    /// </summary>
    Task<bool> RecordRatingAsync(TranscriptionRating rating);

    /// <summary>
    /// Gets correction history for analysis
    /// </summary>
    Task<IEnumerable<TranscriptionCorrection>> GetCorrectionsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        SpeechEngineType? engine = null);

    /// <summary>
    /// Gets user's most commonly corrected words/phrases
    /// </summary>
    Task<IEnumerable<CommonCorrection>> GetCommonCorrectionsAsync(int limit = 50);

    /// <summary>
    /// Gets accuracy metrics for a specific time period
    /// </summary>
    Task<AccuracyMetrics> GetAccuracyMetricsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Suggests corrections based on context and user history
    /// </summary>
    Task<IEnumerable<string>> SuggestCorrectionsAsync(string originalText, string context = "");

    /// <summary>
    /// Event raised when user provides feedback
    /// </summary>
    event EventHandler<UserFeedbackEventArgs> FeedbackReceived;
}

/// <summary>
/// Represents a user correction of a transcription
/// </summary>
public class TranscriptionCorrection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = "";
    public string OriginalText { get; set; } = "";
    public string CorrectedText { get; set; } = "";
    public string ApplicationContext { get; set; } = "";
    public SpeechEngineType Engine { get; set; }
    public float OriginalConfidence { get; set; }
    public CorrectionType Type { get; set; }
    public string AudioFingerprint { get; set; } = ""; // For audio pattern learning
}

/// <summary>
/// Represents user satisfaction rating for a transcription
/// </summary>
public class TranscriptionRating
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = "";
    public string TranscriptionId { get; set; } = "";
    public string TranscribedText { get; set; } = "";
    public int Rating { get; set; } // 1-5 scale
    public string ApplicationContext { get; set; } = "";
    public SpeechEngineType Engine { get; set; }
    public float Confidence { get; set; }
    public string Comments { get; set; } = "";
}

/// <summary>
/// Common correction patterns discovered from user feedback
/// </summary>
public class CommonCorrection
{
    public string OriginalPhrase { get; set; } = "";
    public string CorrectedPhrase { get; set; } = "";
    public int Frequency { get; set; }
    public float ConfidenceImprovement { get; set; }
    public DateTime LastSeen { get; set; }
    public string Context { get; set; } = "";
}

/// <summary>
/// Accuracy metrics for measuring transcription improvement
/// </summary>
public class AccuracyMetrics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTranscriptions { get; set; }
    public int CorrectedTranscriptions { get; set; }
    public float AccuracyRate => TotalTranscriptions > 0 ? 
        (float)(TotalTranscriptions - CorrectedTranscriptions) / TotalTranscriptions : 0f;
    public float AverageConfidence { get; set; }
    public Dictionary<SpeechEngineType, float> EngineAccuracy { get; set; } = new();
    public Dictionary<string, int> CommonMistakes { get; set; } = new();
}

/// <summary>
/// Types of corrections users can make
/// </summary>
public enum CorrectionType
{
    FullReplacement,      // Complete text replacement
    PartialCorrection,    // Fixing specific words/phrases
    Punctuation,          // Adding/fixing punctuation
    Capitalization,       // Fixing capitalization
    VocabularyAddition,   // Teaching new words
    ContextSpecific       // Context-dependent correction
}

/// <summary>
/// Event arguments for user feedback events
/// </summary>
public class UserFeedbackEventArgs : EventArgs
{
    public TranscriptionCorrection? Correction { get; set; }
    public TranscriptionRating? Rating { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}