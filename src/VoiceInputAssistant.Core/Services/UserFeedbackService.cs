using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// Implementation of user feedback service for collecting and processing transcription corrections
/// </summary>
public class UserFeedbackService : IUserFeedbackService
{
    private readonly ILogger<UserFeedbackService> _logger;
    private readonly List<TranscriptionCorrection> _corrections = new();
    private readonly List<TranscriptionRating> _ratings = new();
    
    public event EventHandler<UserFeedbackEventArgs>? FeedbackReceived;

    public UserFeedbackService(ILogger<UserFeedbackService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> RecordCorrectionAsync(TranscriptionCorrection correction)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(correction.OriginalText) || 
                string.IsNullOrWhiteSpace(correction.CorrectedText))
            {
                _logger.LogWarning("Invalid correction: empty original or corrected text");
                return false;
            }

            // Analyze the type of correction
            correction.Type = AnalyzeCorrectionType(correction.OriginalText, correction.CorrectedText);
            
            _corrections.Add(correction);
            
            _logger.LogInformation("Recorded correction from user {UserId}: '{Original}' -> '{Corrected}'", 
                correction.UserId, correction.OriginalText, correction.CorrectedText);

            // Raise feedback received event
            FeedbackReceived?.Invoke(this, new UserFeedbackEventArgs 
            { 
                Correction = correction 
            });

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record correction for user {UserId}", correction.UserId);
            return false;
        }
    }

    public async Task<bool> RecordRatingAsync(TranscriptionRating rating)
    {
        try
        {
            if (rating.Rating < 1 || rating.Rating > 5)
            {
                _logger.LogWarning("Invalid rating value: {Rating}. Must be between 1-5", rating.Rating);
                return false;
            }

            _ratings.Add(rating);
            
            _logger.LogInformation("Recorded rating {Rating}/5 from user {UserId} for transcription: '{Text}'", 
                rating.Rating, rating.UserId, rating.TranscribedText);

            // Raise feedback received event
            FeedbackReceived?.Invoke(this, new UserFeedbackEventArgs 
            { 
                Rating = rating 
            });

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record rating for user {UserId}", rating.UserId);
            return false;
        }
    }

    public async Task<IEnumerable<TranscriptionCorrection>> GetCorrectionsAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        SpeechEngineType? engine = null)
    {
        try
        {
            var query = _corrections.AsEnumerable();

            if (startDate.HasValue)
                query = query.Where(c => c.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.Timestamp <= endDate.Value);

            if (engine.HasValue)
                query = query.Where(c => c.Engine == engine.Value);

            return await Task.FromResult(query.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get corrections for date range {StartDate} - {EndDate}", 
                startDate, endDate);
            return Enumerable.Empty<TranscriptionCorrection>();
        }
    }

    public async Task<IEnumerable<CommonCorrection>> GetCommonCorrectionsAsync(int limit = 50)
    {
        try
        {
            var commonCorrections = _corrections
            .GroupBy(c => new { OriginalText = c.OriginalText.ToLowerInvariant(), CorrectedText = c.CorrectedText.ToLowerInvariant() })
                .Select(g => new CommonCorrection
                {
                    OriginalPhrase = g.Key.OriginalText,
                    CorrectedPhrase = g.Key.CorrectedText,
                    Frequency = g.Count(),
                    LastSeen = g.Max(c => c.Timestamp),
                    Context = g.First().ApplicationContext,
                    ConfidenceImprovement = g.Average(c => 1.0f - c.OriginalConfidence)
                })
                .OrderByDescending(c => c.Frequency)
                .Take(limit)
                .ToList();

            return await Task.FromResult(commonCorrections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get common corrections");
            return Enumerable.Empty<CommonCorrection>();
        }
    }

    public async Task<AccuracyMetrics> GetAccuracyMetricsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var correctionsInPeriod = _corrections
                .Where(c => c.Timestamp >= startDate && c.Timestamp <= endDate)
                .ToList();

            var ratingsInPeriod = _ratings
                .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
                .ToList();

            // Calculate accuracy metrics
            var totalTranscriptions = ratingsInPeriod.Count;
            var correctedTranscriptions = correctionsInPeriod.Count;

            var metrics = new AccuracyMetrics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalTranscriptions = totalTranscriptions,
                CorrectedTranscriptions = correctedTranscriptions,
                AverageConfidence = ratingsInPeriod.Any() ? ratingsInPeriod.Average(r => r.Confidence) : 0f
            };

            // Engine-specific accuracy
            var engineGroups = correctionsInPeriod.GroupBy(c => c.Engine);
            foreach (var group in engineGroups)
            {
                var engineCorrections = group.Count();
                var engineTotal = ratingsInPeriod.Count(r => r.Engine == group.Key);
                var accuracy = engineTotal > 0 ? 1.0f - ((float)engineCorrections / engineTotal) : 1.0f;
                metrics.EngineAccuracy[group.Key] = accuracy;
            }

            // Common mistakes
            metrics.CommonMistakes = correctionsInPeriod
                .SelectMany(c => ExtractWords(c.OriginalText))
                .GroupBy(w => w.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            return await Task.FromResult(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get accuracy metrics for period {StartDate} - {EndDate}", 
                startDate, endDate);
            return new AccuracyMetrics { StartDate = startDate, EndDate = endDate };
        }
    }

    public async Task<IEnumerable<string>> SuggestCorrectionsAsync(string originalText, string context = "")
    {
        try
        {
            var suggestions = new List<string>();

            // Find similar corrections from history
            var similarCorrections = _corrections
                .Where(c => 
                    c.OriginalText.ToLowerInvariant().Contains(originalText.ToLowerInvariant()) ||
                    LevenshteinDistance(c.OriginalText, originalText) <= 3)
                .Where(c => string.IsNullOrEmpty(context) || c.ApplicationContext.Contains(context))
                .OrderBy(c => LevenshteinDistance(c.OriginalText, originalText))
                .Take(5)
                .Select(c => c.CorrectedText)
                .Distinct();

            suggestions.AddRange(similarCorrections);

            // Add context-based suggestions
            if (!string.IsNullOrEmpty(context))
            {
                var contextSuggestions = _corrections
                    .Where(c => c.ApplicationContext.Equals(context, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(c => c.CorrectedText)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key);

                suggestions.AddRange(contextSuggestions);
            }

            return await Task.FromResult(suggestions.Distinct().Take(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest corrections for text: {Text}", originalText);
            return Enumerable.Empty<string>();
        }
    }

    private CorrectionType AnalyzeCorrectionType(string original, string corrected)
    {
        // Simple analysis of correction type
        var originalWords = ExtractWords(original);
        var correctedWords = ExtractWords(corrected);

        // Full replacement if completely different
        if (originalWords.Count != correctedWords.Count || 
            !originalWords.Any(w => correctedWords.Contains(w, StringComparer.OrdinalIgnoreCase)))
        {
            return CorrectionType.FullReplacement;
        }

        // Check for punctuation changes
        if (original.Replace(" ", "").Equals(corrected.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
        {
            if (original != corrected)
                return CorrectionType.Punctuation;
        }

        // Check for capitalization changes
        if (original.Equals(corrected, StringComparison.OrdinalIgnoreCase) && original != corrected)
        {
            return CorrectionType.Capitalization;
        }

        // Check for new vocabulary
        if (correctedWords.Any(w => !originalWords.Contains(w, StringComparer.OrdinalIgnoreCase)))
        {
            return CorrectionType.VocabularyAddition;
        }

        return CorrectionType.PartialCorrection;
    }

    private List<string> ExtractWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim(new char[] { '.', ',', '"', ':', ';', '!', '?' }))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

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
}