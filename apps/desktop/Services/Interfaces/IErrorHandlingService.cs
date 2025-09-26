using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services.Interfaces
{
    /// <summary>
    /// Provides application-wide error handling functionality
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Handles an exception with default settings
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="operation">The operation that was being performed</param>
        /// <param name="showErrorDialog">Whether to show an error dialog</param>
        /// <returns>True if the application should continue execution, false if it should shut down</returns>
        Task<bool> HandleExceptionAsync(Exception exception, string operation, bool showErrorDialog = true);

        /// <summary>
        /// Handles an error with custom error information
        /// </summary>
        /// <param name="errorInfo">Detailed error information</param>
        /// <param name="showErrorDialog">Whether to show an error dialog</param>
        /// <returns>True if the application should continue execution, false if it should shut down</returns>
        Task<bool> HandleErrorAsync(ErrorInfo errorInfo, bool showErrorDialog = true);
        
        /// <summary>
        /// Gets recovery suggestions for the specified exception
        /// </summary>
        /// <param name="exception">The exception to get recovery suggestions for</param>
        /// <param name="operation">The operation that was being performed</param>
        /// <returns>A list of recovery suggestions</returns>
        List<RecoverySuggestion> GetRecoverySuggestions(Exception exception, string operation);
        
        /// <summary>
        /// Logs an error to the application log
        /// </summary>
        /// <param name="errorInfo">The error information to log</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task LogErrorAsync(ErrorInfo errorInfo);
        
        /// <summary>
        /// Reports an error to analytics services
        /// </summary>
        /// <param name="errorInfo">The error information to report</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task ReportErrorAsync(ErrorInfo errorInfo);
        
        /// <summary>
        /// Determines the severity of an exception
        /// </summary>
        /// <param name="exception">The exception to determine severity for</param>
        /// <param name="operation">The operation that was being performed</param>
        /// <returns>The error severity</returns>
        ErrorSeverity DetermineErrorSeverity(Exception exception, string operation);
        
        /// <summary>
        /// Determines whether an error is recoverable
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <param name="operation">The operation that was being performed</param>
        /// <returns>True if the error is recoverable, false otherwise</returns>
        bool IsErrorRecoverable(Exception exception, string operation);
        
        /// <summary>
        /// Creates error information from an exception
        /// </summary>
        /// <param name="exception">The exception to create info for</param>
        /// <param name="operation">The operation that was being performed</param>
        /// <param name="customSeverity">Optional custom severity level</param>
        /// <param name="isRecoverable">Optional custom recoverable flag</param>
        /// <returns>The created error information</returns>
        ErrorInfo CreateErrorInfo(Exception exception, string operation, ErrorSeverity? customSeverity = null, bool? isRecoverable = null);
        
        /// <summary>
        /// Shows an error dialog to the user
        /// </summary>
        /// <param name="errorInfo">The error information to display</param>
        /// <returns>True if the application should restart</returns>
        Task<bool> ShowErrorDialogAsync(ErrorInfo errorInfo);
        
        /// <summary>
        /// Saves error details to a file for diagnostics
        /// </summary>
        /// <param name="errorInfo">The error information to save</param>
        /// <returns>The path to the saved error file, or null if saving failed</returns>
        Task<string?> SaveErrorToFileAsync(ErrorInfo errorInfo);
        
        /// <summary>
        /// Gets or sets whether error reporting is enabled
        /// </summary>
        bool IsErrorReportingEnabled { get; set; }
        
        /// <summary>
        /// Gets or sets whether detailed error information is shown to users
        /// </summary>
        bool ShowDetailedErrorsToUsers { get; set; }
    }
}