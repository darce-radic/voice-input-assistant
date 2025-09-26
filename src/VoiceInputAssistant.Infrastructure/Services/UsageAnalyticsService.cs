using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Infrastructure.Data;
using VoiceInputAssistant.Infrastructure.Data.Entities;

namespace VoiceInputAssistant.Infrastructure.Services;

/// <summary>
/// Service for tracking and analyzing usage metrics with database storage
/// </summary>
public class UsageAnalyticsService : IUsageAnalyticsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UsageAnalyticsService> _logger;
    private bool _isEnabled = true;

    public bool IsEnabled => _isEnabled;

    public UsageAnalyticsService(
        ApplicationDbContext dbContext,
        ILogger<UsageAnalyticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RecordRecognitionAsync(SpeechRecognitionResult result)
    {
        if (!_isEnabled) return;

        try
        {
            var entity = new TranscriptionEventEntity
            {
                Id = Guid.NewGuid(),
                Text = result.Text,
                ProcessedText = result.Text, // Could be different if post-processed
                ApplicationName = result.ApplicationName,
                Engine = result.Engine,
                ConfidenceScore = result.Confidence,
                ProcessingTimeMs = (int)result.ProcessingTime.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
                LanguageCode = result.Language,
                Metadata = result.Metadata
            };

            await _dbContext.TranscriptionEvents.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Recorded recognition event for application {ApplicationName}", result.ApplicationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record recognition event");
        }
    }

    public async Task RecordErrorAsync(string errorType, string message, Exception? exception = null)
    {
        if (!_isEnabled) return;

        try
        {
            var entity = new ErrorEventEntity
            {
                Id = Guid.NewGuid(),
                ErrorType = errorType,
                ErrorMessage = message,
                StackTrace = exception?.StackTrace,
                ApplicationContext = GetCurrentApplicationContext(),
                Timestamp = DateTime.UtcNow,
                Severity = GetSeverityFromException(exception),
                AdditionalData = exception != null ? new Dictionary<string, string>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["Source"] = exception.Source ?? ""
                } : null
            };

            await _dbContext.ErrorEvents.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Recorded error event: {ErrorType}", errorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record error event");
        }
    }

    public async Task RecordUserActionAsync(string action, Dictionary<string, object>? context = null)
    {
        if (!_isEnabled) return;

        try
        {
            // For now, we'll record user actions as a special type of transcription event
            var entity = new TranscriptionEventEntity
            {
                Id = Guid.NewGuid(),
                Text = $"UserAction:{action}",
                ProcessedText = action,
                ApplicationName = "System",
                Engine = SpeechEngineType.Unspecified,
                ConfidenceScore = 1.0f,
                ProcessingTimeMs = 0,
                Timestamp = DateTime.UtcNow,
                LanguageCode = "system",
                Metadata = context?.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "")
            };

            await _dbContext.TranscriptionEvents.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Recorded user action: {Action}", action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record user action");
        }
    }

    public async Task<IEnumerable<SpeechRecognitionResult>> GetRecentRecognitionsAsync(int count = 10)
    {
        try
        {
            var entities = await _dbContext.TranscriptionEvents
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();

            return entities.Select(e => new SpeechRecognitionResult
            {
                Text = e.Text,
                Confidence = e.ConfidenceScore,
                Engine = e.Engine,
                Language = e.LanguageCode,
                ProcessingTime = TimeSpan.FromMilliseconds(e.ProcessingTimeMs),
                ApplicationName = e.ApplicationName,
                Timestamp = e.Timestamp,
                Success = e.ConfidenceScore > 0.5f, // Use Success instead of IsSuccessful
                Metadata = e.Metadata
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent recognitions");
            return Enumerable.Empty<SpeechRecognitionResult>();
        }
    }

    public async Task<UsageStatistics> GetUsageStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            var statistics = new UsageStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRecognitions = transcriptions.Count,
                SuccessfulRecognitions = transcriptions.Count(t => t.ConfidenceScore > 0.5f),
                FailedRecognitions = transcriptions.Count(t => t.ConfidenceScore <= 0.5f),
                AverageAccuracy = transcriptions.Any() ? transcriptions.Average(t => t.ConfidenceScore) : 0,
                TotalUsageTime = TimeSpan.FromMilliseconds(transcriptions.Sum(t => t.ProcessingTimeMs)),
                TotalWordsRecognized = transcriptions.Sum(t => t.Text?.Split(' ')?.Length ?? 0),
                AverageProcessingTime = transcriptions.Any() ? TimeSpan.FromMilliseconds(transcriptions.Average(t => t.ProcessingTimeMs)) : TimeSpan.Zero
            };

            // Group by engine
            statistics.RecognitionsByEngine = transcriptions
                .GroupBy(t => t.Engine.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by application
            statistics.RecognitionsByApplication = transcriptions
                .Where(t => !string.IsNullOrEmpty(t.ApplicationName))
                .GroupBy(t => t.ApplicationName!)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by language
            statistics.RecognitionsByLanguage = transcriptions
                .Where(t => !string.IsNullOrEmpty(t.LanguageCode))
                .GroupBy(t => t.LanguageCode!)
                .ToDictionary(g => g.Key, g => g.Count());

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage statistics");
            return new UsageStatistics { StartDate = startDate, EndDate = endDate };
        }
    }

    public async Task<UsageStatistics> GetDailyUsageAsync(DateTime? date = null)
    {
        var targetDate = date ?? DateTime.Today;
        return await GetUsageStatisticsAsync(targetDate, targetDate.AddDays(1).AddTicks(-1));
    }

    public async Task<UsageStatistics> GetWeeklyUsageAsync(DateTime? weekStartDate = null)
    {
        var startDate = weekStartDate ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        return await GetUsageStatisticsAsync(startDate, startDate.AddDays(7).AddTicks(-1));
    }

    public async Task<UsageStatistics> GetMonthlyUsageAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);
        return await GetUsageStatisticsAsync(startDate, endDate);
    }

    public async Task<IEnumerable<ApplicationUsage>> GetTopApplicationsAsync(int count = 10, int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate)
                .Where(e => !string.IsNullOrEmpty(e.ApplicationName))
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            return transcriptions
                .GroupBy(t => t.ApplicationName!)
                .Select(g => new ApplicationUsage
                {
                    ApplicationName = g.Key,
                    ExecutablePath = g.Key, // We don't store separate executable path
                    RecognitionCount = g.Count(),
                    TotalUsageTime = TimeSpan.FromMilliseconds(g.Sum(t => t.ProcessingTimeMs)),
                    AverageAccuracy = g.Average(t => t.ConfidenceScore),
                    LastUsed = g.Max(t => t.Timestamp)
                })
                .OrderByDescending(a => a.RecognitionCount)
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top applications");
            return Enumerable.Empty<ApplicationUsage>();
        }
    }

    public async Task<IEnumerable<EngineAccuracyStatistics>> GetAccuracyStatisticsByEngineAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            return transcriptions
                .GroupBy(t => t.Engine)
                .Select(g => new EngineAccuracyStatistics
                {
                    EngineName = g.Key.ToString(),
                    TotalRecognitions = g.Count(),
                    AverageAccuracy = g.Average(t => t.ConfidenceScore),
                    MedianAccuracy = GetMedian(g.Select(t => (double)t.ConfidenceScore)),
                    AverageProcessingTime = TimeSpan.FromMilliseconds(g.Average(t => t.ProcessingTimeMs)),
                    ErrorCount = g.Count(t => t.ConfidenceScore <= 0.5f)
                })
                .OrderByDescending(s => s.AverageAccuracy)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get accuracy statistics by engine");
            return Enumerable.Empty<EngineAccuracyStatistics>();
        }
    }

    public async Task<IEnumerable<ErrorStatistics>> GetErrorStatisticsAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var errors = await _dbContext.ErrorEvents
                .Where(e => e.Timestamp >= startDate)
                .AsNoTracking()
                .ToListAsync();

            return errors
                .GroupBy(e => e.ErrorType)
                .Select(g => new ErrorStatistics
                {
                    ErrorType = g.Key,
                    Count = g.Count(),
                    LastOccurred = g.Max(e => e.Timestamp),
                    MostCommonMessage = g.GroupBy(e => e.ErrorMessage)
                                       .OrderByDescending(mg => mg.Count())
                                       .FirstOrDefault()?.Key ?? ""
                })
                .OrderByDescending(s => s.Count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error statistics");
            return Enumerable.Empty<ErrorStatistics>();
        }
    }

    public async Task ExportUsageDataAsync(DateTime startDate, DateTime endDate, string filePath, ExportFormat format = ExportFormat.Json)
    {
        try
        {
            var statistics = await GetUsageStatisticsAsync(startDate, endDate);
            var json = System.Text.Json.JsonSerializer.Serialize(statistics, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Exported usage data to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export usage data");
            throw;
        }
    }

    public async Task<int> CleanupOldDataAsync(int olderThanDays = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            
            var oldTranscriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp < cutoffDate)
                .ToListAsync();

            var oldErrors = await _dbContext.ErrorEvents
                .Where(e => e.Timestamp < cutoffDate)
                .ToListAsync();

            _dbContext.TranscriptionEvents.RemoveRange(oldTranscriptions);
            _dbContext.ErrorEvents.RemoveRange(oldErrors);

            await _dbContext.SaveChangesAsync();

            var totalDeleted = oldTranscriptions.Count + oldErrors.Count;
            _logger.LogInformation("Cleaned up {Count} old records", totalDeleted);
            
            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old data");
            throw;
        }
    }

    public Task SetEnabledAsync(bool enabled)
    {
        _isEnabled = enabled;
        _logger.LogInformation("Analytics collection {Status}", enabled ? "enabled" : "disabled");
        return Task.CompletedTask;
    }

    private static string GetCurrentApplicationContext()
    {
        try
        {
            return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        }
        catch
        {
            return "Unknown";
        }
    }

    private static int GetSeverityFromException(Exception? exception)
    {
        if (exception == null) return 1; // Warning
        if (exception is OutOfMemoryException || exception is StackOverflowException) return 3; // Critical
        if (exception is ArgumentException || exception is InvalidOperationException) return 2; // Error
        return 1; // Warning
    }

    private static double GetMedian(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        if (sorted.Count == 0) return 0;
        if (sorted.Count == 1) return sorted[0];

        if (sorted.Count % 2 == 0)
        {
            return (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;
        }
        else
        {
            return sorted[sorted.Count / 2];
        }
    }

    // New methods implementation
    public async Task<object> GetDetailedAnalyticsAsync(object filter)
    {
        try
        {
            // Basic implementation - return comprehensive analytics data
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            
            var statistics = await GetUsageStatisticsAsync(startDate, endDate);
            var topApps = await GetTopApplicationsAsync();
            var engineStats = await GetAccuracyStatisticsByEngineAsync();
            var errorStats = await GetErrorStatisticsAsync();
            
            return new
            {
                Statistics = statistics,
                TopApplications = topApps,
                EngineStatistics = engineStats,
                ErrorStatistics = errorStats,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get detailed analytics");
            return new { Error = "Failed to retrieve detailed analytics" };
        }
    }

    public async Task<object> GetPerformanceMetricsAsync()
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-7);
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            return new
            {
                AverageProcessingTime = transcriptions.Any() ? transcriptions.Average(t => t.ProcessingTimeMs) : 0,
                MedianProcessingTime = GetMedian(transcriptions.Select(t => (double)t.ProcessingTimeMs)),
                TotalProcessedRequests = transcriptions.Count,
                SuccessRate = transcriptions.Any() ? (double)transcriptions.Count(t => t.ConfidenceScore > 0.5f) / transcriptions.Count * 100 : 0,
                AverageConfidence = transcriptions.Any() ? transcriptions.Average(t => t.ConfidenceScore) : 0,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics");
            return new { Error = "Failed to retrieve performance metrics" };
        }
    }

    public async Task<object> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate, string metricType)
    {
        try
        {
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            var result = new
            {
                DateRange = new { Start = startDate, End = endDate },
                MetricType = metricType,
                AverageProcessingTime = transcriptions.Any() ? transcriptions.Average(t => t.ProcessingTimeMs) : 0,
                MedianProcessingTime = GetMedian(transcriptions.Select(t => (double)t.ProcessingTimeMs)),
                TotalProcessedRequests = transcriptions.Count,
                SuccessRate = transcriptions.Any() ? (double)transcriptions.Count(t => t.ConfidenceScore > 0.5f) / transcriptions.Count * 100 : 0,
                AverageConfidence = transcriptions.Any() ? transcriptions.Average(t => t.ConfidenceScore) : 0,
                MinProcessingTime = transcriptions.Any() ? transcriptions.Min(t => t.ProcessingTimeMs) : 0,
                MaxProcessingTime = transcriptions.Any() ? transcriptions.Max(t => t.ProcessingTimeMs) : 0,
                GeneratedAt = DateTime.UtcNow
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics with filter");
            return new { Error = "Failed to retrieve performance metrics" };
        }
    }

    public async Task<object> GetApplicationInsightsAsync()
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-30);
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate)
                .Where(e => !string.IsNullOrEmpty(e.ApplicationName))
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            var appInsights = transcriptions
                .GroupBy(t => t.ApplicationName)
                .Select(g => new
                {
                    ApplicationName = g.Key,
                    UsageCount = g.Count(),
                    AverageAccuracy = g.Average(t => t.ConfidenceScore),
                    AverageProcessingTime = g.Average(t => t.ProcessingTimeMs),
                    LastUsed = g.Max(t => t.Timestamp),
                    TotalWordsProcessed = g.Sum(t => t.Text?.Split(' ')?.Length ?? 0)
                })
                .OrderByDescending(a => a.UsageCount)
                .ToList();

            return new
            {
                Applications = appInsights,
                TotalApplications = appInsights.Count,
                MostUsedApp = appInsights.FirstOrDefault()?.ApplicationName,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get application insights");
            return new { Error = "Failed to retrieve application insights" };
        }
    }

    public async Task<object> GetApplicationInsightsAsync(DateTime startDate, DateTime endDate, string insightType)
    {
        try
        {
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .Where(e => !string.IsNullOrEmpty(e.ApplicationName))
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            var appInsights = transcriptions
                .GroupBy(t => t.ApplicationName)
                .Select(g => new
                {
                    ApplicationName = g.Key,
                    UsageCount = g.Count(),
                    AverageAccuracy = g.Average(t => t.ConfidenceScore),
                    AverageProcessingTime = g.Average(t => t.ProcessingTimeMs),
                    LastUsed = g.Max(t => t.Timestamp),
                    TotalWordsProcessed = g.Sum(t => t.Text?.Split(' ')?.Length ?? 0),
                    SuccessRate = (double)g.Count(t => t.ConfidenceScore > 0.5f) / g.Count() * 100,
                    ErrorRate = (double)g.Count(t => t.ConfidenceScore <= 0.5f) / g.Count() * 100
                })
                .OrderByDescending(a => a.UsageCount)
                .ToList();

            return new
            {
                DateRange = new { Start = startDate, End = endDate },
                InsightType = insightType,
                Applications = appInsights,
                TotalApplications = appInsights.Count,
                MostUsedApp = appInsights.FirstOrDefault()?.ApplicationName,
                BestPerformingApp = appInsights.OrderByDescending(a => a.AverageAccuracy).FirstOrDefault()?.ApplicationName,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get application insights with filter");
            return new { Error = "Failed to retrieve application insights" };
        }
    }

    public async Task<object> GetEngineComparisonAsync()
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-30);
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            var engineComparison = transcriptions
                .GroupBy(t => t.Engine)
                .Select(g => new
                {
                    EngineName = g.Key.ToString(),
                    UsageCount = g.Count(),
                    AverageAccuracy = g.Average(t => t.ConfidenceScore),
                    AverageProcessingTime = g.Average(t => t.ProcessingTimeMs),
                    SuccessRate = (double)g.Count(t => t.ConfidenceScore > 0.5f) / g.Count() * 100,
                    TotalProcessingTime = g.Sum(t => t.ProcessingTimeMs)
                })
                .OrderByDescending(e => e.AverageAccuracy)
                .ToList();

            return new
            {
                EngineComparison = engineComparison,
                BestPerformingEngine = engineComparison.FirstOrDefault()?.EngineName,
                TotalEngines = engineComparison.Count,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine comparison");
            return new { Error = "Failed to retrieve engine comparison" };
        }
    }

    public async Task<object> GetEngineComparisonAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            var engineComparison = transcriptions
                .GroupBy(t => t.Engine)
                .Select(g => new
                {
                    EngineName = g.Key.ToString(),
                    UsageCount = g.Count(),
                    AverageAccuracy = g.Average(t => t.ConfidenceScore),
                    AverageProcessingTime = g.Average(t => t.ProcessingTimeMs),
                    SuccessRate = (double)g.Count(t => t.ConfidenceScore > 0.5f) / g.Count() * 100,
                    ErrorRate = (double)g.Count(t => t.ConfidenceScore <= 0.5f) / g.Count() * 100,
                    TotalProcessingTime = g.Sum(t => t.ProcessingTimeMs),
                    MinProcessingTime = g.Min(t => t.ProcessingTimeMs),
                    MaxProcessingTime = g.Max(t => t.ProcessingTimeMs),
                    MedianAccuracy = GetMedian(g.Select(t => (double)t.ConfidenceScore))
                })
                .OrderByDescending(e => e.AverageAccuracy)
                .ToList();

            return new
            {
                DateRange = new { Start = startDate, End = endDate },
                EngineComparison = engineComparison,
                BestPerformingEngine = engineComparison.FirstOrDefault()?.EngineName,
                FastestEngine = engineComparison.OrderBy(e => e.AverageProcessingTime).FirstOrDefault()?.EngineName,
                MostUsedEngine = engineComparison.OrderByDescending(e => e.UsageCount).FirstOrDefault()?.EngineName,
                TotalEngines = engineComparison.Count,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine comparison with date range");
            return new { Error = "Failed to retrieve engine comparison" };
        }
    }

    public async Task<object> ExportDataAsync(object request)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            
            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                DateRange = new { Start = startDate, End = endDate },
                Statistics = await GetUsageStatisticsAsync(startDate, endDate),
                Applications = await GetTopApplicationsAsync(50),
                EngineStatistics = await GetAccuracyStatisticsByEngineAsync(),
                ErrorStatistics = await GetErrorStatisticsAsync()
            };

            return new
            {
                Success = true,
                Data = exportData,
                RecordCount = exportData.Statistics.TotalRecognitions,
                ExportedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data");
            return new { Success = false, Error = "Failed to export data" };
        }
    }

    public async Task<object> GetRealtimeAnalyticsAsync()
    {
        try
        {
            var last24Hours = DateTime.UtcNow.AddDays(-1);
            var lastHour = DateTime.UtcNow.AddHours(-1);
            
            var recentTranscriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= lastHour)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            var dailyTranscriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= last24Hours)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .CountAsync();

            return new
            {
                CurrentHourActivity = recentTranscriptions.Count,
                Last24HourActivity = dailyTranscriptions,
                AverageProcessingTimeLastHour = recentTranscriptions.Any() ? recentTranscriptions.Average(t => t.ProcessingTimeMs) : 0,
                ActiveEngines = recentTranscriptions.Select(t => t.Engine.ToString()).Distinct().ToList(),
                LastActivity = recentTranscriptions.Any() ? recentTranscriptions.Max(t => t.Timestamp) : (DateTime?)null,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get realtime analytics");
            return new { Error = "Failed to retrieve realtime analytics" };
        }
    }

    public async Task<object> GetPredictiveAnalyticsAsync()
    {
        try
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);
            var transcriptions = await _dbContext.TranscriptionEvents
                .Where(e => e.Timestamp >= last30Days)
                .Where(e => !e.Text.StartsWith("UserAction:"))
                .AsNoTracking()
                .ToListAsync();

            // Simple trend analysis
            var dailyUsage = transcriptions
                .GroupBy(t => t.Timestamp.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var averageDailyUsage = dailyUsage.Any() ? dailyUsage.Average(d => d.Count) : 0;
            var trend = dailyUsage.Count > 1 ? 
                (dailyUsage.TakeLast(7).Average(d => d.Count) - dailyUsage.Take(7).Average(d => d.Count)) : 0;

            return new
            {
                AverageDailyUsage = averageDailyUsage,
                WeeklyTrend = trend > 0 ? "Increasing" : trend < 0 ? "Decreasing" : "Stable",
                PredictedNextWeekUsage = Math.Max(0, averageDailyUsage + trend) * 7,
                DailyUsagePattern = dailyUsage,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get predictive analytics");
            return new { Error = "Failed to retrieve predictive analytics" };
        }
    }

    public Task<object> CreateDashboardAsync(object dashboardRequest)
    {
        try
        {
            // Simple dashboard creation - return a structured dashboard object
            var dashboard = new
            {
                Id = Guid.NewGuid(),
                Name = "Usage Analytics Dashboard",
                CreatedAt = DateTime.UtcNow,
                Widgets = new[]
                {
                    new { Type = "UsageStatistics", Title = "Usage Overview" },
                    new { Type = "EngineComparison", Title = "Engine Performance" },
                    new { Type = "ApplicationInsights", Title = "Application Usage" },
                    new { Type = "ErrorStatistics", Title = "Error Analysis" }
                },
                RefreshInterval = TimeSpan.FromMinutes(5)
            };

            return Task.FromResult(new
            {
                Success = true,
                Dashboard = dashboard,
                Message = "Dashboard created successfully"
            } as object);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create dashboard");
            return Task.FromResult(new { Success = false, Error = "Failed to create dashboard" } as object);
        }
    }

    public Task<object> GetUserDashboardsAsync(string userId)
    {
        try
        {
            // Simple implementation - return mock dashboard data
            // In a real implementation, this would query user-specific dashboards from database
            var dashboards = new[]
            {
                new
                {
                    Id = Guid.NewGuid(),
                    Name = "Main Dashboard",
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    LastModified = DateTime.UtcNow.AddDays(-1),
                    IsDefault = true
                },
                new
                {
                    Id = Guid.NewGuid(),
                    Name = "Performance Dashboard",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    LastModified = DateTime.UtcNow,
                    IsDefault = false
                }
            };

            return Task.FromResult(new
            {
                UserId = userId,
                Dashboards = dashboards,
                Count = dashboards.Length,
                GeneratedAt = DateTime.UtcNow
            } as object);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user dashboards");
            return Task.FromResult(new { Error = "Failed to retrieve user dashboards" } as object);
        }
    }
}
