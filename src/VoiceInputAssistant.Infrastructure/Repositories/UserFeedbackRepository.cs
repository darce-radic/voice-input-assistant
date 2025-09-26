using Microsoft.EntityFrameworkCore;
using VoiceInputAssistant.Core.Events;
using VoiceInputAssistant.Core.Interfaces;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Infrastructure.Data;
using VoiceInputAssistant.Infrastructure.Data.Entities;

namespace VoiceInputAssistant.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user feedback persistence
/// </summary>
public class UserFeedbackRepository : IUserFeedbackService
{
    private readonly ApplicationDbContext _context;

    public UserFeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordCorrectionAsync(Guid userId, string originalText, string correctedText, string engine, double? confidenceScore = null, string? applicationContext = null, Guid? transcriptionEventId = null)
    {
        var feedback = new UserFeedbackEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TranscriptionEventId = transcriptionEventId,
            OriginalText = originalText,
            CorrectedText = correctedText,
            Engine = engine,
            ConfidenceScore = confidenceScore,
            ApplicationContext = applicationContext,
            FeedbackType = "Correction",
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.UserFeedbacks.Add(feedback);
        await _context.SaveChangesAsync();
    }

    public async Task RecordRatingAsync(Guid userId, string transcribedText, int rating, string engine, string? applicationContext = null, Guid? transcriptionEventId = null)
    {
        var feedback = new UserFeedbackEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TranscriptionEventId = transcriptionEventId,
            OriginalText = transcribedText,
            CorrectedText = transcribedText, // Same as original for ratings
            Rating = rating,
            Engine = engine,
            ApplicationContext = applicationContext,
            FeedbackType = "Rating",
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.UserFeedbacks.Add(feedback);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TranscriptionCorrection>> GetRecentCorrectionsAsync(Guid userId, int limit = 50)
    {
        var corrections = await _context.UserFeedbacks
            .Where(f => f.UserId == userId && f.FeedbackType == "Correction")
            .OrderByDescending(f => f.CreatedAt)
            .Take(limit)
            .Select(f => new TranscriptionCorrection
            {
                OriginalText = f.OriginalText,
                CorrectedText = f.CorrectedText,
                Engine = f.Engine,
                ConfidenceScore = f.ConfidenceScore ?? 0.0,
                ApplicationContext = f.ApplicationContext,
                Timestamp = f.CreatedAt
            })
            .ToListAsync();

        return corrections;
    }

    public async Task<IEnumerable<TranscriptionCorrection>> GetCommonCorrectionsAsync(Guid userId, string? applicationContext = null, int limit = 100)
    {
        var query = _context.UserFeedbacks
            .Where(f => f.UserId == userId && f.FeedbackType == "Correction");

        if (!string.IsNullOrEmpty(applicationContext))
        {
            query = query.Where(f => f.ApplicationContext == applicationContext);
        }

        var corrections = await query
            .GroupBy(f => new { f.OriginalText, f.CorrectedText })
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => new TranscriptionCorrection
            {
                OriginalText = g.Key.OriginalText,
                CorrectedText = g.Key.CorrectedText,
                Engine = g.First().Engine,
                ConfidenceScore = g.Average(f => f.ConfidenceScore ?? 0.0),
                ApplicationContext = g.First().ApplicationContext,
                Timestamp = g.Max(f => f.CreatedAt)
            })
            .ToListAsync();

        return corrections;
    }

    public async Task<double> GetAccuracyMetricAsync(Guid userId, string engine, string? applicationContext = null, DateTime? since = null)
    {
        var query = _context.UserFeedbacks
            .Where(f => f.UserId == userId && f.Engine == engine);

        if (!string.IsNullOrEmpty(applicationContext))
        {
            query = query.Where(f => f.ApplicationContext == applicationContext);
        }

        if (since.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= since.Value);
        }

        var ratings = await query
            .Where(f => f.FeedbackType == "Rating" && f.Rating.HasValue)
            .Select(f => f.Rating!.Value)
            .ToListAsync();

        if (!ratings.Any())
            return 0.0;

        return ratings.Average() / 5.0; // Convert 1-5 scale to 0-1 scale
    }

    public async Task<IEnumerable<string>> SuggestCorrectionsAsync(Guid userId, string text, string? applicationContext = null)
    {
        // Look for partial matches in user's correction history
        var suggestions = await _context.UserFeedbacks
            .Where(f => f.UserId == userId && 
                       f.FeedbackType == "Correction" &&
                       (applicationContext == null || f.ApplicationContext == applicationContext) &&
                       f.OriginalText.Contains(text))
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.CorrectedText)
            .Distinct()
            .Take(5)
            .ToListAsync();

        return suggestions;
    }

    public async Task<IEnumerable<UserFeedbackEntity>> GetUnprocessedFeedbackAsync(int limit = 100)
    {
        return await _context.UserFeedbacks
            .Where(f => !f.IsProcessed)
            .OrderBy(f => f.CreatedAt)
            .Take(limit)
            .Include(f => f.User)
            .Include(f => f.TranscriptionEvent)
            .ToListAsync();
    }

    public async Task MarkFeedbackAsProcessedAsync(IEnumerable<Guid> feedbackIds)
    {
        var feedbacks = await _context.UserFeedbacks
            .Where(f => feedbackIds.Contains(f.Id))
            .ToListAsync();

        foreach (var feedback in feedbacks)
        {
            feedback.IsProcessed = true;
        }

        await _context.SaveChangesAsync();
    }
}