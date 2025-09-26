using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services;

/// <summary>
/// Basic implementation of usage analytics service
/// </summary>
public class UsageAnalyticsService : IUsageAnalyticsService
{
    private readonly ILogger<UsageAnalyticsService> _logger;

    public UsageAnalyticsService(ILogger<UsageAnalyticsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<SpeechRecognitionResult>> GetRecentRecognitionsAsync(int count)
    {
        await Task.Delay(10);
        return new List<SpeechRecognitionResult>();
    }

    public async Task RecordRecognitionAsync(SpeechRecognitionResult result)
    {
        _logger.LogDebug("Recording recognition result: {Text}", result.Text);
        await Task.Delay(1);
    }

    public async Task RecordErrorAsync(string error, DateTime timestamp)
    {
        _logger.LogDebug("Recording error: {Error} at {Timestamp}", error, timestamp);
        await Task.Delay(1);
    }
}