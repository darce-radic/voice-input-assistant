namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Request model for generating reports
/// </summary>
public class ReportRequest
{
    public ReportType Type { get; set; }
    public ReportFormat Format { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public AnalyticsFilter? Filter { get; set; }
    public List<string> IncludedMetrics { get; set; } = new();
    public List<string> CustomMetrics { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeDetails { get; set; } = true;
    public ReportType ReportType { get; set; }
    public AnalyticsFilter? Filters { get; set; }
}

/// <summary>
/// Types of reports that can be generated
/// </summary>
public enum ReportType
{
    Usage,
    Performance,
    Accuracy,
    Errors,
    Applications,
    Engines,
    Comprehensive
}

/// <summary>
/// Available formats for report export
/// </summary>
public enum ReportFormat
{
    Json,
    Csv,
    Excel,
    Pdf,
    Html
}