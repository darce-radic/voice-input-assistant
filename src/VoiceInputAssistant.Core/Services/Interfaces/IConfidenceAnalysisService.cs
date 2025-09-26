using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for analyzing transcription confidence and identifying areas needing improvement
/// </summary>
public interface IConfidenceAnalysisService
{
    /// <summary>
    /// Analyzes confidence levels and identifies uncertain segments
    /// </summary>
    Task<ConfidenceAnalysisResult> AnalyzeConfidenceAsync(TranscriptionResult result, string userId = "");

    /// <summary>
    /// Determines if transcription needs user feedback based on confidence analysis
    /// </summary>
    Task<bool> ShouldRequestFeedbackAsync(TranscriptionResult result, string userId = "");

    /// <summary>
    /// Gets confidence thresholds for different scenarios
    /// </summary>
    Task<ConfidenceThresholds> GetConfidenceThresholdsAsync(string context, string userId = "");

    /// <summary>
    /// Updates confidence thresholds based on user feedback patterns
    /// </summary>
    Task<bool> UpdateConfidenceThresholdsAsync(string userId, ConfidenceThresholds thresholds);

    /// <summary>
    /// Calculates quality score for a transcription
    /// </summary>
    Task<float> CalculateQualityScoreAsync(TranscriptionResult result, string userId = "");

    /// <summary>
    /// Gets words/phrases with historically low confidence
    /// </summary>
    Task<IEnumerable<LowConfidencePattern>> GetLowConfidencePatternsAsync(
        string userId, 
        int daysBack = 30, 
        float thresholdBelow = 0.7f);

    /// <summary>
    /// Predicts confidence for text based on historical patterns
    /// </summary>
    Task<float> PredictConfidenceAsync(string text, string engine, string userId = "");

    /// <summary>
    /// Event raised when low confidence is detected
    /// </summary>
    event EventHandler<LowConfidenceEventArgs> LowConfidenceDetected;
}

/// <summary>
/// Result of confidence analysis
/// </summary>
public class ConfidenceAnalysisResult
{
    public string TranscriptionId { get; set; } = "";
    public float OverallConfidence { get; set; }
    public float QualityScore { get; set; }
    public bool NeedsFeedback { get; set; }
    public string FeedbackReason { get; set; } = "";
    
    public List<ConfidenceSegment> Segments { get; set; } = new();
    public List<UncertainWord> UncertainWords { get; set; } = new();
    public List<ConfidenceFlag> Flags { get; set; } = new();
    
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Confidence analysis for a text segment
/// </summary>
public class ConfidenceSegment
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public ConfidenceLevel Level { get; set; }
    public List<string> AlternativeCandidates { get; set; } = new();
}

/// <summary>
/// Word with uncertain transcription
/// </summary>
public class UncertainWord
{
    public string Word { get; set; } = "";
    public int Position { get; set; }
    public float Confidence { get; set; }
    public List<string> Alternatives { get; set; } = new();
    public UncertaintyReason Reason { get; set; }
    public bool IsNewWord { get; set; }
}

/// <summary>
/// Confidence thresholds for different scenarios
/// </summary>
public class ConfidenceThresholds
{
    public string UserId { get; set; } = "";
    public string Context { get; set; } = "";
    
    public float MinimumAcceptable { get; set; } = 0.6f;      // Below this = definitely needs review
    public float RequestFeedback { get; set; } = 0.75f;       // Below this = ask for feedback
    public float HighConfidence { get; set; } = 0.9f;         // Above this = very confident
    
    public float CriticalWordThreshold { get; set; } = 0.8f;  // For important words
    public float TechnicalTermThreshold { get; set; } = 0.85f; // For technical terms
    public float ProperNounThreshold { get; set; } = 0.8f;    // For names/places
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public int SampleSize { get; set; } // Number of corrections used to calculate thresholds
}

/// <summary>
/// Pattern of historically low confidence words/phrases
/// </summary>
public class LowConfidencePattern
{
    public string Pattern { get; set; } = "";
    public float AverageConfidence { get; set; }
    public int Frequency { get; set; }
    public DateTime LastSeen { get; set; }
    public List<string> CommonMisrecognitions { get; set; } = new();
    public string RecommendedAction { get; set; } = "";
}

/// <summary>
/// Confidence level categories
/// </summary>
public enum ConfidenceLevel
{
    VeryLow,    // 0.0 - 0.4
    Low,        // 0.4 - 0.6
    Medium,     // 0.6 - 0.8
    High,       // 0.8 - 0.9
    VeryHigh    // 0.9 - 1.0
}

/// <summary>
/// Reasons for uncertainty in transcription
/// </summary>
public enum UncertaintyReason
{
    LowAcousticClarity,     // Audio quality issues
    UnknownWord,            // Word not in vocabulary
    AmbiguousPhonetics,     // Similar sounding words
    ContextualMismatch,     // Word doesn't fit context
    MultipleAlternatives,   // Several possible interpretations
    TechnicalJargon,        // Specialized terminology
    ProperNoun,             // Names or places
    BackgroundNoise,        // Audio interference
    SpeakingTooFast,        // Rapid speech
    Accent                  // Accent-related uncertainty
}

/// <summary>
/// Flags raised during confidence analysis
/// </summary>
public class ConfidenceFlag
{
    public ConfidenceFlagType Type { get; set; }
    public string Description { get; set; } = "";
    public float Severity { get; set; } // 0-1 scale
    public string Recommendation { get; set; } = "";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of confidence flags
/// </summary>
public enum ConfidenceFlagType
{
    LowOverallConfidence,
    InconsistentSegments,
    UnknownTerminology,
    ContextualMismatch,
    QualityThresholdViolation,
    PatternDeviation,
    HistoricallyProblematic
}

/// <summary>
/// Event arguments for low confidence detection
/// </summary>
public class LowConfidenceEventArgs : EventArgs
{
    public string TranscriptionId { get; set; } = "";
    public string UserId { get; set; } = "";
    public ConfidenceAnalysisResult Analysis { get; set; } = new();
    public bool RequiresImmediateAttention { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}