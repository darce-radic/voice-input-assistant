using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Configuration;
using System;
using System.Reflection;

namespace VoiceInputAssistant.Services.Logging;

public static class LoggingExtensions
{
    public static LoggerConfiguration WithMachineName(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));

        return enrichmentConfiguration.WithProperty("MachineName", Environment.MachineName);
    }
}

/// <summary>
/// Simple telemetry converter for Application Insights
/// </summary>
public class TelemetryConverter
{
    public static readonly TelemetryConverter Traces = new();

    private TelemetryConverter() { }

    public void Process(string logEvent)
    {
        // We're not actually using Application Insights for this desktop app
        // Just implementing the interface for compatibility
    }
}