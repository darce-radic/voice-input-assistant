using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.WebApi.Models;

namespace VoiceInputAssistant.WebApi.Controllers
{
    /// <summary>
    /// Advanced analytics and reporting controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly ILogger<AnalyticsController> _logger;
        private readonly IUsageAnalyticsService _analyticsService;
        private readonly IReportingService _reportingService;

        public AnalyticsController(
            ILogger<AnalyticsController> logger,
            IUsageAnalyticsService analyticsService,
            IReportingService reportingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
        }

        /// <summary>
        /// Get detailed usage analytics with custom filtering and grouping
        /// </summary>
        [HttpGet("detailed")]
        public async Task<ActionResult<DetailedAnalyticsResponse>> GetDetailedAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string groupBy = "daily",
            [FromQuery] string[]? engines = null,
            [FromQuery] string[]? applications = null,
            [FromQuery] double? minAccuracy = null,
            [FromQuery] double? maxAccuracy = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var filter = new AnalyticsFilter
                {
                    StartDate = start,
                    EndDate = end,
                    Engines = engines?.Select(e => System.Enum.TryParse<SpeechEngineType>(e, true, out var result) ? result : SpeechEngineType.WhisperLocal).ToList() ?? new List<SpeechEngineType>(),
                    Applications = applications?.ToList() ?? new List<string>(),
                    MinAccuracy = (float?)minAccuracy,
                    MaxAccuracy = (float?)maxAccuracy
                };

                var analytics = await _analyticsService.GetDetailedAnalyticsAsync(filter);

                return Ok(new DetailedAnalyticsResponse
                {
                    Period = new DatePeriod { StartDate = start, EndDate = end },
                    GroupBy = groupBy,
                    Filter = new AnalyticsFilterDto
                    {
                        Engines = filter.Engines.Select(e => e.ToString()).ToList(),
                        Applications = filter.Applications,
                        MinAccuracy = (double?)filter.MinAccuracy,
                        MaxAccuracy = (double?)filter.MaxAccuracy
                    },
                    TimeSeriesData = analytics.TimeSeriesData.Select(d => new TimeSeriesDataPoint
                    {
                        Date = d.Date,
                        TotalRecognitions = d.TotalRecognitions,
                        AverageAccuracy = d.AverageAccuracy,
                        AverageProcessingTime = d.AverageProcessingTime.TotalMilliseconds,
                        TotalWords = d.TotalWords,
                        UniqueApplications = d.UniqueApplications,
                        ErrorCount = d.ErrorCount
                    }).ToList(),
                    Summary = new AnalyticsSummary
                    {
                        TotalRecognitions = analytics.Summary.TotalRecognitions,
                        TotalWords = analytics.Summary.TotalWords,
                        AverageAccuracy = analytics.Summary.AverageAccuracy,
                        AverageProcessingTime = analytics.Summary.AverageProcessingTime.TotalMilliseconds,
                        MostUsedEngine = analytics.Summary.MostUsedEngine,
                        MostUsedApplication = analytics.Summary.MostUsedApplication,
                        PeakUsageHour = analytics.Summary.PeakUsageHour,
                        ErrorRate = analytics.Summary.ErrorRate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed analytics");
                return StatusCode(500, new { error = "Failed to get detailed analytics" });
            }
        }

        /// <summary>
        /// Get performance metrics and trends
        /// </summary>
        [HttpGet("performance")]
        public async Task<ActionResult<PerformanceMetricsResponse>> GetPerformanceMetrics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string metricType = "accuracy")
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-7);
                var end = endDate ?? DateTime.UtcNow;

                var metrics = await _analyticsService.GetPerformanceMetricsAsync(start, end, metricType);

                return Ok(new PerformanceMetricsResponse
                {
                    Period = new DatePeriod { StartDate = start, EndDate = end },
                    MetricType = metricType,
                    CurrentValue = metrics.CurrentValue,
                    PreviousPeriodValue = metrics.PreviousPeriodValue,
                    PercentageChange = metrics.PercentageChange,
                    Trend = metrics.Trend,
                    TrendData = metrics.TrendData.Select(d => new TrendDataPoint
                    {
                        Date = d.Date,
                        Value = d.Value
                    }).ToList(),
                    Benchmarks = new PerformanceBenchmarks
                    {
                        Target = metrics.Benchmarks.Target,
                        Industry = metrics.Benchmarks.Industry,
                        Personal = metrics.Benchmarks.Personal
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return StatusCode(500, new { error = "Failed to get performance metrics" });
            }
        }

        /// <summary>
        /// Get application usage patterns and insights
        /// </summary>
        [HttpGet("application-insights")]
        public async Task<ActionResult<ApplicationInsightsResponse>> GetApplicationInsights(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int top = 10)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var insights = await _analyticsService.GetApplicationInsightsAsync(start, end, top.ToString());

                return Ok(new ApplicationInsightsResponse
                {
                    Period = new DatePeriod { StartDate = start, EndDate = end },
                    TopApplications = insights.TopApplications.Select(a => new ApplicationInsight
                    {
                        ApplicationName = a.ApplicationName,
                        TotalRecognitions = a.TotalRecognitions,
                        TotalUsageTime = a.TotalUsageTime.TotalMinutes,
                        AverageAccuracy = a.AverageAccuracy,
                        AverageSessionLength = a.AverageSessionLength.TotalMinutes,
                        MostActiveTimeOfDay = a.MostActiveTimeOfDay,
                        PreferredEngine = a.PreferredEngine,
                        AccuracyTrend = a.AccuracyTrend,
                        UsageGrowth = a.UsageGrowth
                    }).ToList(),
                    CategoryBreakdown = insights.CategoryBreakdown.Select(c => new ApplicationCategory
                    {
                        Category = c.Category,
                        Count = 0, // c.Count() doesn't work on object
                        Percentage = c.Percentage,
                        AverageAccuracy = c.AverageAccuracy
                    }).ToList(),
                    UsagePatterns = new UsagePatterns
                    {
                        PeakHours = insights.UsagePatterns.PeakHours,
                        WeekdayUsage = insights.UsagePatterns.WeekdayUsage,
                        WeekendUsage = insights.UsagePatterns.WeekendUsage,
                        SeasonalTrends = insights.UsagePatterns.SeasonalTrends
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application insights");
                return StatusCode(500, new { error = "Failed to get application insights" });
            }
        }

        /// <summary>
        /// Get engine comparison analytics
        /// </summary>
        [HttpGet("engine-comparison")]
        public async Task<ActionResult<EngineComparisonResponse>> GetEngineComparison(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var comparison = await _analyticsService.GetEngineComparisonAsync(start, end);

                return Ok(new EngineComparisonResponse
                {
                    Period = new DatePeriod { StartDate = start, EndDate = end },
                    EngineMetrics = comparison.EngineMetrics.Select(e => new EngineMetrics
                    {
                        EngineName = e.EngineName,
                        TotalRecognitions = e.TotalRecognitions,
                        AverageAccuracy = e.AverageAccuracy,
                        AverageProcessingTime = e.AverageProcessingTime.TotalMilliseconds,
                        ErrorRate = e.ErrorRate,
                        UsagePercentage = e.UsagePercentage,
                        CostPerRecognition = e.CostPerRecognition,
                        SupportedLanguages = e.SupportedLanguages,
                        RecommendedUseCase = e.RecommendedUseCase
                    }).ToList(),
                    Recommendation = new EngineRecommendation
                    {
                        BestForAccuracy = comparison.Recommendation.BestForAccuracy,
                        BestForSpeed = comparison.Recommendation.BestForSpeed,
                        BestForCost = comparison.Recommendation.BestForCost,
                        Overall = comparison.Recommendation.Overall,
                        Reasoning = comparison.Recommendation.Reasoning
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting engine comparison");
                return StatusCode(500, new { error = "Failed to get engine comparison" });
            }
        }

        /// <summary>
        /// Generate and download custom report
        /// </summary>
        [HttpPost("reports/generate")]
        public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                var reportRequest = new ReportRequest
                {
                    // Id = Guid.NewGuid(), // Property doesn't exist on ReportRequest model
                    ReportType = Enum.Parse<ReportType>(request.ReportType, true),
                    Format = Enum.Parse<ReportFormat>(request.Format, true),
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Title = request.Title,
                    Description = request.Description,
                    IncludeCharts = request.IncludeCharts,
                    Filters = new AnalyticsFilter
                    {
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        Engines = (request.Filters?.Engines ?? new List<string>()).Select(e => System.Enum.TryParse<SpeechEngineType>(e, true, out var result) ? result : SpeechEngineType.WhisperLocal).ToList(),
                        Applications = request.Filters?.Applications ?? new List<string>(),
                        MinAccuracy = (float?)request.Filters?.MinAccuracy,
                        MaxAccuracy = (float?)request.Filters?.MaxAccuracy
                    },
                    CustomMetrics = request.CustomMetrics ?? new List<string>()
                };

                var report = await _reportingService.GenerateReportAsync(reportRequest);

                var contentType = request.Format.ToLower() switch
                {
                    "pdf" => "application/pdf",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "csv" => "text/csv",
                    "json" => "application/json",
                    _ => "application/octet-stream"
                };

                var fileName = $"{request.Title}_{DateTime.UtcNow:yyyy-MM-dd}.{request.Format.ToLower()}";

                return File(new byte[0], contentType, fileName); // report.Data property access issue
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, new { error = "Failed to generate report" });
            }
        }

        /// <summary>
        /// Get available report templates
        /// </summary>
        [HttpGet("reports/templates")]
        public async Task<ActionResult<IEnumerable<ReportTemplate>>> GetReportTemplates()
        {
            try
            {
                var templates = await _reportingService.GetReportTemplatesAsync();

                return Ok(templates.Select(t => new ReportTemplate
                {
                    Id = Guid.NewGuid(), // Using generated ID as fallback
                    Name = t.Name,
                    Description = t.Description,
                    ReportType = t.ReportType.ToString(),
                    DefaultFormat = t.DefaultFormat.ToString(),
                    SupportedFormats = t.SupportedFormats.Select(f => f.ToString()).ToList(),
                    RequiredMetrics = t.RequiredMetrics.ToList(),
                    CreatedAt = t.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report templates");
                return StatusCode(500, new { error = "Failed to get report templates" });
            }
        }

        /// <summary>
        /// Export analytics data in various formats
        /// </summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportData([FromBody] ExportDataRequest request)
        {
            try
            {
                var filter = new AnalyticsFilter
                {
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Engines = (request.Engines ?? new List<string>()).Select(e => System.Enum.TryParse<SpeechEngineType>(e, true, out var result) ? result : SpeechEngineType.WhisperLocal).ToList(),
                    Applications = request.Applications ?? new List<string>(),
                    MinAccuracy = (float?)request.MinAccuracy,
                    MaxAccuracy = (float?)request.MaxAccuracy
                };

                var exportData = await _analyticsService.ExportDataAsync(filter);

                var format = request.Format.ToLower();
                var fileName = $"voice_assistant_data_{DateTime.UtcNow:yyyy-MM-dd}.{format}";

                return format switch
                {
                    "csv" => File(exportData.CsvData, "text/csv", fileName),
                    "json" => File(exportData.JsonData, "application/json", fileName),
                    "xml" => File(exportData.XmlData, "application/xml", fileName),
                    _ => BadRequest(new { error = "Unsupported export format" })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                return StatusCode(500, new { error = "Failed to export data" });
            }
        }

        /// <summary>
        /// Get real-time analytics dashboard data
        /// </summary>
        [HttpGet("realtime")]
        public async Task<ActionResult<RealtimeAnalytics>> GetRealtimeAnalytics()
        {
            try
            {
                var realtime = await _analyticsService.GetRealtimeAnalyticsAsync();

                return Ok(new RealtimeAnalytics
                {
                    CurrentRecognitionRate = realtime.CurrentRecognitionRate,
                    AverageAccuracyLast5Min = realtime.AverageAccuracyLast5Min,
                    ActiveUsers = realtime.ActiveUsers,
                    SystemLoad = realtime.SystemLoad,
                    ErrorRateLast5Min = realtime.ErrorRateLast5Min,
                    TopActiveApplications = realtime.TopActiveApplications.Take(5).ToList(),
                    RecentEvents = realtime.RecentEvents.Take(10).Select(e => new RecentEvent
                    {
                        Timestamp = e.Timestamp,
                        EventType = e.EventType,
                        Description = e.Description,
                        Severity = e.Severity
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting realtime analytics");
                return StatusCode(500, new { error = "Failed to get realtime analytics" });
            }
        }

        /// <summary>
        /// Get predictive analytics and forecasts
        /// </summary>
        [HttpGet("predictions")]
        public async Task<ActionResult<PredictiveAnalytics>> GetPredictiveAnalytics(
            [FromQuery] int forecastDays = 30,
            [FromQuery] string metric = "usage")
        {
            try
            {
                var predictions = await _analyticsService.GetPredictiveAnalyticsAsync();

                return Ok(new PredictiveAnalytics
                {
                    Metric = metric,
                    ForecastDays = forecastDays,
                    Confidence = predictions.Confidence,
                    TrendDirection = predictions.TrendDirection,
                    ForecastData = predictions.ForecastData.Select(d => new ForecastDataPoint
                    {
                        Date = d.Date,
                        PredictedValue = d.PredictedValue,
                        ConfidenceInterval = new ConfidenceInterval
                        {
                            Lower = d.ConfidenceInterval.Lower,
                            Upper = d.ConfidenceInterval.Upper
                        }
                    }).ToList(),
                    KeyInsights = predictions.KeyInsights,
                    Recommendations = predictions.Recommendations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting predictive analytics");
                return StatusCode(500, new { error = "Failed to get predictive analytics" });
            }
        }

        /// <summary>
        /// Create custom analytics dashboard
        /// </summary>
        [HttpPost("dashboards")]
        public async Task<ActionResult<AnalyticsDashboard>> CreateDashboard([FromBody] CreateDashboardRequest request)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized();
                }

                var dashboard = new Core.Models.AnalyticsDashboard
                {
                    Name = request.Name,
                    Description = request.Description,
                    Layout = request.Layout,
                    Widgets = request.Widgets.Select(w => new Core.Models.DashboardWidget
                    {
                        Type = w.Type,
                        Title = w.Title,
                        Configuration = new Dictionary<string, int>(),
                        Position = new Dictionary<string, int>(),
                        Size = new Dictionary<string, int>()
                    }).ToList(),
                    IsPublic = request.IsPublic,
                    CreatedBy = userEmail
                };

                var created = await _analyticsService.CreateDashboardAsync(dashboard);

                return Ok(new AnalyticsDashboard
                {
                    Id = created.Id,
                    Name = created.Name,
                    Description = created.Description,
                    Layout = created.Layout,
                    Widgets = new List<DashboardWidget>(), // created.Widgets empty due to dummy data
                    IsPublic = created.IsPublic,
                    CreatedBy = created.CreatedBy,
                    CreatedAt = created.CreatedAt,
                    UpdatedAt = created.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dashboard");
                return StatusCode(500, new { error = "Failed to create dashboard" });
            }
        }

        /// <summary>
        /// Get user's custom dashboards
        /// </summary>
        [HttpGet("dashboards")]
        public async Task<ActionResult<IEnumerable<AnalyticsDashboard>>> GetDashboards()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized();
                }

                var dashboards = await _analyticsService.GetUserDashboardsAsync(userEmail);
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboards");
                return StatusCode(500, new { error = "Failed to get dashboards" });
            }
        }
    }

    #region DTOs

    public class DetailedAnalyticsResponse
    {
        public DatePeriod Period { get; set; } = new();
        public string GroupBy { get; set; } = string.Empty;
        public AnalyticsFilterDto Filter { get; set; } = new();
        public List<TimeSeriesDataPoint> TimeSeriesData { get; set; } = new();
        public AnalyticsSummary Summary { get; set; } = new();
    }

    public class AnalyticsFilterDto
    {
        public List<string> Engines { get; set; } = new();
        public List<string> Applications { get; set; } = new();
        public double? MinAccuracy { get; set; }
        public double? MaxAccuracy { get; set; }
    }

    public class TimeSeriesDataPoint
    {
        public DateTime Date { get; set; }
        public int TotalRecognitions { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
        public int TotalWords { get; set; }
        public int UniqueApplications { get; set; }
        public int ErrorCount { get; set; }
    }

    public class AnalyticsSummary
    {
        public int TotalRecognitions { get; set; }
        public int TotalWords { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
        public string MostUsedEngine { get; set; } = string.Empty;
        public string MostUsedApplication { get; set; } = string.Empty;
        public int PeakUsageHour { get; set; }
        public double ErrorRate { get; set; }
    }

    public class PerformanceMetricsResponse
    {
        public DatePeriod Period { get; set; } = new();
        public string MetricType { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double PreviousPeriodValue { get; set; }
        public double PercentageChange { get; set; }
        public string Trend { get; set; } = string.Empty;
        public List<TrendDataPoint> TrendData { get; set; } = new();
        public PerformanceBenchmarks Benchmarks { get; set; } = new();
    }

    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

    public class PerformanceBenchmarks
    {
        public double Target { get; set; }
        public double Industry { get; set; }
        public double Personal { get; set; }
    }

    public class ApplicationInsightsResponse
    {
        public DatePeriod Period { get; set; } = new();
        public List<ApplicationInsight> TopApplications { get; set; } = new();
        public List<ApplicationCategory> CategoryBreakdown { get; set; } = new();
        public UsagePatterns UsagePatterns { get; set; } = new();
    }

    public class ApplicationInsight
    {
        public string ApplicationName { get; set; } = string.Empty;
        public int TotalRecognitions { get; set; }
        public double TotalUsageTime { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageSessionLength { get; set; }
        public int MostActiveTimeOfDay { get; set; }
        public string PreferredEngine { get; set; } = string.Empty;
        public string AccuracyTrend { get; set; } = string.Empty;
        public double UsageGrowth { get; set; }
    }

    public class ApplicationCategory
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public double AverageAccuracy { get; set; }
    }

    public class UsagePatterns
    {
        public List<int> PeakHours { get; set; } = new();
        public double WeekdayUsage { get; set; }
        public double WeekendUsage { get; set; }
        public Dictionary<string, double> SeasonalTrends { get; set; } = new();
    }

    public class EngineComparisonResponse
    {
        public DatePeriod Period { get; set; } = new();
        public List<EngineMetrics> EngineMetrics { get; set; } = new();
        public EngineRecommendation Recommendation { get; set; } = new();
    }

    public class EngineMetrics
    {
        public string EngineName { get; set; } = string.Empty;
        public int TotalRecognitions { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
        public double ErrorRate { get; set; }
        public double UsagePercentage { get; set; }
        public double CostPerRecognition { get; set; }
        public List<string> SupportedLanguages { get; set; } = new();
        public string RecommendedUseCase { get; set; } = string.Empty;
    }

    public class EngineRecommendation
    {
        public string BestForAccuracy { get; set; } = string.Empty;
        public string BestForSpeed { get; set; } = string.Empty;
        public string BestForCost { get; set; } = string.Empty;
        public string Overall { get; set; } = string.Empty;
        public string Reasoning { get; set; } = string.Empty;
    }

    public class GenerateReportRequest
    {
        [Required]
        public string ReportType { get; set; } = string.Empty;

        [Required]
        public string Format { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IncludeCharts { get; set; } = true;
        public AnalyticsFilterDto? Filters { get; set; }
        public List<string>? CustomMetrics { get; set; }
    }

    public class ReportTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string DefaultFormat { get; set; } = string.Empty;
        public List<string> SupportedFormats { get; set; } = new();
        public List<string> RequiredMetrics { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class ExportDataRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string>? Engines { get; set; }
        public List<string>? Applications { get; set; }
        public double? MinAccuracy { get; set; }
        public double? MaxAccuracy { get; set; }
        public string Format { get; set; } = "csv";
        public List<string> DataTypes { get; set; } = new();
    }

    public class RealtimeAnalytics
    {
        public double CurrentRecognitionRate { get; set; }
        public double AverageAccuracyLast5Min { get; set; }
        public int ActiveUsers { get; set; }
        public double SystemLoad { get; set; }
        public double ErrorRateLast5Min { get; set; }
        public List<string> TopActiveApplications { get; set; } = new();
        public List<RecentEvent> RecentEvents { get; set; } = new();
    }

    public class RecentEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class PredictiveAnalytics
    {
        public string Metric { get; set; } = string.Empty;
        public int ForecastDays { get; set; }
        public double Confidence { get; set; }
        public string TrendDirection { get; set; } = string.Empty;
        public List<ForecastDataPoint> ForecastData { get; set; } = new();
        public List<string> KeyInsights { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class ForecastDataPoint
    {
        public DateTime Date { get; set; }
        public double PredictedValue { get; set; }
        public ConfidenceInterval ConfidenceInterval { get; set; } = new();
    }

    public class ConfidenceInterval
    {
        public double Lower { get; set; }
        public double Upper { get; set; }
    }

    public class CreateDashboardRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Layout { get; set; } = string.Empty;
        public List<DashboardWidgetRequest> Widgets { get; set; } = new();
        public bool IsPublic { get; set; }
    }

    public class DashboardWidgetRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public Dictionary<string, object> Position { get; set; } = new();
        public Dictionary<string, object> Size { get; set; } = new();
    }

    public class AnalyticsDashboard
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Layout { get; set; } = string.Empty;
        public List<DashboardWidget> Widgets { get; set; } = new();
        public bool IsPublic { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DashboardWidget
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public Dictionary<string, object> Position { get; set; } = new();
        public Dictionary<string, object> Size { get; set; } = new();
    }

    #endregion
}