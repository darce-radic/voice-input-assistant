using System;
using System.Collections.Generic;

namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Individual vocabulary entry for personalized learning
/// </summary>
public class VocabularyEntry
{
    public string Word { get; set; } = "";
    public string OriginalWord { get; set; } = "";
    public string CorrectedWord { get; set; } = "";
    public string Pronunciation { get; set; } = "";
    public float Frequency { get; set; }
    public List<string> AlternativeSpellings { get; set; } = new();
    public List<string> Contexts { get; set; } = new();
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    public float UserConfidence { get; set; } = 1.0f;
    public double ConfidenceLevel { get; set; }
    public string ApplicationContext { get; set; } = "";
    public string Category { get; set; } = "";
    public string Language { get; set; } = "en";
    public int UsageCount { get; set; }
}

/// <summary>
/// Context pattern for learning text patterns
/// </summary>
public class ContextPattern
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = "";
    public string PrecedingContext { get; set; } = "";
    public string FollowingContext { get; set; } = "";
    public string ExpectedText { get; set; } = "";
    public string ApplicationContext { get; set; } = "";
    public int ObservationCount { get; set; }
    public double ConfidenceLevel { get; set; }
    public string PatternType { get; set; } = "";
    public string Language { get; set; } = "en";
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// User's speech profile and preferences
/// </summary>
public class SpeechProfile
{
    public string UserId { get; set; } = "";
    public string ProfileName { get; set; } = "Default";
    public string PreferredEngine { get; set; } = "";
    public string Language { get; set; } = "en-US";
    public List<string> ApplicationContexts { get; set; } = new();
    public Dictionary<string, object> EngineSettings { get; set; } = new();
    public Dictionary<string, object> AccuracyMetrics { get; set; } = new();
    public bool IsDefault { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Speech characteristics
    public string PreferredAccent { get; set; } = "US";
    public float SpeechRate { get; set; } = 1.0f; // Words per minute relative to average
    public Dictionary<string, float> PhoneticPatterns { get; set; } = new();
    
    // Usage patterns
    public Dictionary<string, int> ApplicationUsage { get; set; } = new();
    public Dictionary<string, int> TopicFrequency { get; set; } = new();
    public Dictionary<SpeechEngineType, float> EnginePreference { get; set; } = new();
    
    // Learning history
    public int TotalCorrections { get; set; }
    public int TotalTranscriptions { get; set; }
    public float ImprovementRate { get; set; }
    public DateTime LastModelUpdate { get; set; } = DateTime.UtcNow;
    
    // Personalization settings
    public bool EnableContextualLearning { get; set; } = true;
    public bool EnableVocabularyExpansion { get; set; } = true;
    public bool EnableAcousticAdaptation { get; set; } = true;
    public float LearningAggression { get; set; } = 0.5f; // 0-1 scale
}

/// <summary>
/// Personalized vocabulary for a user
/// </summary>
public class PersonalizedVocabulary
{
    public string UserId { get; set; } = "";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, VocabularyEntry> Entries { get; set; } = new();
    public Dictionary<string, float> PhraseConfidence { get; set; } = new();
    public Dictionary<string, List<string>> ContextualSynonyms { get; set; } = new();
}

/// <summary>
/// Contextual transcription suggestions
/// </summary>
public class TranscriptionSuggestion
{
    public string OriginalText { get; set; } = "";
    public string SuggestedText { get; set; } = "";
    public float ConfidenceScore { get; set; }
    public string Reason { get; set; } = "";
    public SuggestionType Type { get; set; }
    public string Context { get; set; } = "";
}

/// <summary>
/// Types of transcription suggestions
/// </summary>
public enum SuggestionType
{
    VocabularyCorrection,    // Based on user's vocabulary
    ContextualReplacement,   // Based on application context
    GrammarCorrection,       // Grammar and syntax fixes
    PersonalPhrase,          // User's common phrases
    TechnicalTerm,           // Domain-specific terminology
    ProperNoun               // Names and specific entities
}

/// <summary>
/// Event arguments for learning updates
/// </summary>
public class LearningUpdateEventArgs : EventArgs
{
    public string UserId { get; set; } = "";
    public LearningUpdateType UpdateType { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of learning updates
/// </summary>
public enum LearningUpdateType
{
    VocabularyUpdate,
    ContextUpdate,
    ProfileUpdate,
    ModelRetrain,
    ContextualLearning,
    AccuracyImprovement
}

/// <summary>
/// Learning statistics for a user
/// </summary>
public class LearningStats
{
    public string UserId { get; set; } = "";
    public int TotalCorrections { get; set; }
    public float AccuracyImprovement { get; set; }
    public DateTimeOffset LastUpdateTimestamp { get; set; }
    public Dictionary<string, int> CommonMistakePatterns { get; set; } = new();
    public int VocabularySize { get; set; }
    public int TotalTranscriptions { get; set; }
    public float AverageConfidence { get; set; }
    public Dictionary<string, float> ContextualAccuracy { get; set; } = new();
    public TimeSpan TotalUsageTime { get; set; }
}

/// <summary>
/// User feedback for transcription quality and corrections
/// </summary>
public class UserFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string? SessionId { get; set; }
    public int Rating { get; set; } // 1-5 scale
    public string? Comment { get; set; }
    public string? OriginalText { get; set; }
    public string? CorrectedText { get; set; }
    public string? ApplicationContext { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public FeedbackType Type { get; set; } = FeedbackType.TranscriptionQuality;
    public float? ConfidenceScore { get; set; }
    public string? Engine { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsProcessed { get; set; } = false;
}

/// <summary>
/// Types of user feedback
/// </summary>
public enum FeedbackType
{
    TranscriptionQuality,
    TranscriptionCorrection,
    EnginePreference,
    FeatureRequest,
    BugReport,
    General
}
