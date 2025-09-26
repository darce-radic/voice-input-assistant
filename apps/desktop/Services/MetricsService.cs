using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services;

public interface IMetricsService
{
    void RecordSpeechRecognitionAttempt(string engine, TimeSpan duration, double confidence, bool success);
    void RecordApiCall(string service, string endpoint, TimeSpan duration, int statusCode);
    void RecordUserAction(string action);
    void RecordError(string category, string error);
    void RecordPerformanceMetric(string operation, TimeSpan duration);
    Task<MetricsSnapshot> GetMetricsSnapshotAsync();
}

public class MetricsService : BackgroundService, IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _speechRecognitionAttempts;
    private readonly Counter<long> _speechRecognitionSuccesses;
    private readonly Counter<long> _speechRecognitionFailures;
    private readonly Counter<long> _apiCalls;
    private readonly Counter<long> _apiFailures;
    private readonly Counter<long> _userActions;
    private readonly Counter<long> _errors;
    
    // Histograms
    private readonly Histogram<double> _speechRecognitionDuration;
    private readonly Histogram<double> _speechRecognitionConfidence;
    private readonly Histogram<double> _apiCallDuration;
    private readonly Histogram<double> _operationDuration;
    
    // Gauges (simulated with concurrent dictionaries)
    private readonly ConcurrentDictionary<string, double> _gauges;
    private readonly ConcurrentDictionary<string, long> _counters;
    
    // Session tracking
    private readonly DateTime _sessionStart;
    private readonly ConcurrentDictionary<string, EngineMetrics> _engineMetrics;
    private readonly ConcurrentDictionary<string, long> _actionCounts;
    private readonly ConcurrentQueue<ErrorEvent> _recentErrors;
    private readonly object _metricsLock = new();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
        _sessionStart = DateTime.UtcNow;
        
        // Initialize OpenTelemetry meter
        _meter = new Meter("VoiceInputAssistant", "1.0.0");
        
        // Initialize counters
        _speechRecognitionAttempts = _meter.CreateCounter<long>(
            "speech_recognition_attempts_total",
            description: "Total number of speech recognition attempts");
            
        _speechRecognitionSuccesses = _meter.CreateCounter<long>(
            "speech_recognition_successes_total",
            description: "Total number of successful speech recognition attempts");
            
        _speechRecognitionFailures = _meter.CreateCounter<long>(
            "speech_recognition_failures_total",
            description: "Total number of failed speech recognition attempts");
            
        _apiCalls = _meter.CreateCounter<long>(
            "api_calls_total",
            description: "Total number of API calls made");
            
        _apiFailures = _meter.CreateCounter<long>(
            "api_failures_total",
            description: "Total number of failed API calls");
            
        _userActions = _meter.CreateCounter<long>(
            "user_actions_total",
            description: "Total number of user actions");
            
        _errors = _meter.CreateCounter<long>(
            "errors_total",
            description: "Total number of errors");
        
        // Initialize histograms
        _speechRecognitionDuration = _meter.CreateHistogram<double>(
            "speech_recognition_duration_ms",
            "ms",
            "Duration of speech recognition operations");
            
        _speechRecognitionConfidence = _meter.CreateHistogram<double>(
            "speech_recognition_confidence",
            description: "Confidence scores of speech recognition results");
            
        _apiCallDuration = _meter.CreateHistogram<double>(
            "api_call_duration_ms",
            "ms", 
            "Duration of API calls");
            
        _operationDuration = _meter.CreateHistogram<double>(
            "operation_duration_ms",
            "ms",
            "Duration of various operations");
        
        // Initialize collections
        _gauges = new ConcurrentDictionary<string, double>();
        _counters = new ConcurrentDictionary<string, long>();
        _engineMetrics = new ConcurrentDictionary<string, EngineMetrics>();
        _actionCounts = new ConcurrentDictionary<string, long>();
        _recentErrors = new ConcurrentQueue<ErrorEvent>();
        
        _logger.LogInformation("Metrics service initialized");
    }

    public void RecordSpeechRecognitionAttempt(string engine, TimeSpan duration, double confidence, bool success)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("engine", engine),
            new("success", success)
        };
        
        _speechRecognitionAttempts.Add(1, tags);
        _speechRecognitionDuration.Record(duration.TotalMilliseconds, tags);
        
        if (success)
        {
            _speechRecognitionSuccesses.Add(1, tags);
            _speechRecognitionConfidence.Record(confidence, tags);
        }
        else
        {
            _speechRecognitionFailures.Add(1, tags);
        }
        
        // Update engine-specific metrics
        _engineMetrics.AddOrUpdate(engine, 
            new EngineMetrics 
            { 
                TotalAttempts = 1, 
                SuccessfulAttempts = success ? 1 : 0,
                TotalDurationMs = duration.TotalMilliseconds,
                TotalConfidence = success ? confidence : 0
            },
            (key, existing) => new EngineMetrics
            {
                TotalAttempts = existing.TotalAttempts + 1,
                SuccessfulAttempts = existing.SuccessfulAttempts + (success ? 1 : 0),
                TotalDurationMs = existing.TotalDurationMs + duration.TotalMilliseconds,
                TotalConfidence = existing.TotalConfidence + (success ? confidence : 0)
            });
            
        LoggingService.LogSpeechRecognitionMetric(engine, duration, 0, confidence, success);
    }

    public void RecordApiCall(string service, string endpoint, TimeSpan duration, int statusCode)
    {
        var isSuccess = statusCode >= 200 && statusCode < 300;
        var tags = new KeyValuePair<string, object?>[]
        {
            new("service", service),
            new("endpoint", endpoint),
            new("status_code", statusCode),
            new("success", isSuccess)
        };
        
        _apiCalls.Add(1, tags);
        _apiCallDuration.Record(duration.TotalMilliseconds, tags);
        
        if (!isSuccess)
        {
            _apiFailures.Add(1, tags);
        }
        
        LoggingService.LogApiCall(service, endpoint, duration, statusCode);
    }

    public void RecordUserAction(string action, string actionId)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("action", action),
            new("action_id", actionId)
        };
        
        _userActions.Add(1, tags);
        _actionCounts.AddOrUpdate(action, 1, (key, count) => count + 1);
        
        LoggingService.LogUserAction(action, actionId);
    }

    public void RecordUserAction(string action)
    {
        RecordUserAction(action, Guid.NewGuid().ToString());
    }

    public void RecordError(string category, string error)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("category", category),
            new("error_type", error)
        };
        
        _errors.Add(1, tags);
        
        var errorEvent = new ErrorEvent
        {
            Timestamp = DateTime.UtcNow,
            Category = category,
            Error = error
        };
        
        _recentErrors.Enqueue(errorEvent);
        
        // Keep only last 100 errors
        while (_recentErrors.Count > 100)
        {
            _recentErrors.TryDequeue(out _);
        }
        
        _logger.LogError("Error recorded: {Category} - {Error}", category, error);
    }

    public void RecordPerformanceMetric(string operation, TimeSpan duration)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("operation", operation)
        };
        
        _operationDuration.Record(duration.TotalMilliseconds, tags);
        LoggingService.LogPerformanceMetric(operation, duration);
    }

    public Task<MetricsSnapshot> GetMetricsSnapshotAsync()
    {
        lock (_metricsLock)
        {
            var snapshot = new MetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                SessionDuration = DateTime.UtcNow - _sessionStart,
                EngineMetrics = new Dictionary<string, EngineStats>(),
                ActionCounts = new Dictionary<string, long>(_actionCounts),
                RecentErrors = new List<ErrorEvent>(_recentErrors),
                SystemMetrics = GetCurrentSystemMetrics()
            };
            
            // Calculate engine statistics
            foreach (var kvp in _engineMetrics)
            {
                var metrics = kvp.Value;
                snapshot.EngineMetrics[kvp.Key] = new EngineStats
                {
                    TotalAttempts = metrics.TotalAttempts,
                    SuccessfulAttempts = metrics.SuccessfulAttempts,
                    SuccessRate = metrics.TotalAttempts > 0 ? (double)metrics.SuccessfulAttempts / metrics.TotalAttempts : 0,
                    AverageDurationMs = metrics.TotalAttempts > 0 ? metrics.TotalDurationMs / metrics.TotalAttempts : 0,
                    AverageConfidence = metrics.SuccessfulAttempts > 0 ? metrics.TotalConfidence / metrics.SuccessfulAttempts : 0
                };
            }
            
            return Task.FromResult(snapshot);
        }
    }

    private SystemMetrics GetCurrentSystemMetrics()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return new SystemMetrics
            {
                MemoryUsageMB = process.WorkingSet64 / 1024.0 / 1024.0,
                CpuUsagePercent = 0, // Would need calculation over time
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get system metrics");
            return new SystemMetrics();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics background service starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Periodic metrics collection and reporting
                var snapshot = await GetMetricsSnapshotAsync();
                
                // Update gauges with current values
                _gauges["session_duration_minutes"] = snapshot.SessionDuration.TotalMinutes;
                _gauges["memory_usage_mb"] = snapshot.SystemMetrics.MemoryUsageMB;
                _gauges["thread_count"] = snapshot.SystemMetrics.ThreadCount;
                _gauges["handle_count"] = snapshot.SystemMetrics.HandleCount;
                
                // Log summary every 5 minutes
                _logger.LogInformation("Metrics summary - Session: {SessionDuration:F1}min, Memory: {MemoryUsage:F1}MB, Actions: {ActionCount}",
                    snapshot.SessionDuration.TotalMinutes,
                    snapshot.SystemMetrics.MemoryUsageMB,
                    snapshot.ActionCounts.Values.Sum());
                
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics background service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        
        _logger.LogInformation("Metrics background service stopping");
    }

    public override void Dispose()
    {
        _meter?.Dispose();
        base.Dispose();
    }
}

public class EngineMetrics
{
    public long TotalAttempts { get; set; }
    public long SuccessfulAttempts { get; set; }
    public double TotalDurationMs { get; set; }
    public double TotalConfidence { get; set; }
}

public class EngineStats
{
    public long TotalAttempts { get; set; }
    public long SuccessfulAttempts { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDurationMs { get; set; }
    public double AverageConfidence { get; set; }
}

public class ErrorEvent
{
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public Dictionary<string, EngineStats> EngineMetrics { get; set; } = new();
    public Dictionary<string, long> ActionCounts { get; set; } = new();
    public List<ErrorEvent> RecentErrors { get; set; } = new();
    public SystemMetrics SystemMetrics { get; set; } = new();
}