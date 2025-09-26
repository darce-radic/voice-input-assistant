using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for generating reports and analytics
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generates a usage report for the specified period
    /// </summary>
    Task<byte[]> GenerateUsageReportAsync(DateTime startDate, DateTime endDate, string format = "pdf");
    
    /// <summary>
    /// Gets available report templates
    /// </summary>
    Task<IEnumerable<ReportTemplate>> GetReportTemplatesAsync();
    
    /// <summary>
    /// Generates a custom report based on template
    /// </summary>
    Task<byte[]> GenerateCustomReportAsync(string templateId, Dictionary<string, object> parameters);
    
    /// <summary>
    /// Generates a report based on the request
    /// </summary>
    Task<object> GenerateReportAsync(ReportRequest request);
}

/// <summary>
/// Report template information
/// </summary>
public record ReportTemplate(
    string Id,
    string Name,
    string Description,
    string[] SupportedFormats,
    ReportType ReportType,
    ReportFormat DefaultFormat,
    string[] RequiredMetrics,
    DateTime CreatedAt
);
