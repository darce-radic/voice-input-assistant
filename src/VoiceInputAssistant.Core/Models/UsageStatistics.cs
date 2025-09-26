namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Represents usage statistics for a specified time period
/// </summary>
public class UsageStatistics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalRecognitions { get; set; }
    public int SuccessfulRecognitions { get; set; }
    public int FailedRecognitions { get; set; }
    public double AverageAccuracy { get; set; }
    public TimeSpan TotalUsageTime { get; set; }
    public int TotalWordsRecognized { get; set; }
    public int TotalWords => TotalWordsRecognized; // Alias for compatibility
    public TimeSpan AverageProcessingTime { get; set; }
    public Dictionary<string, int> RecognitionsByEngine { get; set; } = new();
    public Dictionary<string, int> RecognitionsByApplication { get; set; } = new();
    public Dictionary<string, int> RecognitionsByLanguage { get; set; } = new();
    public Dictionary<string, TimeSpan> UsageTimeByApplication { get; set; } = new();
}