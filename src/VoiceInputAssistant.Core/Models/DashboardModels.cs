namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Analytics dashboard configuration
/// </summary>
public class AnalyticsDashboard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public List<DashboardWidget> Widgets { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDefault { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    
    /// <summary>
    /// Dashboard layout configuration
    /// </summary>
    public string Layout { get; set; } = "grid";
    
    /// <summary>
    /// Whether the dashboard is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; }
    
    /// <summary>
    /// User who created this dashboard
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Dashboard widget configuration
/// </summary>
public class DashboardWidget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    /// <summary>
    /// Widget position (alternative to X, Y)
    /// </summary>
    public Dictionary<string, int> Position { get; set; } = new() { { "x", 0 }, { "y", 0 } };
    
    /// <summary>
    /// Widget size (alternative to Width, Height)
    /// </summary>
    public Dictionary<string, int> Size { get; set; } = new() { { "width", 1 }, { "height", 1 } };
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string DataSource { get; set; } = string.Empty;
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Widget types available for dashboards
/// </summary>
public enum DashboardWidgetType
{
    UsageChart,
    AccuracyMetric,
    ErrorRate,
    TopApplications,
    EngineComparison,
    RealtimeActivity,
    PerformanceMetrics
}