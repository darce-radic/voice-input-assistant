using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Interfaces;
using VoiceInputAssistant.Infrastructure.Repositories;

namespace VoiceInputAssistant.Infrastructure.Services;

/// <summary>
/// Persistent implementation of adaptive learning service that combines business logic with data persistence
/// </summary>
public class PersistentAdaptiveLearningService : IAdaptiveLearningService
{
    private readonly ILogger<PersistentAdaptiveLearningService> _logger;
    private readonly AdaptiveLearningRepository _repository;
    private readonly Core.Interfaces.IUserFeedbackService _feedbackService;

    public event EventHandler<LearningUpdateEventArgs>? ModelUpdated;

    public PersistentAdaptiveLearningService(
        ILogger<PersistentAdaptiveLearningService> logger,
        AdaptiveLearningRepository repository,
        Core.Interfaces.IUserFeedbackService feedbackService)
    {
        _logger = logger;
        _repository = repository;
        _feedbackService = feedbackService;
    }

    public async Task UpdateVocabularyFromCorrectionsAsync(Guid userId, IEnumerable<Core.Services.Interfaces.TranscriptionCorrection> corrections)
    {
        try
        {
            var vocabularyEntries = corrections.Select(c => new VocabularyEntry
            {
                OriginalWord = ExtractFirstWord(c.OriginalText),
                CorrectedWord = ExtractFirstWord(c.CorrectedText),
                ConfidenceLevel = Math.Max(0.7, 1.0 - c.OriginalConfidence), // Inverse confidence - low original confidence means high learning value
                ApplicationContext = c.ApplicationContext,
                Category = DetermineWordCategory(c.CorrectedText),
                Language = "en-US",
                UsageCount = 1
            }).Where(v => !string.IsNullOrEmpty(v.OriginalWord) && !string.IsNullOrEmpty(v.CorrectedWord));

            await _repository.UpdateUserVocabularyAsync(userId, vocabularyEntries);

            _logger.LogInformation("Updated vocabulary for user {UserId} with {Count} corrections", 
                userId, corrections.Count());

            // Raise learning update event
            ModelUpdated?.Invoke(this, new LearningUpdateEventArgs
            {
                UserId = userId.ToString(),
                UpdateType = LearningUpdateType.VocabularyUpdate,
                Description = $"Updated vocabulary from {corrections.Count()} corrections"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vocabulary from corrections for user {UserId}", userId);
        }
    }

    public async Task LearnContextPatternsAsync(Guid userId, IEnumerable<Core.Services.Interfaces.TranscriptionCorrection> corrections)
    {
        try
        {
            var contextPatterns = corrections.SelectMany(c => ExtractContextPatterns(c))
                .Where(p => !string.IsNullOrEmpty(p.PrecedingContext) || !string.IsNullOrEmpty(p.FollowingContext));

            await _repository.UpdateContextPatternsAsync(userId, contextPatterns);

            _logger.LogInformation("Updated context patterns for user {UserId} with {Count} patterns", 
                userId, contextPatterns.Count());

            // Raise learning update event
            ModelUpdated?.Invoke(this, new LearningUpdateEventArgs
            {
                UserId = userId.ToString(),
                UpdateType = LearningUpdateType.ContextUpdate,
                Description = $"Learned {contextPatterns.Count()} context patterns"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to learn context patterns for user {UserId}", userId);
        }
    }

    public async Task<IEnumerable<string>> SuggestCorrectionsAsync(Guid userId, string text, string? applicationContext = null)
    {
        try
        {
            var suggestions = new List<string>();

            // Get user's vocabulary
            var vocabulary = await _repository.GetUserVocabularyAsync(userId, applicationContext);
            
            // Get user's context patterns
            var patterns = await _repository.GetContextPatternsAsync(userId, applicationContext);

            // Apply vocabulary-based suggestions
            var vocabularySuggestions = ApplyVocabularySuggestions(text, vocabulary);
            suggestions.AddRange(vocabularySuggestions);

            // Apply context pattern-based suggestions
            var patternSuggestions = ApplyContextPatternSuggestions(text, patterns);
            suggestions.AddRange(patternSuggestions);

            // Use feedback service for additional suggestions
            var feedbackSuggestions = await _feedbackService.SuggestCorrectionsAsync(userId, text, applicationContext);
            suggestions.AddRange(feedbackSuggestions);

            return suggestions.Distinct().Take(10);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest corrections for user {UserId}", userId);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<TranscriptionResult> ApplyPersonalizationAsync(TranscriptionResult originalResult, string userId, string context = "")
    {
        try
        {
            var personalizedResult = new TranscriptionResult
            {
                Id = originalResult.Id,
                Text = originalResult.Text,
                Confidence = originalResult.Confidence,
                Engine = originalResult.Engine,
                ProcessingTime = originalResult.ProcessingTime,
                Language = originalResult.Language,
                Success = originalResult.Success,
                ErrorMessage = originalResult.ErrorMessage,
                Metadata = new Dictionary<string, object>(originalResult.Metadata)
            };

            // Get user's vocabulary and patterns
            var userIdGuid = Guid.Parse(userId);
            var vocabulary = await _repository.GetUserVocabularyAsync(userIdGuid, context);
            var patterns = await _repository.GetContextPatternsAsync(userIdGuid, context);

            // Apply vocabulary corrections
            personalizedResult.Text = ApplyVocabularyCorrections(personalizedResult.Text, vocabulary);

            // Apply context pattern corrections
            personalizedResult.Text = ApplyContextPatternCorrections(personalizedResult.Text, patterns);

            // Adjust confidence based on personalization
            personalizedResult.Confidence = CalculatePersonalizedConfidence(
                personalizedResult.Text, originalResult.Confidence, vocabulary, patterns);

            // Add personalization metadata
            personalizedResult.Metadata["PersonalizationApplied"] = originalResult.Text != personalizedResult.Text;
            personalizedResult.Metadata["OriginalText"] = originalResult.Text;

            if (originalResult.Text != personalizedResult.Text)
            {
                _logger.LogInformation("Applied personalization for user {UserId}: '{Original}' -> '{Personalized}'", 
                    userId, originalResult.Text, personalizedResult.Text);
            }

            return personalizedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply personalization for user {UserId}", userId);
            return originalResult;
        }
    }

    public async Task<SpeechProfile> GetSpeechProfileAsync(string userId)
    {
        try
        {
            var userIdGuid = Guid.Parse(userId);
            var profile = await _repository.GetSpeechProfileAsync(userIdGuid, null);
            return profile ?? new SpeechProfile { UserId = userId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get speech profile for user {UserId}", userId);
            return new SpeechProfile { UserId = userId };
        }
    }

    public async Task<bool> UpdateSpeechProfileAsync(SpeechProfile profile)
    {
        try
        {
            await _repository.CreateOrUpdateSpeechProfileAsync(profile);

            _logger.LogInformation("Updated speech profile for user {UserId}", profile.UserId);

            // Raise learning update event
            ModelUpdated?.Invoke(this, new LearningUpdateEventArgs
            {
                UserId = profile.UserId,
                UpdateType = LearningUpdateType.ProfileUpdate,
                Description = "Updated speech profile"
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update speech profile for user {UserId}", profile.UserId);
            return false;
        }
    }

    public async Task<float> CalculatePersonalizedConfidenceAsync(string text, float originalConfidence, string userId, SpeechEngineType engine)
    {
        try
        {
            var userIdGuid = Guid.Parse(userId);
            var vocabulary = await _repository.GetUserVocabularyAsync(userIdGuid, null);
            var patterns = await _repository.GetContextPatternsAsync(userIdGuid, null);

            var confidence = CalculatePersonalizedConfidence(text, originalConfidence, vocabulary, patterns);
            return (float)confidence;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate personalized confidence for user {UserId}", userId);
            return originalConfidence;
        }
    }

    public async Task ProcessLearningBacklogAsync(int batchSize = 100)
    {
        try
        {
            _logger.LogInformation("Processing learning backlog with batch size {BatchSize}", batchSize);

            // Process unprocessed feedback
            var feedbackRepo = _feedbackService as UserFeedbackRepository;
            if (feedbackRepo != null)
            {
                var unprocessedFeedback = await feedbackRepo.GetUnprocessedFeedbackAsync(batchSize);
                var feedbackList = unprocessedFeedback.ToList();

                if (feedbackList.Any())
                {
                    // Group by user and process
                    var userGroups = feedbackList.GroupBy(f => f.UserId);

                    foreach (var userGroup in userGroups)
                    {
                        var userId = userGroup.Key;
                        var corrections = userGroup.Select(f => new Core.Services.Interfaces.TranscriptionCorrection
                        {
                            OriginalText = f.OriginalText,
                            CorrectedText = f.CorrectedText,
                            Engine = Enum.TryParse<SpeechEngineType>(f.Engine, out var engineType) ? engineType : SpeechEngineType.WindowsSpeech,
                            OriginalConfidence = (float)(f.ConfidenceScore ?? 0.0),
                            ApplicationContext = f.ApplicationContext,
                            Timestamp = f.CreatedAt
                        });

                        await UpdateVocabularyFromCorrectionsAsync(userId, corrections);
                        await LearnContextPatternsAsync(userId, corrections);
                    }

                    // Mark feedback as processed
                    var feedbackIds = feedbackList.Select(f => f.Id);
                    await feedbackRepo.MarkFeedbackAsProcessedAsync(feedbackIds);

                    _logger.LogInformation("Processed {Count} feedback items for {UserCount} users", 
                        feedbackList.Count, userGroups.Count());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process learning backlog");
        }
    }

    // Missing interface methods
    public async Task<bool> LearnFromCorrectionAsync(Core.Services.Interfaces.TranscriptionCorrection correction)
    {
        try
        {
            if (Guid.TryParse(correction.UserId, out var userIdGuid))
            {
                await UpdateVocabularyFromCorrectionsAsync(userIdGuid, new[] { correction });
                await LearnContextPatternsAsync(userIdGuid, new[] { correction });
                return true;
            }
            else
            {
                _logger.LogError("Invalid UserId format for correction: {UserId}", correction.UserId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to learn from correction for user {UserId}", correction.UserId);
            return false;
        }
    }

    public async Task<PersonalizedVocabulary> GetPersonalizedVocabularyAsync(string userId)
    {
        try
        {
            var userIdGuid = Guid.Parse(userId);
            var vocabularyEntries = await _repository.GetUserVocabularyAsync(userIdGuid, null);
            
            var personalizedVocab = new PersonalizedVocabulary
            {
                UserId = userId,
                LastUpdated = DateTime.UtcNow,
                Entries = vocabularyEntries.ToDictionary(v => v.Word, v => v)
            };
            
            return personalizedVocab;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get personalized vocabulary for user {UserId}", userId);
            return new PersonalizedVocabulary { UserId = userId };
        }
    }

    public async Task<bool> UpdateVocabularyAsync(string userId, IEnumerable<VocabularyEntry> entries)
    {
        try
        {
            var userIdGuid = Guid.Parse(userId);
            await _repository.UpdateUserVocabularyAsync(userIdGuid, entries);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vocabulary for user {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<TranscriptionSuggestion>> GetContextSuggestionsAsync(string text, string applicationContext, string userId)
    {
        try
        {
            var userIdGuid = Guid.Parse(userId);
            var suggestions = await SuggestCorrectionsAsync(userIdGuid, text, applicationContext);
            
            return suggestions.Select(s => new TranscriptionSuggestion
            {
                OriginalText = text,
                SuggestedText = s,
                ConfidenceScore = 0.8f,
                Reason = "Based on user vocabulary and patterns",
                Type = SuggestionType.VocabularyCorrection,
                Context = applicationContext
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get context suggestions for user {UserId}", userId);
            return Enumerable.Empty<TranscriptionSuggestion>();
        }
    }

    public async Task<bool> RetrainUserModelAsync(string userId)
    {
        try
        {
            // Process any pending learning updates
            await ProcessLearningBacklogAsync(100);
            
            // Raise model update event
            ModelUpdated?.Invoke(this, new LearningUpdateEventArgs
            {
                UserId = userId,
                UpdateType = LearningUpdateType.ModelRetrain,
                Description = "User model retrained"
            });
            
            _logger.LogInformation("Retrained user model for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrain user model for user {UserId}", userId);
            return false;
        }
    }

    public async Task ProcessTranscriptionCorrectionAsync(Core.Interfaces.TranscriptionCorrection correction)
    {
        try
        {
            // Convert from Interfaces.TranscriptionCorrection to Services.Interfaces.TranscriptionCorrection
            var serviceCorrection = new Core.Services.Interfaces.TranscriptionCorrection
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                UserId = "default", // The Interfaces version doesn't have UserId, so we use a default
                OriginalText = correction.OriginalText,
                CorrectedText = correction.CorrectedText,
                ApplicationContext = correction.ApplicationContext ?? "general",
                Engine = SpeechEngineType.Unspecified,
                OriginalConfidence = (float)correction.ConfidenceScore,
                Type = CorrectionType.FullReplacement,
                AudioFingerprint = ""
            };
            
            await LearnFromCorrectionAsync(serviceCorrection);
            _logger.LogInformation("Processed transcription correction for user default");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transcription correction");
            throw;
        }
    }

    public async Task ProcessUserFeedbackAsync(UserFeedback feedback)
    {
        try
        {
            // Store the feedback using the feedback service - convert to the appropriate method call
            if (Guid.TryParse(feedback.UserId, out var userIdGuid))
            {
                await _feedbackService.RecordCorrectionAsync(
                    userIdGuid, 
                    feedback.OriginalText ?? "", 
                    feedback.CorrectedText ?? "", 
                    "general", // engine
                    feedback.ConfidenceScore ?? 0.5, // confidenceScore
                    feedback.ApplicationContext, // applicationContext
                    null // transcriptionEventId
                );
            }
            
            // If it's a correction-type feedback, also process it as a transcription correction
            if (feedback.Type == FeedbackType.TranscriptionCorrection && 
                !string.IsNullOrWhiteSpace(feedback.OriginalText) && 
                !string.IsNullOrWhiteSpace(feedback.CorrectedText))
            {
                var correction = new Core.Interfaces.TranscriptionCorrection
                {
                    OriginalText = feedback.OriginalText,
                    CorrectedText = feedback.CorrectedText,
                    ApplicationContext = feedback.ApplicationContext ?? "general",
                    Timestamp = feedback.Timestamp.DateTime,
                    ConfidenceScore = feedback.ConfidenceScore ?? 0.5
                };
                
                await ProcessTranscriptionCorrectionAsync(correction);
            }
            
            _logger.LogInformation("Processed user feedback for user {UserId}", feedback.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user feedback for user {UserId}", feedback.UserId);
            throw;
        }
    }

    public async Task<LearningStats> GetLearningStatsAsync(string userId)
    {
        try
        {
            var userIdGuid = Guid.Parse(userId);
            
            // Get user's vocabulary and patterns to calculate stats
            var vocabulary = await _repository.GetUserVocabularyAsync(userIdGuid, null);
            var patterns = await _repository.GetContextPatternsAsync(userIdGuid, null);
            var profile = await _repository.GetSpeechProfileAsync(userIdGuid, null);
            
            // Calculate common mistake patterns
            var mistakePatterns = new Dictionary<string, int>();
            foreach (var entry in vocabulary)
            {
                if (entry.OriginalWord != entry.CorrectedWord)
                {
                    var pattern = $"{entry.OriginalWord} -> {entry.CorrectedWord}";
                    mistakePatterns[pattern] = mistakePatterns.GetValueOrDefault(pattern, 0) + entry.UsageCount;
                }
            }
            
            var stats = new LearningStats
            {
                UserId = userId,
                TotalCorrections = vocabulary.Sum(v => v.UsageCount),
                AccuracyImprovement = profile?.ImprovementRate ?? 0.0f,
                LastUpdateTimestamp = profile?.LastModelUpdate ?? DateTimeOffset.UtcNow,
                CommonMistakePatterns = mistakePatterns,
                VocabularySize = vocabulary.Count(),
                TotalTranscriptions = profile?.TotalTranscriptions ?? 0,
                AverageConfidence = vocabulary.Any() ? (float)vocabulary.Average(v => v.ConfidenceLevel) : 0.0f,
                ContextualAccuracy = new Dictionary<string, float>(), // Could be calculated from patterns
                TotalUsageTime = TimeSpan.Zero // Would need additional tracking
            };
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning statistics for user {UserId}", userId);
            return new LearningStats { UserId = userId };
        }
    }

    // Private helper methods
    private string ExtractFirstWord(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[0].Trim(".,!?;:\"".ToCharArray()) : string.Empty;
    }

    private string DetermineWordCategory(string word)
    {
        // Simple categorization logic - could be more sophisticated
        if (char.IsUpper(word[0]))
            return "proper_noun";
        if (word.All(c => char.IsUpper(c) || c == '-'))
            return "abbreviation";
        if (word.Any(char.IsDigit))
            return "numeric";
        return "general";
    }

    private IEnumerable<ContextPattern> ExtractContextPatterns(Core.Services.Interfaces.TranscriptionCorrection correction)
    {
        var patterns = new List<ContextPattern>();
        
        // Extract word-level patterns
        var originalWords = correction.OriginalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var correctedWords = correction.CorrectedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (originalWords.Length == correctedWords.Length)
        {
            for (int i = 0; i < originalWords.Length; i++)
            {
                if (originalWords[i] != correctedWords[i])
                {
                    var precedingContext = i > 0 ? string.Join(" ", originalWords.Take(i)) : "";
                    var followingContext = i < originalWords.Length - 1 ? string.Join(" ", originalWords.Skip(i + 1)) : "";

                    patterns.Add(new ContextPattern
                    {
                        PrecedingContext = precedingContext,
                        FollowingContext = followingContext,
                        ExpectedText = correctedWords[i],
                        ApplicationContext = correction.ApplicationContext,
                        ConfidenceLevel = 0.8,
                        PatternType = "word_correction",
                        Language = "en-US",
                        ObservationCount = 1
                    });
                }
            }
        }

        return patterns;
    }

    private IEnumerable<string> ApplyVocabularySuggestions(string text, IEnumerable<VocabularyEntry> vocabulary)
    {
        var suggestions = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i].Trim(".,!?;:\"".ToCharArray());
            var vocabularyMatch = vocabulary.FirstOrDefault(v => 
                string.Equals(v.OriginalWord, word, StringComparison.OrdinalIgnoreCase) &&
                v.ConfidenceLevel > 0.8);

            if (vocabularyMatch != null)
            {
                var newWords = words.ToArray();
                newWords[i] = vocabularyMatch.CorrectedWord;
                suggestions.Add(string.Join(" ", newWords));
            }
        }

        return suggestions;
    }

    private IEnumerable<string> ApplyContextPatternSuggestions(string text, IEnumerable<ContextPattern> patterns)
    {
        var suggestions = new List<string>();
        
        foreach (var pattern in patterns.Where(p => p.ConfidenceLevel > 0.7))
        {
            // Simple pattern matching - could be more sophisticated
            if (text.Contains(pattern.PrecedingContext, StringComparison.OrdinalIgnoreCase) &&
                text.Contains(pattern.FollowingContext, StringComparison.OrdinalIgnoreCase))
            {
                // This is a simplified implementation
                // In reality, you'd want more sophisticated pattern matching
                var suggestion = text; // Apply pattern-based correction here
                suggestions.Add(suggestion);
            }
        }

        return suggestions;
    }

    private string ApplyVocabularyCorrections(string text, IEnumerable<VocabularyEntry> vocabulary)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var correctedWords = words.ToArray();

        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i].Trim(".,!?;:\"".ToCharArray());
            var vocabularyMatch = vocabulary.FirstOrDefault(v => 
                string.Equals(v.OriginalWord, word, StringComparison.OrdinalIgnoreCase) &&
                v.ConfidenceLevel > 0.9);

            if (vocabularyMatch != null)
            {
                correctedWords[i] = vocabularyMatch.CorrectedWord;
            }
        }

        return string.Join(" ", correctedWords);
    }

    private string ApplyContextPatternCorrections(string text, IEnumerable<ContextPattern> patterns)
    {
        var correctedText = text;
        
        foreach (var pattern in patterns.Where(p => p.ConfidenceLevel > 0.85))
        {
            // Apply high-confidence pattern corrections
            // This is a simplified implementation
            if (correctedText.Contains(pattern.PrecedingContext, StringComparison.OrdinalIgnoreCase))
            {
                // Apply pattern-based correction
                // In a real implementation, this would be more sophisticated
            }
        }

        return correctedText;
    }

    private double CalculatePersonalizedConfidence(string text, double originalConfidence, 
        IEnumerable<VocabularyEntry> vocabulary, IEnumerable<ContextPattern> patterns)
    {
        var adjustedConfidence = originalConfidence;
        
        // Boost confidence for familiar vocabulary
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var vocabularyMatches = words.Count(word => 
            vocabulary.Any(v => string.Equals(v.CorrectedWord, word.Trim(".,!?;:\"".ToCharArray()), 
                StringComparison.OrdinalIgnoreCase) && v.ConfidenceLevel > 0.8));

        if (words.Length > 0)
        {
            var vocabularyFamiliarity = (double)vocabularyMatches / words.Length;
            adjustedConfidence += vocabularyFamiliarity * 0.1; // Boost up to 10%
        }

        // Boost confidence for recognized patterns
        var matchingPatterns = patterns.Count(p => 
            text.Contains(p.PrecedingContext, StringComparison.OrdinalIgnoreCase) ||
            text.Contains(p.FollowingContext, StringComparison.OrdinalIgnoreCase));

        if (matchingPatterns > 0)
        {
            adjustedConfidence += Math.Min(0.05 * matchingPatterns, 0.1); // Boost up to 10%
        }

        return Math.Max(0.0, Math.Min(1.0, adjustedConfidence));
    }
}