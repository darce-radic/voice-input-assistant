using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for usage analytics service
/// </summary>
public interface IUsageAnalyticsService
{
    Task<List<SpeechRecognitionResult>> GetRecentRecognitionsAsync(int count);
    Task RecordRecognitionAsync(SpeechRecognitionResult result);
    Task RecordErrorAsync(string error, DateTime timestamp);
}