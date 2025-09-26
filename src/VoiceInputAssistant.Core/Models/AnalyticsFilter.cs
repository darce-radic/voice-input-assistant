namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Filter criteria for analytics queries
/// </summary>
public class AnalyticsFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ApplicationName { get; set; }
    public SpeechEngineType? Engine { get; set; }
    public string? Language { get; set; }
    public float? MinConfidence { get; set; }
    public float? MaxConfidence { get; set; }
    public float? MinAccuracy { get; set; }
    public float? MaxAccuracy { get; set; }
    public bool? SuccessfulOnly { get; set; }
    public List<string> Applications { get; set; } = new();
    public List<SpeechEngineType> Engines { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}