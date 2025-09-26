using Microsoft.EntityFrameworkCore;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Infrastructure.Data;
using VoiceInputAssistant.Infrastructure.Data.Entities;

namespace VoiceInputAssistant.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for adaptive learning persistence
/// </summary>
public class AdaptiveLearningRepository
{
    private readonly ApplicationDbContext _context;

    public AdaptiveLearningRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // User Vocabulary Management
    public async Task UpdateUserVocabularyAsync(Guid userId, IEnumerable<VocabularyEntry> vocabularyEntries)
    {
        foreach (var entry in vocabularyEntries)
        {
            var existingEntry = await _context.UserVocabularies
                .FirstOrDefaultAsync(v => v.UserId == userId && 
                                         v.OriginalWord == entry.OriginalWord &&
                                         v.ApplicationContext == entry.ApplicationContext);

            if (existingEntry != null)
            {
                existingEntry.CorrectedWord = entry.CorrectedWord;
                existingEntry.UsageCount++;
                existingEntry.ConfidenceLevel = Math.Min(1.0, existingEntry.ConfidenceLevel + 0.1);
                existingEntry.LastUsedAt = DateTime.UtcNow;
            }
            else
            {
                var newEntry = new UserVocabularyEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OriginalWord = entry.OriginalWord,
                    CorrectedWord = entry.CorrectedWord,
                    UsageCount = 1,
                    ConfidenceLevel = entry.ConfidenceLevel,
                    ApplicationContext = entry.ApplicationContext,
                    Category = entry.Category,
                    Language = entry.Language ?? "en-US",
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UserVocabularies.Add(newEntry);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<VocabularyEntry>> GetUserVocabularyAsync(Guid userId, string? applicationContext = null)
    {
        var query = _context.UserVocabularies
            .Where(v => v.UserId == userId && v.IsActive);

        if (!string.IsNullOrEmpty(applicationContext))
        {
            query = query.Where(v => v.ApplicationContext == applicationContext);
        }

        var entries = await query
            .Select(v => new VocabularyEntry
            {
                OriginalWord = v.OriginalWord,
                CorrectedWord = v.CorrectedWord,
                ConfidenceLevel = v.ConfidenceLevel,
                ApplicationContext = v.ApplicationContext,
                Category = v.Category,
                Language = v.Language,
                UsageCount = v.UsageCount
            })
            .ToListAsync();

        return entries;
    }

    // Context Pattern Management
    public async Task UpdateContextPatternsAsync(Guid userId, IEnumerable<ContextPattern> patterns)
    {
        foreach (var pattern in patterns)
        {
            var existingPattern = await _context.ContextPatterns
                .FirstOrDefaultAsync(p => p.UserId == userId &&
                                         p.PrecedingContext == pattern.PrecedingContext &&
                                         p.FollowingContext == pattern.FollowingContext &&
                                         p.ApplicationContext == pattern.ApplicationContext);

            if (existingPattern != null)
            {
                existingPattern.ExpectedText = pattern.ExpectedText;
                existingPattern.ObservationCount++;
                existingPattern.ConfidenceLevel = Math.Min(1.0, existingPattern.ConfidenceLevel + 0.1);
                existingPattern.LastUsedAt = DateTime.UtcNow;
            }
            else
            {
                var newPattern = new ContextPatternEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PrecedingContext = pattern.PrecedingContext,
                    FollowingContext = pattern.FollowingContext,
                    ExpectedText = pattern.ExpectedText,
                    ApplicationContext = pattern.ApplicationContext,
                    ObservationCount = 1,
                    ConfidenceLevel = pattern.ConfidenceLevel,
                    PatternType = pattern.PatternType,
                    Language = pattern.Language ?? "en-US",
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ContextPatterns.Add(newPattern);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ContextPattern>> GetContextPatternsAsync(Guid userId, string? applicationContext = null)
    {
        var query = _context.ContextPatterns
            .Where(p => p.UserId == userId && p.IsActive);

        if (!string.IsNullOrEmpty(applicationContext))
        {
            query = query.Where(p => p.ApplicationContext == applicationContext);
        }

        var patterns = await query
            .OrderByDescending(p => p.ConfidenceLevel)
            .ThenByDescending(p => p.ObservationCount)
            .Select(p => new ContextPattern
            {
                PrecedingContext = p.PrecedingContext,
                FollowingContext = p.FollowingContext,
                ExpectedText = p.ExpectedText,
                ApplicationContext = p.ApplicationContext,
                ConfidenceLevel = p.ConfidenceLevel,
                PatternType = p.PatternType,
                Language = p.Language,
                ObservationCount = p.ObservationCount
            })
            .ToListAsync();

        return patterns;
    }

    // Speech Profile Management
    public async Task<SpeechProfile?> GetSpeechProfileAsync(Guid userId, string? applicationContext = null)
    {
        SpeechProfileEntity? profileEntity = null;

        if (!string.IsNullOrEmpty(applicationContext))
        {
            // Look for a profile that includes this application context
            profileEntity = await _context.SpeechProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .FirstOrDefaultAsync(p => p.ApplicationContexts.Contains(applicationContext));
        }

        // Fall back to default profile if no context-specific profile found
        if (profileEntity == null)
        {
            profileEntity = await _context.SpeechProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault && p.IsActive);
        }

        if (profileEntity == null)
            return null;

        return new SpeechProfile
        {
            UserId = profileEntity.UserId.ToString(),
            ProfileName = profileEntity.ProfileName,
            PreferredEngine = profileEntity.PreferredEngine,
            Language = profileEntity.Language,
            ApplicationContexts = profileEntity.ApplicationContexts,
            EngineSettings = profileEntity.EngineSettings ?? new Dictionary<string, object>(),
            AccuracyMetrics = profileEntity.AccuracyMetrics.ToDictionary(kv => kv.Key, kv => (object)kv.Value)
        };
    }

    public async Task CreateOrUpdateSpeechProfileAsync(SpeechProfile profile)
    {
        var userIdGuid = Guid.Parse(profile.UserId);
        var existingProfile = await _context.SpeechProfiles
            .FirstOrDefaultAsync(p => p.UserId == userIdGuid && 
                                     p.ProfileName == profile.ProfileName);

        if (existingProfile != null)
        {
            existingProfile.PreferredEngine = profile.PreferredEngine;
            existingProfile.Language = profile.Language;
            existingProfile.ApplicationContexts = profile.ApplicationContexts;
            existingProfile.EngineSettings = profile.EngineSettings;
            existingProfile.AccuracyMetrics = profile.AccuracyMetrics.ToDictionary(kv => kv.Key, kv => Convert.ToDouble(kv.Value));
            existingProfile.UpdatedAt = DateTime.UtcNow;
            existingProfile.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            var newProfile = new SpeechProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = userIdGuid,
                ProfileName = profile.ProfileName,
                PreferredEngine = profile.PreferredEngine,
                Language = profile.Language,
                ApplicationContexts = profile.ApplicationContexts,
                EngineSettings = profile.EngineSettings,
                AccuracyMetrics = profile.AccuracyMetrics.ToDictionary(kv => kv.Key, kv => Convert.ToDouble(kv.Value)),
                IsDefault = profile.IsDefault,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };

            _context.SpeechProfiles.Add(newProfile);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SpeechProfile>> GetAllSpeechProfilesAsync(Guid userId)
    {
        var profiles = await _context.SpeechProfiles
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.ProfileName)
            .Select(p => new SpeechProfile
            {
                UserId = p.UserId.ToString(),
                ProfileName = p.ProfileName,
                PreferredEngine = p.PreferredEngine,
                Language = p.Language,
                ApplicationContexts = p.ApplicationContexts,
                EngineSettings = p.EngineSettings ?? new Dictionary<string, object>(),
                AccuracyMetrics = p.AccuracyMetrics.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                IsDefault = p.IsDefault
            })
            .ToListAsync();

        return profiles;
    }

    // Confidence Score Management
    public async Task SaveConfidenceAnalysisAsync(Guid transcriptionEventId, Guid? userId, ConfidenceAnalysisResult analysis)
    {
        var confidenceScore = new ConfidenceScoreEntity
        {
            Id = Guid.NewGuid(),
            TranscriptionEventId = transcriptionEventId,
            UserId = userId,
            Text = analysis.TranscriptionId, // Use TranscriptionId as the best available text identifier
            OverallConfidence = analysis.OverallConfidence,
            WordConfidences = analysis.UncertainWords.ToDictionary(w => w.Word, w => (double)w.Confidence), // Convert UncertainWords to dictionary of word confidences
            UncertainWords = analysis.UncertainWords.Select(w => w.Word).ToList(),
            Engine = analysis.TranscriptionId, // Use TranscriptionId as engine placeholder
            ApplicationContext = "", // Default empty context
            ConfidenceThreshold = 0.75f, // Default threshold
            FeedbackRequested = analysis.NeedsFeedback,
            AnalysisMetadata = new Dictionary<string, object>(), // Empty metadata for now
            AnalyzedAt = DateTime.UtcNow
        };

        _context.ConfidenceScores.Add(confidenceScore);
        await _context.SaveChangesAsync();
    }

    public async Task<ConfidenceAnalysisResult?> GetConfidenceAnalysisAsync(Guid transcriptionEventId)
    {
        var entity = await _context.ConfidenceScores
            .FirstOrDefaultAsync(c => c.TranscriptionEventId == transcriptionEventId);

        if (entity == null)
            return null;

        return new ConfidenceAnalysisResult
        {
            TranscriptionId = entity.Text,
            OverallConfidence = (float)entity.OverallConfidence,
            NeedsFeedback = entity.FeedbackRequested,
            FeedbackReason = "Confidence analysis",
            UncertainWords = entity.UncertainWords.Select(w => new Core.Services.Interfaces.UncertainWord 
            {
                Word = w,
                Position = 0,
                Confidence = 0.5f,
                Alternatives = new List<string>(),
                Reason = Core.Services.Interfaces.UncertaintyReason.LowAcousticClarity,
                IsNewWord = false
            }).ToList(),
            AnalyzedAt = entity.AnalyzedAt
        };
    }

    public async Task MarkFeedbackReceivedAsync(Guid transcriptionEventId)
    {
        var confidenceScore = await _context.ConfidenceScores
            .FirstOrDefaultAsync(c => c.TranscriptionEventId == transcriptionEventId);

        if (confidenceScore != null)
        {
            confidenceScore.FeedbackReceived = true;
            await _context.SaveChangesAsync();
        }
    }
}