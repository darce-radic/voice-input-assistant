using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for adaptive learning and personalization of speech recognition
/// </summary>
public interface IAdaptiveLearningService
{
    /// <summary>
    /// Learns from user corrections and updates personalized models
    /// </summary>
    Task<bool> LearnFromCorrectionAsync(TranscriptionCorrection correction);

    /// <summary>
    /// Gets personalized vocabulary for the user
    /// </summary>
    Task<PersonalizedVocabulary> GetPersonalizedVocabularyAsync(string userId);

    /// <summary>
    /// Updates user's vocabulary with new words/phrases
    /// </summary>
    Task<bool> UpdateVocabularyAsync(string userId, IEnumerable<VocabularyEntry> entries);

    /// <summary>
    /// Gets context-aware suggestions for improving transcription
    /// </summary>
    Task<IEnumerable<TranscriptionSuggestion>> GetContextSuggestionsAsync(
        string text, 
        string applicationContext, 
        string userId);

    /// <summary>
    /// Applies personalized corrections to transcription result
    /// </summary>
    Task<TranscriptionResult> ApplyPersonalizationAsync(
        TranscriptionResult originalResult, 
        string userId, 
        string context = "");

    /// <summary>
    /// Gets user's speech patterns and preferences
    /// </summary>
    Task<SpeechProfile> GetSpeechProfileAsync(string userId);

    /// <summary>
    /// Updates user's speech profile based on usage patterns
    /// </summary>
    Task<bool> UpdateSpeechProfileAsync(SpeechProfile profile);

    /// <summary>
    /// Gets confidence adjustments based on user history
    /// </summary>
    Task<float> CalculatePersonalizedConfidenceAsync(
        string text, 
        float originalConfidence, 
        string userId, 
        SpeechEngineType engine);

    /// <summary>
    /// Triggers model retraining based on accumulated user data
    /// </summary>
    Task<bool> RetrainUserModelAsync(string userId);

    /// <summary>
    /// Process transcription correction and update learning models
    /// </summary>
    Task ProcessTranscriptionCorrectionAsync(VoiceInputAssistant.Core.Interfaces.TranscriptionCorrection correction);

    /// <summary>
    /// Process user feedback and update learning models
    /// </summary>
    Task ProcessUserFeedbackAsync(UserFeedback feedback);

    /// <summary>
    /// Get learning statistics for a user
    /// </summary>
    Task<LearningStats> GetLearningStatsAsync(string userId);

    /// <summary>
    /// Event raised when learning model is updated
    /// </summary>
    event EventHandler<LearningUpdateEventArgs> ModelUpdated;
}

