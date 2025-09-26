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
/// Implementation of confidence analysis service for identifying uncertain transcriptions
/// </summary>
public class ConfidenceAnalysisService : IConfidenceAnalysisService
{
    private readonly ILogger<ConfidenceAnalysisService> _logger;
    private readonly IUserFeedbackService _feedbackService;
    private readonly Dictionary<string, ConfidenceThresholds> _userThresholds = new();

    public event EventHandler<LowConfidenceEventArgs>? LowConfidenceDetected;

    public ConfidenceAnalysisService(
        ILogger<ConfidenceAnalysisService> logger, 
        IUserFeedbackService feedbackService)
    {
        _logger = logger;
        _feedbackService = feedbackService;
    }

    public async Task<ConfidenceAnalysisResult> AnalyzeConfidenceAsync(TranscriptionResult result, string userId = "")
    {
        try
        {
            var analysis = new ConfidenceAnalysisResult
            {
                TranscriptionId = result.Id,
                OverallConfidence = (float)result.Confidence
            };

            // Get user-specific thresholds
            var thresholds = await GetConfidenceThresholdsAsync("", userId);

            // Analyze text segments
            analysis.Segments = await AnalyzeTextSegmentsAsync(result.Text, (float)result.Confidence);

            // Identify uncertain words
            analysis.UncertainWords = await IdentifyUncertainWordsAsync(result.Text, (float)result.Confidence, userId);

            // Calculate quality score
            analysis.QualityScore = await CalculateQualityScoreAsync(result, userId);

            // Generate flags
            analysis.Flags = await GenerateConfidenceFlagsAsync(result, analysis, thresholds);

            // Determine if feedback is needed
            analysis.NeedsFeedback = await ShouldRequestFeedbackAsync(result, userId);
            analysis.FeedbackReason = DetermineFeedbackReason(analysis, thresholds);

            // Raise event if low confidence detected
            if (analysis.OverallConfidence < thresholds.RequestFeedback)
            {
                LowConfidenceDetected?.Invoke(this, new LowConfidenceEventArgs
                {
                    TranscriptionId = result.Id,
                    UserId = userId,
                    Analysis = analysis,
                    RequiresImmediateAttention = analysis.OverallConfidence < thresholds.MinimumAcceptable
                });
            }

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze confidence for transcription {TranscriptionId}", result.Id);
            return new ConfidenceAnalysisResult 
            { 
                TranscriptionId = result.Id, 
                OverallConfidence = (float)result.Confidence 
            };
        }
    }

    public async Task<bool> ShouldRequestFeedbackAsync(TranscriptionResult result, string userId = "")
    {
        var thresholds = await GetConfidenceThresholdsAsync("", userId);
        
        // Request feedback if overall confidence is low
        if (result.Confidence < thresholds.RequestFeedback)
            return true;

        // Request feedback if contains unknown words
        var unknownWords = await IdentifyUncertainWordsAsync(result.Text, (float)result.Confidence, userId);
        if (unknownWords.Any(w => w.IsNewWord))
            return true;

        // Request feedback for technical terms with low confidence
        if (ContainsTechnicalTerms(result.Text) && result.Confidence < thresholds.TechnicalTermThreshold)
            return true;

        return false;
    }

    public async Task<ConfidenceThresholds> GetConfidenceThresholdsAsync(string context, string userId = "")
    {
        var key = $"{userId}_{context}";
        
        if (_userThresholds.TryGetValue(key, out var thresholds))
            return thresholds;

        // Return default thresholds if user-specific ones don't exist
        var defaultThresholds = new ConfidenceThresholds
        {
            UserId = userId,
            Context = context,
            MinimumAcceptable = 0.6f,
            RequestFeedback = 0.75f,
            HighConfidence = 0.9f,
            CriticalWordThreshold = 0.8f,
            TechnicalTermThreshold = 0.85f,
            ProperNounThreshold = 0.8f
        };

        _userThresholds[key] = defaultThresholds;
        return await Task.FromResult(defaultThresholds);
    }

    public async Task<bool> UpdateConfidenceThresholdsAsync(string userId, ConfidenceThresholds thresholds)
    {
        try
        {
            var key = $"{userId}_{thresholds.Context}";
            thresholds.LastUpdated = DateTime.UtcNow;
            _userThresholds[key] = thresholds;
            
            _logger.LogInformation("Updated confidence thresholds for user {UserId} in context '{Context}'", 
                userId, thresholds.Context);
            
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update confidence thresholds for user {UserId}", userId);
            return false;
        }
    }

    public async Task<float> CalculateQualityScoreAsync(TranscriptionResult result, string userId = "")
    {
        try
        {
            var score = (float)result.Confidence;

            // Adjust based on text characteristics
            var words = result.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Penalize very short transcriptions
            if (words.Length < 3)
                score *= 0.9f;

            // Penalize transcriptions with no punctuation for longer texts
            if (words.Length > 5 && !ContainsPunctuation(result.Text))
                score *= 0.95f;

            // Bonus for proper capitalization
            if (HasProperCapitalization(result.Text))
                score *= 1.05f;

            // Penalize if contains many numbers (often misrecognized)
            var numberCount = words.Count(w => IsNumeric(w));
            if (numberCount > words.Length * 0.3f)
                score *= 0.9f;

            // Get historical accuracy for similar texts
            if (!string.IsNullOrEmpty(userId))
            {
                var historicalAccuracy = await PredictConfidenceAsync(result.Text, result.Engine.ToString(), userId);
                if (historicalAccuracy > 0)
                {
                    // Blend with historical data (70% current, 30% historical)
                    score = (score * 0.7f) + ((float)historicalAccuracy * 0.3f);
                }
            }

            return (float)Math.Max(0f, Math.Min(1f, score));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate quality score");
            return (float)result.Confidence;
        }
    }

    public async Task<IEnumerable<LowConfidencePattern>> GetLowConfidencePatternsAsync(
        string userId, 
        int daysBack = 30, 
        float thresholdBelow = 0.7f)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-daysBack);
            var corrections = await _feedbackService.GetCorrectionsAsync(startDate, null);

            var patterns = corrections
                .Where(c => c.UserId == userId && c.OriginalConfidence < thresholdBelow)
                .GroupBy(c => c.OriginalText.ToLowerInvariant())
                .Select(g => new LowConfidencePattern
                {
                    Pattern = g.Key,
                    AverageConfidence = g.Average(c => c.OriginalConfidence),
                    Frequency = g.Count(),
                    LastSeen = g.Max(c => c.Timestamp),
                    CommonMisrecognitions = g.Select(c => c.CorrectedText).Distinct().ToList(),
                    RecommendedAction = GenerateRecommendation(g.Key, g.Average(c => c.OriginalConfidence))
                })
                .OrderBy(p => p.AverageConfidence)
                .ToList();

            return await Task.FromResult(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get low confidence patterns for user {UserId}", userId);
            return Enumerable.Empty<LowConfidencePattern>();
        }
    }

    public async Task<float> PredictConfidenceAsync(string text, string engine, string userId = "")
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                return 0f;

            var corrections = await _feedbackService.GetCorrectionsAsync();
            var userCorrections = corrections.Where(c => c.UserId == userId && c.Engine.ToString() == engine);

            if (!userCorrections.Any())
                return 0f;

            // Find similar texts using simple word overlap
            var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var similarities = userCorrections
                .Select(c => new
                {
                    Correction = c,
                    Similarity = CalculateWordOverlap(words, 
                        c.OriginalText.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                })
                .Where(s => s.Similarity > 0.1f)
                .OrderByDescending(s => s.Similarity)
                .Take(5);

            if (!similarities.Any())
                return 0f;

            // Weight predictions by similarity
            var weightedConfidence = similarities
                .Sum(s => s.Correction.OriginalConfidence * s.Similarity) / 
                similarities.Sum(s => s.Similarity);

            return weightedConfidence;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to predict confidence for user {UserId}", userId);
            return 0f;
        }
    }

    private async Task<List<ConfidenceSegment>> AnalyzeTextSegmentsAsync(string text, float overallConfidence)
    {
        var segments = new List<ConfidenceSegment>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var confidence = CalculateWordConfidence(word, overallConfidence);
            
            segments.Add(new ConfidenceSegment
            {
                StartIndex = text.IndexOf(word, i > 0 ? segments[i-1].EndIndex : 0),
                EndIndex = text.IndexOf(word, i > 0 ? segments[i-1].EndIndex : 0) + word.Length,
                Text = word,
                Confidence = confidence,
                Level = GetConfidenceLevel(confidence)
            });
        }

        return await Task.FromResult(segments);
    }

    private async Task<List<UncertainWord>> IdentifyUncertainWordsAsync(string text, float confidence, string userId)
    {
        var uncertainWords = new List<UncertainWord>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i].Trim(new char[] { '.', ',', '"', ':', ';', '!', '?' });
            var wordConfidence = CalculateWordConfidence(word, confidence);
            
            if (wordConfidence < 0.8f || IsLikelyUncertain(word))
            {
                uncertainWords.Add(new UncertainWord
                {
                    Word = word,
                    Position = i,
                    Confidence = wordConfidence,
                    Reason = DetermineUncertaintyReason(word, wordConfidence),
                    IsNewWord = await IsNewWordForUser(word, userId),
                    Alternatives = GenerateAlternatives(word)
                });
            }
        }

        return uncertainWords;
    }

    private async Task<List<ConfidenceFlag>> GenerateConfidenceFlagsAsync(
        TranscriptionResult result, 
        ConfidenceAnalysisResult analysis, 
        ConfidenceThresholds thresholds)
    {
        var flags = new List<ConfidenceFlag>();

        // Low overall confidence flag
        if (result.Confidence < thresholds.MinimumAcceptable)
        {
            flags.Add(new ConfidenceFlag
            {
                Type = ConfidenceFlagType.LowOverallConfidence,
                Description = $"Overall confidence {result.Confidence:P1} is below minimum acceptable threshold {thresholds.MinimumAcceptable:P1}",
                Severity = (float)(1.0f - result.Confidence),
                Recommendation = "Request user correction"
            });
        }

        // Inconsistent segments flag
        var confidenceVariance = analysis.Segments.Any() ? 
            analysis.Segments.Select(s => s.Confidence).ToList().Variance() : 0f;
        
        if (confidenceVariance > 0.1f)
        {
            flags.Add(new ConfidenceFlag
            {
                Type = ConfidenceFlagType.InconsistentSegments,
                Description = "High variance in segment confidence levels",
                Severity = Math.Min(1.0f, confidenceVariance * 2),
                Recommendation = "Review uncertain segments individually"
            });
        }

        // Unknown terminology flag
        if (analysis.UncertainWords.Any(w => w.IsNewWord))
        {
            flags.Add(new ConfidenceFlag
            {
                Type = ConfidenceFlagType.UnknownTerminology,
                Description = $"Contains {analysis.UncertainWords.Count(w => w.IsNewWord)} unknown words",
                Severity = Math.Min(1.0f, analysis.UncertainWords.Count(w => w.IsNewWord) * 0.2f),
                Recommendation = "Add new words to personal vocabulary"
            });
        }

        return await Task.FromResult(flags);
    }

    private float CalculateWordConfidence(string word, float baseConfidence)
    {
        var confidence = baseConfidence;

        // Reduce confidence for short words (often misheard)
        if (word.Length <= 2)
            confidence *= 0.9f;

        // Reduce confidence for numbers
        if (IsNumeric(word))
            confidence *= 0.85f;

        // Reduce confidence for words with unusual capitalization
        if (HasUnusualCapitalization(word))
            confidence *= 0.9f;

        // Reduce confidence for words with special characters
        if (word.Any(c => !char.IsLetterOrDigit(c) && c != '\'' && c != '-'))
            confidence *= 0.85f;

        return Math.Max(0.1f, confidence);
    }

    private ConfidenceLevel GetConfidenceLevel(float confidence)
    {
        return confidence switch
        {
            >= 0.9f => ConfidenceLevel.VeryHigh,
            >= 0.8f => ConfidenceLevel.High,
            >= 0.6f => ConfidenceLevel.Medium,
            >= 0.4f => ConfidenceLevel.Low,
            _ => ConfidenceLevel.VeryLow
        };
    }

    private UncertaintyReason DetermineUncertaintyReason(string word, float confidence)
    {
        if (confidence < 0.3f) return UncertaintyReason.LowAcousticClarity;
        if (IsNumeric(word)) return UncertaintyReason.AmbiguousPhonetics;
        if (char.IsUpper(word[0]) && word.Length > 3) return UncertaintyReason.ProperNoun;
        if (ContainsTechnicalTerms(word)) return UncertaintyReason.TechnicalJargon;
        return UncertaintyReason.UnknownWord;
    }

    private async Task<bool> IsNewWordForUser(string word, string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        
        var corrections = await _feedbackService.GetCorrectionsAsync();
        return !corrections.Any(c => c.UserId == userId && 
            (c.OriginalText.Contains(word, StringComparison.OrdinalIgnoreCase) ||
             c.CorrectedText.Contains(word, StringComparison.OrdinalIgnoreCase)));
    }

    private List<string> GenerateAlternatives(string word)
    {
        var alternatives = new List<string>();
        
        // Simple phonetic alternatives (this could be much more sophisticated)
        if (word.EndsWith("s") && word.Length > 2)
            alternatives.Add(word[..^1]);
        
        if (!word.EndsWith("s"))
            alternatives.Add(word + "s");

        // Common misrecognitions
        var commonMisrecognitions = new Dictionary<string, string[]>
        {
            ["to"] = new[] { "two", "too" },
            ["there"] = new[] { "their", "they're" },
            ["its"] = new[] { "it's" },
            ["your"] = new[] { "you're" },
            ["hear"] = new[] { "here" },
            ["break"] = new[] { "brake" }
        };

        if (commonMisrecognitions.TryGetValue(word.ToLowerInvariant(), out var alts))
            alternatives.AddRange(alts);

        return alternatives.Distinct().Take(3).ToList();
    }

    private string DetermineFeedbackReason(ConfidenceAnalysisResult analysis, ConfidenceThresholds thresholds)
    {
        if (analysis.OverallConfidence < thresholds.MinimumAcceptable)
            return "Very low confidence - transcription likely inaccurate";
        
        if (analysis.UncertainWords.Any(w => w.IsNewWord))
            return "Contains unknown words that need verification";
        
        if (analysis.Flags.Any(f => f.Type == ConfidenceFlagType.InconsistentSegments))
            return "Inconsistent confidence across segments";
        
        return "Below feedback threshold - please verify accuracy";
    }

    private string GenerateRecommendation(string pattern, float confidence)
    {
        if (confidence < 0.5f)
            return "Consider training voice recognition with this phrase";
        if (confidence < 0.7f)
            return "Add to custom vocabulary for better recognition";
        return "Monitor for consistent misrecognition";
    }

    private bool ContainsTechnicalTerms(string text)
    {
        var technicalIndicators = new[] { "API", "HTTP", "SQL", "JSON", "XML", "URL", "ID", "DB" };
        return technicalIndicators.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsPunctuation(string text) => 
        text.Any(c => ".!?,:;".Contains(c));

    private bool HasProperCapitalization(string text) =>
        !string.IsNullOrEmpty(text) && char.IsUpper(text[0]);

    private bool IsNumeric(string word) => 
        word.All(c => char.IsDigit(c) || c == '.' || c == ',');

    private bool HasUnusualCapitalization(string word) =>
        word.Length > 1 && word.Skip(1).Any(char.IsUpper) && !word.All(char.IsUpper);

    private bool IsLikelyUncertain(string word) =>
        word.Length <= 2 || IsNumeric(word) || HasUnusualCapitalization(word);

    private float CalculateWordOverlap(string[] words1, string[] words2)
    {
        var set1 = words1.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var set2 = words2.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var intersection = set1.Intersect(set2, StringComparer.OrdinalIgnoreCase).Count();
        var union = set1.Union(set2, StringComparer.OrdinalIgnoreCase).Count();
        return union > 0 ? (float)intersection / union : 0f;
    }
}

public static class VarianceExtension
{
    public static float Variance(this IEnumerable<float> values)
    {
        var list = values.ToList();
        if (list.Count <= 1) return 0f;
        var mean = list.Average();
        return list.Sum(v => (v - mean) * (v - mean)) / list.Count;
    }
}