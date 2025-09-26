using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// Implementation of adaptive learning service for personalizing speech recognition
/// </summary>
public class AdaptiveLearningService : IAdaptiveLearningService
{
    private readonly ILogger<AdaptiveLearningService> _logger;
    private readonly IUserFeedbackService _feedbackService;
    private readonly Dictionary<string, PersonalizedVocabulary> _userVocabularies = new();
    private readonly Dictionary<string, SpeechProfile> _userProfiles = new();

    public event EventHandler<LearningUpdateEventArgs>? ModelUpdated;

    public AdaptiveLearningService(
        ILogger<AdaptiveLearningService> logger, 
        IUserFeedbackService feedbackService)
    {
        _logger = logger;
        _feedbackService = feedbackService;
    }

    public async Task<bool> LearnFromCorrectionAsync(TranscriptionCorrection correction)
    {
        try
        {
            var userId = correction.UserId;
            
            // Update user vocabulary
            await UpdateUserVocabularyFromCorrection(correction);
            
            // Update speech profile
            await UpdateSpeechProfileFromCorrection(correction);
            
            // Learn contextual patterns
            await LearnContextualPatterns(correction);
            
            _logger.LogInformation("Learned from correction for user {UserId}: '{Original}' -> '{Corrected}'", 
                userId, correction.OriginalText, correction.CorrectedText);

            // Raise learning update event
            ModelUpdated?.Invoke(this, new LearningUpdateEventArgs
            {
                UserId = userId,
                UpdateType = LearningUpdateType.VocabularyUpdate,
                Description = $"Learned from correction: {correction.Type}",
                Metadata = new Dictionary<string, object>
                {
                    ["OriginalText"] = correction.OriginalText,
                    ["CorrectedText"] = correction.CorrectedText,
                    ["CorrectionType"] = correction.Type.ToString()
                }
            });

            return true;
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
            if (_userVocabularies.TryGetValue(userId, out var vocabulary))
                return vocabulary;

            // Create new vocabulary if it doesn't exist
            vocabulary = new PersonalizedVocabulary
            {
                UserId = userId,
                LastUpdated = DateTime.UtcNow
            };

            // Initialize with corrections history
            await InitializeVocabularyFromHistory(vocabulary);
            
            _userVocabularies[userId] = vocabulary;
            return vocabulary;
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
            var vocabulary = await GetPersonalizedVocabularyAsync(userId);
            
            foreach (var entry in entries)
            {
                vocabulary.Entries[entry.Word.ToLowerInvariant()] = entry;
            }
            
            vocabulary.LastUpdated = DateTime.UtcNow;
            
            _logger.LogInformation("Updated vocabulary for user {UserId} with {Count} entries", 
                userId, entries.Count());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vocabulary for user {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<TranscriptionSuggestion>> GetContextSuggestionsAsync(
        string text, 
        string applicationContext, 
        string userId)
    {
        try
        {
            var suggestions = new List<TranscriptionSuggestion>();
            var vocabulary = await GetPersonalizedVocabularyAsync(userId);
            
            // Get corrections history for context-aware suggestions
            var corrections = await _feedbackService.GetCorrectionsAsync();
            var userCorrections = corrections.Where(c => c.UserId == userId);
            
            // Context-specific corrections
            var contextCorrections = userCorrections
                .Where(c => !string.IsNullOrEmpty(applicationContext) && 
                           c.ApplicationContext.Contains(applicationContext, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Find suggestions based on similar text patterns
            suggestions.AddRange(await GetPatternBasedSuggestions(text, contextCorrections, vocabulary));
            
            // Find vocabulary-based suggestions
            suggestions.AddRange(await GetVocabularyBasedSuggestions(text, vocabulary));
            
            // Find grammar and capitalization suggestions
            suggestions.AddRange(await GetGrammarSuggestions(text, userCorrections));
            
            return suggestions.OrderByDescending(s => s.ConfidenceScore).Take(10);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get context suggestions for user {UserId}", userId);
            return Enumerable.Empty<TranscriptionSuggestion>();
        }
    }

    public async Task<TranscriptionResult> ApplyPersonalizationAsync(
        TranscriptionResult originalResult, 
        string userId, 
        string context = "")
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
                Metadata = originalResult.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            // Apply vocabulary corrections
            personalizedResult.Text = await ApplyVocabularyCorrections(personalizedResult.Text, userId);
            
            // Apply context-specific corrections
            personalizedResult.Text = await ApplyContextCorrections(personalizedResult.Text, userId, context);
            
            // Adjust confidence based on personalization
            personalizedResult.Confidence = await CalculatePersonalizedConfidenceAsync(
                personalizedResult.Text, (float)originalResult.Confidence, userId, SpeechEngineType.Unspecified);
            
            // Add personalization metadata
            personalizedResult.Metadata["PersonalizationApplied"] = true;
            personalizedResult.Metadata["OriginalText"] = originalResult.Text;
            personalizedResult.Metadata["PersonalizationChanges"] = 
                originalResult.Text != personalizedResult.Text ? "Applied" : "None";

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
            if (_userProfiles.TryGetValue(userId, out var profile))
                return profile;

            // Create new profile if it doesn't exist
            profile = new SpeechProfile
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            // Initialize profile from history
            await InitializeProfileFromHistory(profile);
            
            _userProfiles[userId] = profile;
            return profile;
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
            profile.LastUpdated = DateTime.UtcNow;
            _userProfiles[profile.UserId] = profile;
            
            _logger.LogInformation("Updated speech profile for user {UserId}", profile.UserId);
            
            // Raise learning update event
            ModelUpdated?.Invoke(this, new LearningUpdateEventArgs
            {
                UserId = profile.UserId,
                UpdateType = LearningUpdateType.ProfileUpdate,
                Description = "Updated speech profile"
            });

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update speech profile for user {UserId}", profile.UserId);
            return false;
        }
    }

    public async Task<float> CalculatePersonalizedConfidenceAsync(
        string text, 
        float originalConfidence, 
        string userId, 
        SpeechEngineType engine)
    {
        try
        {
            var adjustedConfidence = originalConfidence;
            var vocabulary = await GetPersonalizedVocabularyAsync(userId);
            var profile = await GetSpeechProfileAsync(userId);
            
            // Adjust based on vocabulary familiarity
            var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var knownWordCount = words.Count(w => vocabulary.Entries.ContainsKey(w));
            var vocabularyFamiliarity = words.Length > 0 ? (float)knownWordCount / words.Length : 0f;
            
            // Boost confidence for familiar vocabulary
            adjustedConfidence += vocabularyFamiliarity * 0.1f;
            
            // Adjust based on engine preference
            if (profile.EnginePreference.TryGetValue(engine, out var enginePreference))
            {
                // Apply engine preference (higher preference = higher confidence boost)
                adjustedConfidence += (enginePreference - 0.5f) * 0.05f;
            }
            
            // Adjust based on historical accuracy
            var historicalAccuracy = CalculateHistoricalAccuracy(userId, engine);
            if (historicalAccuracy > 0)
            {
                // Blend historical accuracy (80% original, 20% historical)
                adjustedConfidence = (adjustedConfidence * 0.8f) + (historicalAccuracy * 0.2f);
            }
            
            return Math.Max(0f, Math.Min(1f, adjustedConfidence));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate personalized confidence for user {UserId}", userId);
            return originalConfidence;
        }
    }

    public async Task<bool> RetrainUserModelAsync(string userId)
    {
        try
        {
            var profile = await GetSpeechProfileAsync(userId);
            var vocabulary = await GetPersonalizedVocabularyAsync(userId);
            
            // Update vocabulary frequencies
            await UpdateVocabularyFrequencies(vocabulary);
            
            // Update confidence thresholds based on correction patterns
            await UpdateConfidenceThresholds(userId);
            
            // Update engine preferences based on accuracy
            await UpdateEnginePreferences(profile);
            
            profile.LastModelUpdate = DateTime.UtcNow;
            await UpdateSpeechProfileAsync(profile);
            
            _logger.LogInformation("Retrained model for user {UserId}", userId);
            
            // Raise learning update event
            ModelUpdated?.Invoke(this, new LearningUpdateEventArgs
            {
                UserId = userId,
                UpdateType = LearningUpdateType.ModelRetrain,
                Description = "Completed model retraining"
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrain model for user {UserId}", userId);
            return false;
        }
    }

    private async Task UpdateUserVocabularyFromCorrection(TranscriptionCorrection correction)
    {
        var vocabulary = await GetPersonalizedVocabularyAsync(correction.UserId);
        
        // Extract new words from corrected text
        var correctedWords = ExtractWords(correction.CorrectedText);
        var originalWords = ExtractWords(correction.OriginalText);
        
        foreach (var word in correctedWords)
        {
            var key = word.ToLowerInvariant();
            if (vocabulary.Entries.TryGetValue(key, out var entry))
            {
                // Update existing entry
                entry.Frequency += 1f;
                entry.LastUsed = DateTime.UtcNow;
                entry.UserConfidence = Math.Min(1.0f, entry.UserConfidence + 0.1f);
                
                // Add context
                if (!string.IsNullOrEmpty(correction.ApplicationContext) && 
                    !entry.Contexts.Contains(correction.ApplicationContext))
                {
                    entry.Contexts.Add(correction.ApplicationContext);
                }
            }
            else
            {
                // Create new entry
                vocabulary.Entries[key] = new VocabularyEntry
                {
                    Word = word,
                    Frequency = 1f,
                    LastUsed = DateTime.UtcNow,
                    UserConfidence = 0.8f,
                    Contexts = !string.IsNullOrEmpty(correction.ApplicationContext) 
                        ? new List<string> { correction.ApplicationContext } 
                        : new List<string>()
                };
            }
        }

        // Update phrase confidence
        var phraseKey = correction.CorrectedText.ToLowerInvariant();
        vocabulary.PhraseConfidence[phraseKey] = 
            vocabulary.PhraseConfidence.GetValueOrDefault(phraseKey, 0.5f) + 0.2f;
        vocabulary.PhraseConfidence[phraseKey] = Math.Min(1.0f, vocabulary.PhraseConfidence[phraseKey]);
        
        vocabulary.LastUpdated = DateTime.UtcNow;
    }

    private async Task UpdateSpeechProfileFromCorrection(TranscriptionCorrection correction)
    {
        var profile = await GetSpeechProfileAsync(correction.UserId);
        
        profile.TotalCorrections++;
        
        // Update application usage
        if (!string.IsNullOrEmpty(correction.ApplicationContext))
        {
            profile.ApplicationUsage[correction.ApplicationContext] = 
                profile.ApplicationUsage.GetValueOrDefault(correction.ApplicationContext, 0) + 1;
        }
        
        // Update engine performance tracking
        var engineAccuracy = profile.EnginePreference.GetValueOrDefault(correction.Engine, 0.5f);
        // Decrease preference slightly for corrected transcriptions
        profile.EnginePreference[correction.Engine] = Math.Max(0.1f, engineAccuracy - 0.05f);
        
        await UpdateSpeechProfileAsync(profile);
    }

    private async Task LearnContextualPatterns(TranscriptionCorrection correction)
    {
        var vocabulary = await GetPersonalizedVocabularyAsync(correction.UserId);
        
        // Learn contextual synonyms
        var originalWords = ExtractWords(correction.OriginalText);
        var correctedWords = ExtractWords(correction.CorrectedText);
        
        // If it's a word replacement, learn the synonym relationship
        if (originalWords.Count == correctedWords.Count && originalWords.Count == 1)
        {
            var original = originalWords[0].ToLowerInvariant();
            var corrected = correctedWords[0].ToLowerInvariant();
            
            if (!vocabulary.ContextualSynonyms.ContainsKey(original))
                vocabulary.ContextualSynonyms[original] = new List<string>();
            
            if (!vocabulary.ContextualSynonyms[original].Contains(corrected))
                vocabulary.ContextualSynonyms[original].Add(corrected);
        }
    }

    private async Task InitializeVocabularyFromHistory(PersonalizedVocabulary vocabulary)
    {
        var corrections = await _feedbackService.GetCorrectionsAsync();
        var userCorrections = corrections.Where(c => c.UserId == vocabulary.UserId);
        
        foreach (var correction in userCorrections)
        {
            await UpdateUserVocabularyFromCorrection(correction);
        }
    }

    private async Task InitializeProfileFromHistory(SpeechProfile profile)
    {
        var corrections = await _feedbackService.GetCorrectionsAsync();
        var userCorrections = corrections.Where(c => c.UserId == profile.UserId).ToList();
        
        profile.TotalCorrections = userCorrections.Count;
        
        // Calculate application usage
        foreach (var group in userCorrections.GroupBy(c => c.ApplicationContext))
        {
            if (!string.IsNullOrEmpty(group.Key))
                profile.ApplicationUsage[group.Key] = group.Count();
        }
        
        // Calculate engine preferences based on correction frequency (inverse relationship)
        foreach (var group in userCorrections.GroupBy(c => c.Engine))
        {
            var totalForEngine = userCorrections.Count(c => c.Engine == group.Key);
            var correctionRate = (float)group.Count() / Math.Max(1, totalForEngine);
            profile.EnginePreference[group.Key] = Math.Max(0.1f, 1.0f - correctionRate);
        }
    }

    private async Task<IEnumerable<TranscriptionSuggestion>> GetPatternBasedSuggestions(
        string text, 
        List<TranscriptionCorrection> corrections, 
        PersonalizedVocabulary vocabulary)
    {
        var suggestions = new List<TranscriptionSuggestion>();
        
        // Find similar patterns in correction history
        var textWords = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var correction in corrections)
        {
            var originalWords = correction.OriginalText.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var overlap = CalculateWordOverlap(textWords, originalWords);
            
            if (overlap > 0.3f) // If significant overlap
            {
                suggestions.Add(new TranscriptionSuggestion
                {
                    OriginalText = text,
                    SuggestedText = ApplyPatternCorrection(text, correction),
                    ConfidenceScore = overlap * 0.8f,
                    Type = SuggestionType.ContextualReplacement,
                    Reason = $"Based on similar correction pattern (overlap: {overlap:P1})",
                    Context = correction.ApplicationContext
                });
            }
        }
        
        return await Task.FromResult(suggestions);
    }

    private async Task<IEnumerable<TranscriptionSuggestion>> GetVocabularyBasedSuggestions(
        string text, 
        PersonalizedVocabulary vocabulary)
    {
        var suggestions = new List<TranscriptionSuggestion>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i].ToLowerInvariant().Trim(new char[] { '.', ',', '"', ':', ';', '!', '?' });
            
            // Check for direct vocabulary matches with higher confidence
            if (vocabulary.Entries.TryGetValue(word, out var entry) && entry.UserConfidence > 0.8f)
            {
                continue; // Already confident about this word
            }
            
            // Look for similar words in vocabulary
            var similarWords = vocabulary.Entries.Values
                .Where(e => LevenshteinDistance(word, e.Word.ToLowerInvariant()) <= 2)
                .OrderBy(e => LevenshteinDistance(word, e.Word.ToLowerInvariant()))
                .Take(3);
            
            foreach (var similar in similarWords)
            {
                var newText = ReplaceWordAtIndex(text, i, similar.Word);
                suggestions.Add(new TranscriptionSuggestion
                {
                    OriginalText = text,
                    SuggestedText = newText,
                    ConfidenceScore = similar.UserConfidence * 0.7f,
                    Type = SuggestionType.VocabularyCorrection,
                    Reason = $"Personal vocabulary match: '{word}' -> '{similar.Word}'"
                });
            }
            
            // Check contextual synonyms
            if (vocabulary.ContextualSynonyms.TryGetValue(word, out var synonyms))
            {
                foreach (var synonym in synonyms.Take(2))
                {
                    var newText = ReplaceWordAtIndex(text, i, synonym);
                    suggestions.Add(new TranscriptionSuggestion
                    {
                        OriginalText = text,
                        SuggestedText = newText,
                        ConfidenceScore = 0.6f,
                        Type = SuggestionType.ContextualReplacement,
                        Reason = $"Contextual synonym: '{word}' -> '{synonym}'"
                    });
                }
            }
        }
        
        return await Task.FromResult(suggestions);
    }

    private async Task<IEnumerable<TranscriptionSuggestion>> GetGrammarSuggestions(
        string text, 
        IEnumerable<TranscriptionCorrection> corrections)
    {
        var suggestions = new List<TranscriptionSuggestion>();
        
        // Look for capitalization patterns
        var capitalizationCorrections = corrections
            .Where(c => c.Type == CorrectionType.Capitalization)
            .ToList();
        
        foreach (var correction in capitalizationCorrections)
        {
            if (text.Contains(correction.OriginalText, StringComparison.OrdinalIgnoreCase))
            {
                var correctedText = text.Replace(correction.OriginalText, correction.CorrectedText);
                suggestions.Add(new TranscriptionSuggestion
                {
                    OriginalText = text,
                    SuggestedText = correctedText,
                    ConfidenceScore = 0.8f,
                    Type = SuggestionType.GrammarCorrection,
                    Reason = "Capitalization correction based on history"
                });
            }
        }
        
        // Look for punctuation patterns
        var punctuationCorrections = corrections
            .Where(c => c.Type == CorrectionType.Punctuation)
            .ToList();
        
        foreach (var correction in punctuationCorrections)
        {
            // Simple pattern matching for punctuation
            if (text.TrimEnd('.', '!', '?') == correction.OriginalText.TrimEnd('.', '!', '?'))
            {
                suggestions.Add(new TranscriptionSuggestion
                {
                    OriginalText = text,
                    SuggestedText = correction.CorrectedText,
                    ConfidenceScore = 0.7f,
                    Type = SuggestionType.GrammarCorrection,
                    Reason = "Punctuation correction based on history"
                });
            }
        }
        
        return await Task.FromResult(suggestions);
    }

    private async Task<string> ApplyVocabularyCorrections(string text, string userId)
    {
        var vocabulary = await GetPersonalizedVocabularyAsync(userId);
        var words = text.Split(' ');
        var correctedWords = new string[words.Length];
        
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var cleanWord = word.ToLowerInvariant().Trim(new char[] { '.', ',', '"', ':', ';', '!', '?' });
            
            // Check for direct vocabulary match
            if (vocabulary.Entries.TryGetValue(cleanWord, out var entry) && entry.UserConfidence > 0.9f)
            {
                correctedWords[i] = PreserveCasing(word, entry.Word);
            }
            else
            {
                correctedWords[i] = word;
            }
        }
        
        return string.Join(" ", correctedWords);
    }

    private async Task<string> ApplyContextCorrections(string text, string userId, string context)
    {
        var corrections = await _feedbackService.GetCorrectionsAsync();
        var contextCorrections = corrections
            .Where(c => c.UserId == userId && 
                       !string.IsNullOrEmpty(context) && 
                       c.ApplicationContext.Contains(context, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Timestamp)
            .Take(10);
        
        var correctedText = text;
        
        foreach (var correction in contextCorrections)
        {
            if (correctedText.Contains(correction.OriginalText, StringComparison.OrdinalIgnoreCase))
            {
                correctedText = correctedText.Replace(correction.OriginalText, correction.CorrectedText, 
                    StringComparison.OrdinalIgnoreCase);
            }
        }
        
        return correctedText;
    }

    private float CalculateHistoricalAccuracy(string userId, SpeechEngineType engine)
    {
        if (!_userProfiles.TryGetValue(userId, out var profile))
            return 0f;
        
        return profile.EnginePreference.GetValueOrDefault(engine, 0.5f);
    }

    private async Task UpdateVocabularyFrequencies(PersonalizedVocabulary vocabulary)
    {
        // Decay old entries
        foreach (var entry in vocabulary.Entries.Values)
        {
            var daysSinceUsed = (DateTime.UtcNow - entry.LastUsed).TotalDays;
            if (daysSinceUsed > 30)
            {
                entry.Frequency *= 0.95f; // Decay factor
            }
        }
        
        await Task.CompletedTask;
    }

    private async Task UpdateConfidenceThresholds(string userId)
    {
        // This would update confidence thresholds based on correction patterns
        // Implementation would depend on having access to IConfidenceAnalysisService
        await Task.CompletedTask;
    }

    private async Task UpdateEnginePreferences(SpeechProfile profile)
    {
        var corrections = await _feedbackService.GetCorrectionsAsync();
        var userCorrections = corrections.Where(c => c.UserId == profile.UserId);
        
        // Calculate correction rates per engine
        foreach (var engineGroup in userCorrections.GroupBy(c => c.Engine))
        {
            var correctionRate = engineGroup.Count() / Math.Max(1f, profile.TotalTranscriptions);
            profile.EnginePreference[engineGroup.Key] = Math.Max(0.1f, 1.0f - correctionRate);
        }
    }

    // Helper methods
    private List<string> ExtractWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim(new char[] { '.', ',', '"', ':', ';', '!', '?' }))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();
    }

    private float CalculateWordOverlap(string[] words1, string[] words2)
    {
        var set1 = words1.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var set2 = words2.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var intersection = set1.Intersect(set2, StringComparer.OrdinalIgnoreCase).Count();
        var union = set1.Union(set2, StringComparer.OrdinalIgnoreCase).Count();
        return union > 0 ? (float)intersection / union : 0f;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private string ApplyPatternCorrection(string text, TranscriptionCorrection correction)
    {
        // Simple pattern application - in practice, this could be much more sophisticated
        return text.Replace(correction.OriginalText, correction.CorrectedText, StringComparison.OrdinalIgnoreCase);
    }

    private string ReplaceWordAtIndex(string text, int wordIndex, string newWord)
    {
        var words = text.Split(' ');
        if (wordIndex >= 0 && wordIndex < words.Length)
        {
            words[wordIndex] = PreserveCasing(words[wordIndex], newWord);
        }
        return string.Join(" ", words);
    }

    private string PreserveCasing(string original, string replacement)
    {
        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(replacement))
            return replacement;

        if (char.IsUpper(original[0]))
        {
            return char.ToUpper(replacement[0]) + replacement[1..].ToLowerInvariant();
        }

        return replacement.ToLowerInvariant();
    }

    // Missing interface method implementations
    public async Task ProcessTranscriptionCorrectionAsync(VoiceInputAssistant.Core.Interfaces.TranscriptionCorrection correction)
    {
        // Convert from Interfaces.TranscriptionCorrection to Services.Interfaces.TranscriptionCorrection
        var serviceCorrection = new VoiceInputAssistant.Core.Services.Interfaces.TranscriptionCorrection
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
    }

    public async Task ProcessUserFeedbackAsync(UserFeedback feedback)
    {
        try
        {
            _logger.LogInformation("Processing user feedback for user {UserId}", feedback.UserId);
            
            // If the feedback contains a transcription correction, process it as a learning opportunity
            if (feedback.Type == FeedbackType.TranscriptionCorrection && 
                !string.IsNullOrWhiteSpace(feedback.OriginalText) && 
                !string.IsNullOrWhiteSpace(feedback.CorrectedText))
            {
                var correction = new VoiceInputAssistant.Core.Services.Interfaces.TranscriptionCorrection
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = feedback.Timestamp.DateTime,
                    UserId = feedback.UserId,
                    OriginalText = feedback.OriginalText,
                    CorrectedText = feedback.CorrectedText,
                    ApplicationContext = feedback.ApplicationContext ?? "general",
                    Engine = SpeechEngineType.Unspecified,
                    OriginalConfidence = feedback.ConfidenceScore ?? 0.5f,
                    Type = CorrectionType.FullReplacement,
                    AudioFingerprint = ""
                };
                
                await LearnFromCorrectionAsync(correction);
            }
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
            var profile = await GetSpeechProfileAsync(userId);
            var vocabulary = await GetPersonalizedVocabularyAsync(userId);
            
            // Calculate basic stats
            var totalCorrections = vocabulary.Entries.Values.Sum(e => (int)e.Frequency);
            var vocabularySize = vocabulary.Entries.Count;
            
            // Get common mistake patterns (simplified version)
            var commonMistakes = new Dictionary<string, int>();
            foreach (var entry in vocabulary.Entries.Values.Take(10))
            {
                if (!string.IsNullOrEmpty(entry.Word) && entry.Frequency > 1)
                {
                    commonMistakes[entry.Word] = (int)entry.Frequency;
                }
            }
            
            return new LearningStats
            {
                UserId = userId,
                TotalCorrections = totalCorrections,
                AccuracyImprovement = profile.ImprovementRate,
                LastUpdateTimestamp = new DateTimeOffset(profile.LastModelUpdate),
                CommonMistakePatterns = commonMistakes,
                VocabularySize = vocabularySize,
                TotalTranscriptions = profile.TotalTranscriptions,
                AverageConfidence = vocabulary.Entries.Values.Any() ? 
                    vocabulary.Entries.Values.Average(v => v.UserConfidence) : 0.0f,
                ContextualAccuracy = new Dictionary<string, float>(),
                TotalUsageTime = TimeSpan.Zero // Would need additional tracking
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning statistics for user {UserId}", userId);
            return new LearningStats { UserId = userId };
        }
    }
}
