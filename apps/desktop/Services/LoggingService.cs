using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;
using System.Diagnostics;
using System.Reflection;

namespace VoiceInputAssistant.Services;

public static class LoggingService
{
    private static Logger? _logger;

    public static Logger CreateLogger(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var logPath = GetLogPath();
        var appName = Assembly.GetExecutingAssembly().GetName().Name ?? "VoiceInputAssistant";
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0";
        var isDevelopment = environment?.IsDevelopment() ?? Debugger.IsAttached;

        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .Enrich.WithProperty("ProcessName", Process.GetCurrentProcess().ProcessName)
            .Enrich.WithProperty("ThreadId", Environment.CurrentManagedThreadId)
            .Enrich.WithProperty("Application", appName)
            .Enrich.WithProperty("Version", version)
            .Enrich.WithProperty("Environment", environment?.EnvironmentName ?? "Development");

        // Console logging for development
        if (isDevelopment)
        {
            logConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code);
        }

        // File logging
        logConfig.WriteTo.File(
            Path.Combine(logPath, "voice-input-assistant-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Structured JSON logging for analysis
        logConfig.WriteTo.File(
            new JsonFormatter(),
            Path.Combine(logPath, "voice-input-assistant-structured-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);

        // Performance counter logging
        logConfig.WriteTo.File(
            Path.Combine(logPath, "performance-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            restrictedToMinimumLevel: LogEventLevel.Information,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj}{NewLine}");

        // Error-only logging for critical issues
        logConfig.WriteTo.File(
            Path.Combine(logPath, "errors-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 90,
            restrictedToMinimumLevel: LogEventLevel.Error,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {Level:u3} {SourceContext}: {Message:lj}{NewLine}{Exception}");

        // Cloud logging (if configured) - Sentry extension not available, skipping
        var sentryDsn = configuration["Sentry:Dsn"];
        if (!string.IsNullOrEmpty(sentryDsn))
        {
            // TODO: Add Sentry logging when Sentry.Serilog package is available
            // logConfig.WriteTo.Sentry(...);
        }

        // Application Insights (if configured)
        var applicationInsightsKey = configuration["ApplicationInsights:InstrumentationKey"];
        if (!string.IsNullOrEmpty(applicationInsightsKey))
        {
            // logConfig.WriteTo.ApplicationInsights(applicationInsightsKey, TelemetryConverter.Traces);
            // Application Insights not supported in desktop app
        }

        _logger = logConfig.CreateLogger();
        
        // Set global Serilog logger
        Log.Logger = _logger;

        // Log startup information
        _logger.Information("Voice Input Assistant starting up");
        _logger.Information("Version: {Version}, Environment: {Environment}", version, environment?.EnvironmentName ?? "Development");
        _logger.Information("Log path: {LogPath}", logPath);

        return _logger;
    }

    public static void LogPerformanceMetric(string operation, TimeSpan duration, string? context = null)
    {
        _logger?.Information("PERF: {Operation} completed in {Duration:F2}ms {Context}",
            operation, duration.TotalMilliseconds, context ?? "");
    }

    public static void LogSpeechRecognitionMetric(string engine, TimeSpan duration, int audioLengthMs, double confidence, bool success)
    {
        _logger?.Information("SPEECH: {Engine} - Duration: {ProcessingTime:F2}ms, Audio: {AudioLength}ms, Confidence: {Confidence:F2}, Success: {Success}",
            engine, duration.TotalMilliseconds, audioLengthMs, confidence, success);
    }

    public static void LogApiCall(string service, string endpoint, TimeSpan duration, int statusCode, string? error = null)
    {
        if (error != null)
        {
            _logger?.Error("API: {Service}/{Endpoint} failed - Duration: {Duration:F2}ms, Status: {StatusCode}, Error: {Error}",
                service, endpoint, duration.TotalMilliseconds, statusCode, error);
        }
        else
        {
            _logger?.Information("API: {Service}/{Endpoint} - Duration: {Duration:F2}ms, Status: {StatusCode}",
                service, endpoint, duration.TotalMilliseconds, statusCode);
        }
    }

    public static void LogUserAction(string action, string actionId)
    {
        _logger?.Information("USER: {Action} [{ActionId}]", action, actionId);
    }
    
    public static void LogUserAction(string action)
    {
        LogUserAction(action, Guid.NewGuid().ToString());
    }

    public static void LogSystemResource(string resource, double value, string unit)
    {
        _logger?.Information("RESOURCE: {Resource} = {Value} {Unit}", resource, value, unit);
    }

    private static string GetLogPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logPath = Path.Combine(appDataPath, "VoiceInputAssistant", "Logs");
        
        try
        {
            Directory.CreateDirectory(logPath);
        }
        catch (Exception ex)
        {
            // Fallback to temp directory
            logPath = Path.Combine(Path.GetTempPath(), "VoiceInputAssistant", "Logs");
            Directory.CreateDirectory(logPath);
            
            Console.WriteLine($"Warning: Could not create log directory in AppData. Using temp path: {logPath}. Error: {ex.Message}");
        }
        
        return logPath;
    }

    public static void Shutdown()
    {
        Log.Information("Voice Input Assistant shutting down");
        Log.CloseAndFlush();
        _logger?.Dispose();
    }
}