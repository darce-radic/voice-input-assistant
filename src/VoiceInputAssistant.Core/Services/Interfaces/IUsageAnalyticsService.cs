using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for collecting and analyzing usage analytics
    /// </summary>
    public interface IUsageAnalyticsService
    {
        /// <summary>
        /// Records a speech recognition event
        /// </summary>
        /// <param name="result">Recognition result to record</param>
        Task RecordRecognitionAsync(SpeechRecognitionResult result);

        /// <summary>
        /// Records an error event
        /// </summary>
        /// <param name="error">Error information to record</param>
        Task RecordErrorAsync(string errorType, string message, Exception exception = null);

        /// <summary>
        /// Records a user interaction event
        /// </summary>
        /// <param name="action">Action performed by user</param>
        /// <param name="context">Additional context information</param>
        Task RecordUserActionAsync(string action, Dictionary<string, object> context = null);

        /// <summary>
        /// Gets recent recognition results
        /// </summary>
        /// <param name="count">Number of recent results to retrieve</param>
        /// <returns>Collection of recent recognition results</returns>
        Task<IEnumerable<SpeechRecognitionResult>> GetRecentRecognitionsAsync(int count = 10);

        /// <summary>
        /// Gets usage statistics for a specified time period
        /// </summary>
        /// <param name="startDate">Start date for statistics</param>
        /// <param name="endDate">End date for statistics</param>
        /// <returns>Usage statistics for the period</returns>
        Task<UsageStatistics> GetUsageStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets daily usage statistics
        /// </summary>
        /// <param name="date">Date to get statistics for (defaults to today)</param>
        /// <returns>Usage statistics for the specified day</returns>
        Task<UsageStatistics> GetDailyUsageAsync(DateTime? date = null);

        /// <summary>
        /// Gets weekly usage statistics
        /// </summary>
        /// <param name="weekStartDate">Start of the week (defaults to current week)</param>
        /// <returns>Usage statistics for the specified week</returns>
        Task<UsageStatistics> GetWeeklyUsageAsync(DateTime? weekStartDate = null);

        /// <summary>
        /// Gets monthly usage statistics
        /// </summary>
        /// <param name="year">Year to get statistics for</param>
        /// <param name="month">Month to get statistics for</param>
        /// <returns>Usage statistics for the specified month</returns>
        Task<UsageStatistics> GetMonthlyUsageAsync(int year, int month);

        /// <summary>
        /// Gets the most frequently used applications
        /// </summary>
        /// <param name="count">Number of applications to return</param>
        /// <param name="days">Number of days to look back (defaults to 30)</param>
        /// <returns>List of applications with usage frequency</returns>
        Task<IEnumerable<ApplicationUsage>> GetTopApplicationsAsync(int count = 10, int days = 30);

        /// <summary>
        /// Gets recognition accuracy statistics by engine
        /// </summary>
        /// <param name="days">Number of days to look back</param>
        /// <returns>Accuracy statistics by engine</returns>
        Task<IEnumerable<EngineAccuracyStatistics>> GetAccuracyStatisticsByEngineAsync(int days = 30);

        /// <summary>
        /// Gets error frequency statistics
        /// </summary>
        /// <param name="days">Number of days to look back</param>
        /// <returns>Error statistics</returns>
        Task<IEnumerable<ErrorStatistics>> GetErrorStatisticsAsync(int days = 30);

        /// <summary>
        /// Exports usage data for a specified time period
        /// </summary>
        /// <param name="startDate">Start date for export</param>
        /// <param name="endDate">End date for export</param>
        /// <param name="filePath">Path to export the data to</param>
        /// <param name="format">Export format (JSON, CSV, etc.)</param>
        Task ExportUsageDataAsync(DateTime startDate, DateTime endDate, string filePath, VoiceInputAssistant.Core.Models.ExportFormat format = VoiceInputAssistant.Core.Models.ExportFormat.Json);

        /// <summary>
        /// Clears usage data older than the specified number of days
        /// </summary>
        /// <param name="olderThanDays">Number of days to keep data for</param>
        /// <returns>Number of records deleted</returns>
        Task<int> CleanupOldDataAsync(int olderThanDays = 90);

        /// <summary>
        /// Gets whether analytics collection is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Enables or disables analytics collection
        /// </summary>
        /// <param name="enabled">Whether to enable analytics collection</param>
        Task SetEnabledAsync(bool enabled);

        /// <summary>
        /// Gets detailed analytics with filters
        /// </summary>
        /// <param name="filter">Filter criteria for analytics</param>
        Task<object> GetDetailedAnalyticsAsync(object filter);

        /// <summary>
        /// Gets performance metrics for the system
        /// </summary>
        Task<object> GetPerformanceMetricsAsync();

        /// <summary>
        /// Gets performance metrics for the system with date range and type filter
        /// </summary>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <param name="metricType">Type of metrics to retrieve</param>
        Task<object> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate, string metricType);

        /// <summary>
        /// Gets application insights and usage patterns
        /// </summary>
        Task<object> GetApplicationInsightsAsync();

        /// <summary>
        /// Gets application insights with date range and insight type filter
        /// </summary>
        /// <param name="startDate">Start date for insights</param>
        /// <param name="endDate">End date for insights</param>
        /// <param name="insightType">Type of insights to retrieve</param>
        Task<object> GetApplicationInsightsAsync(DateTime startDate, DateTime endDate, string insightType);

        /// <summary>
        /// Gets engine comparison data
        /// </summary>
        Task<object> GetEngineComparisonAsync();

        /// <summary>
        /// Gets engine comparison data for a specific date range
        /// </summary>
        /// <param name="startDate">Start date for comparison</param>
        /// <param name="endDate">End date for comparison</param>
        Task<object> GetEngineComparisonAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Exports analytics data with filters
        /// </summary>
        /// <param name="filter">Filter criteria for export</param>
        Task<object> ExportDataAsync(object filter);

        /// <summary>
        /// Gets real-time analytics data
        /// </summary>
        Task<object> GetRealtimeAnalyticsAsync();

        /// <summary>
        /// Gets predictive analytics
        /// </summary>
        Task<object> GetPredictiveAnalyticsAsync();

        /// <summary>
        /// Creates a new analytics dashboard
        /// </summary>
        /// <param name="dashboard">Dashboard configuration</param>
        Task<object> CreateDashboardAsync(object dashboard);

        /// <summary>
        /// Gets user dashboards
        /// </summary>
        /// <param name="userId">User ID</param>
        Task<object> GetUserDashboardsAsync(string userId);
    }
}
