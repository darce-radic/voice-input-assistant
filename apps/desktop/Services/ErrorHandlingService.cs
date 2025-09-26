using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using VoiceInputAssistant.Models;
using VoiceInputAssistant.Services.Interfaces;

namespace VoiceInputAssistant.Services
{
    /// <summary>
    /// Provides centralized error handling functionality for the application
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly Dictionary<Type, ErrorSeverity> _severityMappings;
        private readonly Dictionary<Type, List<string>> _recoverySuggestionsMapping;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _severityMappings = new Dictionary<Type, ErrorSeverity>();
            _recoverySuggestionsMapping = new Dictionary<Type, List<string>>();
            InitializeMappings();
        }

        /// <summary>
        /// Initializes exception type mappings for severity and recovery suggestions
        /// </summary>
        private void InitializeMappings()
        {
            // Define severity mappings for different exception types
            _severityMappings[typeof(ArgumentNullException)] = ErrorSeverity.Medium;
            _severityMappings[typeof(ArgumentException)] = ErrorSeverity.Low;
            _severityMappings[typeof(InvalidOperationException)] = ErrorSeverity.Medium;
            _severityMappings[typeof(UnauthorizedAccessException)] = ErrorSeverity.High;
            _severityMappings[typeof(FileNotFoundException)] = ErrorSeverity.Medium;
            _severityMappings[typeof(DirectoryNotFoundException)] = ErrorSeverity.Medium;
            _severityMappings[typeof(IOException)] = ErrorSeverity.High;
            _severityMappings[typeof(OutOfMemoryException)] = ErrorSeverity.Critical;
            _severityMappings[typeof(StackOverflowException)] = ErrorSeverity.Critical;
            _severityMappings[typeof(AccessViolationException)] = ErrorSeverity.Critical;
            _severityMappings[typeof(NotImplementedException)] = ErrorSeverity.High;
            _severityMappings[typeof(SpeechRecognitionException)] = ErrorSeverity.Medium;
            _severityMappings[typeof(AudioDeviceException)] = ErrorSeverity.High;
            _severityMappings[typeof(SettingsException)] = ErrorSeverity.Low;
            _severityMappings[typeof(ProfileException)] = ErrorSeverity.Medium;
            _severityMappings[typeof(HotkeyException)] = ErrorSeverity.Medium;

            // Define recovery suggestions for different exception types
            _recoverySuggestionsMapping[typeof(FileNotFoundException)] = new List<string>
            {
                "Check if the file path is correct",
                "Ensure the file exists in the specified location",
                "Verify that you have read permissions for the file"
            };
            
            _recoverySuggestionsMapping[typeof(UnauthorizedAccessException)] = new List<string>
            {
                "Run the application as administrator",
                "Check file or directory permissions",
                "Ensure the resource is not being used by another process"
            };
            
            _recoverySuggestionsMapping[typeof(IOException)] = new List<string>
            {
                "Close any applications that might be using the file",
                "Check available disk space",
                "Verify network connectivity if accessing remote resources",
                "Try the operation again"
            };
            
            _recoverySuggestionsMapping[typeof(OutOfMemoryException)] = new List<string>
            {
                "Close unnecessary applications to free memory",
                "Restart the application",
                "Process smaller amounts of data at a time"
            };
            
            _recoverySuggestionsMapping[typeof(SpeechRecognitionException)] = new List<string>
            {
                "Check your microphone connection",
                "Verify microphone permissions are granted",
                "Try speaking more clearly or adjusting microphone settings",
                "Restart the speech recognition service"
            };
            
            _recoverySuggestionsMapping[typeof(AudioDeviceException)] = new List<string>
            {
                "Check audio device connections",
                "Update audio drivers",
                "Select a different audio device",
                "Restart the audio service"
            };
            
            _recoverySuggestionsMapping[typeof(SettingsException)] = new List<string>
            {
                "Reset settings to default values",
                "Check settings file permissions",
                "Manually edit the settings file if corrupted"
            };
            
            _recoverySuggestionsMapping[typeof(HotkeyException)] = new List<string>
            {
                "Try using different hotkey combinations",
                "Check for conflicting applications using the same hotkeys",
                "Restart the application to reset hotkey registrations"
            };
        }

        /// <summary>
        /// Handles an exception asynchronously with comprehensive error processing
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="operation">The operation being performed when the error occurred</param>
        /// <param name="showErrorDialog">Whether to show an error dialog to the user</param>
        /// <returns>True if the application should continue, false if it should shut down</returns>
        public async Task<bool> HandleExceptionAsync(Exception exception, string operation, bool showErrorDialog = true)
        {
            try
            {
                // Create error information
                var errorInfo = CreateErrorInfo(exception, operation);
                
                return await HandleErrorAsync(errorInfo, showErrorDialog).ConfigureAwait(false);
            }
            catch (Exception handlingException)
            {
                // If error handling itself fails, log the original exception and return false
                _logger.LogCritical(handlingException, "Critical failure in error handling while processing: {Operation}", operation);
                return false;
            }
        }

        /// <summary>
        /// Handles an error with custom error information
        /// </summary>
        /// <param name="errorInfo">Detailed error information</param>
        /// <param name="showErrorDialog">Whether to show an error dialog</param>
        /// <returns>True if the application should continue execution, false if it should shut down</returns>
        public async Task<bool> HandleErrorAsync(ErrorInfo errorInfo, bool showErrorDialog = true)
        {
            try
            {
                // Log the error
                await LogErrorAsync(errorInfo).ConfigureAwait(false);
                
                // Report to analytics (if enabled)
                if (IsErrorReportingEnabled)
                {
                    await ReportErrorAsync(errorInfo).ConfigureAwait(false);
                }
                
                // Save error to file for diagnostics
                await SaveErrorToFileAsync(errorInfo).ConfigureAwait(false);
                
                // Show error dialog if requested
                bool shouldRestart = false;
                if (showErrorDialog)
                {
                    shouldRestart = await ShowErrorDialogAsync(errorInfo).ConfigureAwait(false);
                }
                
                // Determine if application should continue
                if (shouldRestart || errorInfo.Severity == ErrorSeverity.Critical)
                {
                    return false; // Application should shut down/restart
                }
                
                return errorInfo.IsRecoverable;
            }
            catch (Exception handlingException)
            {
                // If error handling itself fails, log it and return false
                _logger.LogCritical(handlingException, "Critical failure in error handling while processing operation: {Operation}", errorInfo.Operation);
                return false;
            }
        }

        /// <summary>
        /// Gets recovery suggestions for a specific exception type
        /// </summary>
        /// <param name="exception">The exception to get suggestions for</param>
        /// <param name="operation">The operation that was being performed</param>
        /// <returns>A list of recovery suggestions</returns>
        public List<RecoverySuggestion> GetRecoverySuggestions(Exception? exception, string operation)
        {
            var suggestions = new List<RecoverySuggestion>();
            
            if (exception != null && _recoverySuggestionsMapping.TryGetValue(exception.GetType(), out var suggestionStrings))
            {
                for (int i = 0; i < suggestionStrings.Count; i++)
                {
                    suggestions.Add(new RecoverySuggestion
                    {
                        Description = suggestionStrings[i],
                        Priority = i,
                        Details = $"This suggestion is specific to {exception.GetType().Name} exceptions."
                    });
                }
            }
            
            // Add generic suggestions based on operation
            if (operation.Contains("Speech", StringComparison.OrdinalIgnoreCase) ||
                operation.Contains("Recognition", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new RecoverySuggestion
                {
                    Description = "Check microphone settings and permissions",
                    Priority = 90,
                    Details = "Speech-related operations require proper microphone access."
                });
            }
            
            if (operation.Contains("Audio", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(new RecoverySuggestion
                {
                    Description = "Verify audio device is connected and working",
                    Priority = 91,
                    Details = "Audio operations require a functioning audio device."
                });
            }
            
            // Add generic suggestions
            suggestions.Add(new RecoverySuggestion
            {
                Description = "Try the operation again",
                Priority = 100,
                Details = "Sometimes temporary issues resolve themselves on retry."
            });
            
            suggestions.Add(new RecoverySuggestion
            {
                Description = "Check application logs for more details",
                Priority = 101,
                Details = "Additional technical information may be available in the application logs."
            });
            
            return suggestions.OrderBy(s => s.Priority).ToList();
        }

        /// <summary>
        /// Logs error information asynchronously
        /// </summary>
        /// <param name="errorInfo">The error information to log</param>
        public async Task LogErrorAsync(ErrorInfo errorInfo)
        {
            await Task.Run(() =>
            {
                var logLevel = errorInfo.Severity switch
                {
                    ErrorSeverity.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
                    ErrorSeverity.High => Microsoft.Extensions.Logging.LogLevel.Error,
                    ErrorSeverity.Medium => Microsoft.Extensions.Logging.LogLevel.Warning,
                    ErrorSeverity.Low => Microsoft.Extensions.Logging.LogLevel.Information,
                    _ => Microsoft.Extensions.Logging.LogLevel.Error
                };
                
                _logger.Log(logLevel, errorInfo.Exception, 
                    "Error in operation '{Operation}' - Severity: {Severity}, Recoverable: {IsRecoverable}, CorrelationId: {CorrelationId}", 
                    errorInfo.Operation, errorInfo.Severity, errorInfo.IsRecoverable, errorInfo.CorrelationId);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports error to analytics services asynchronously
        /// </summary>
        /// <param name="errorInfo">The error information to report</param>
        public async Task ReportErrorAsync(ErrorInfo errorInfo)
        {
            await Task.Run(() =>
            {
                try
                {
                    // TODO: Implement actual analytics reporting
                    // For now, just simulate the reporting
                    _logger.LogDebug("Simulating error report to analytics for correlation ID: {CorrelationId}", errorInfo.CorrelationId);
                    
                    // In a real implementation, you would:
                    // 1. Send error data to Application Insights, Sentry, or similar service
                    // 2. Include relevant metrics and context
                    // 3. Handle rate limiting and privacy concerns
                    // 4. Ensure user consent for error reporting
                    
                    errorInfo.IsReported = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to report error to analytics");
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines the severity level of an exception
        /// </summary>
        /// <param name="exception">The exception to evaluate</param>
        /// <param name="operation">The operation being performed</param>
        /// <returns>The determined error severity</returns>
        public ErrorSeverity DetermineErrorSeverity(Exception? exception, string operation)
        {
            if (exception == null) return ErrorSeverity.Low;
            
            // Check if we have a specific mapping for this exception type
            if (_severityMappings.TryGetValue(exception.GetType(), out var severity))
            {
                return severity;
            }
            
            // Check for critical system exceptions
            if (exception is OutOfMemoryException || 
                exception is StackOverflowException || 
                exception is AccessViolationException)
            {
                return ErrorSeverity.Critical;
            }
            
            // Check for high-severity exceptions
            if (exception is UnauthorizedAccessException || 
                exception is SecurityException ||
                exception is IOException)
            {
                return ErrorSeverity.High;
            }
            
            // Default to medium for most other exceptions
            return ErrorSeverity.Medium;
        }

        /// <summary>
        /// Determines if an error is recoverable
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <param name="operation">The operation being performed</param>
        /// <returns>True if recoverable, false otherwise</returns>
        public bool IsErrorRecoverable(Exception? exception, string operation)
        {
            if (exception == null) return true;
            
            // Critical system exceptions are generally not recoverable
            if (exception is OutOfMemoryException || 
                exception is StackOverflowException || 
                exception is AccessViolationException)
            {
                return false;
            }
            
            // Most other exceptions are potentially recoverable
            return true;
        }

        /// <summary>
        /// Creates comprehensive error information from an exception
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <param name="operation">The operation being performed</param>
        /// <param name="customSeverity">Optional custom severity override</param>
        /// <param name="isRecoverable">Optional custom recoverable override</param>
        /// <returns>Complete error information</returns>
        public ErrorInfo CreateErrorInfo(Exception exception, string operation, ErrorSeverity? customSeverity = null, bool? isRecoverable = null)
        {
            var severity = customSeverity ?? DetermineErrorSeverity(exception, operation);
            var recoverable = isRecoverable ?? IsErrorRecoverable(exception, operation);
            
            var errorInfo = new ErrorInfo
            {
                Exception = exception,
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                Severity = severity,
                IsRecoverable = recoverable,
                UserAgent = $"VoiceInputAssistant/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}"
            };
            
            // Add system information
            errorInfo.SystemInfo["OS"] = Environment.OSVersion.ToString();
            errorInfo.SystemInfo["CLR"] = Environment.Version.ToString();
            errorInfo.SystemInfo["MachineName"] = Environment.MachineName;
            errorInfo.SystemInfo["UserDomainName"] = Environment.UserDomainName;
            errorInfo.SystemInfo["WorkingSet"] = Environment.WorkingSet.ToString();
            errorInfo.SystemInfo["AvailableMemory"] = GC.GetTotalMemory(false).ToString();
            errorInfo.SystemInfo["ProcessorCount"] = Environment.ProcessorCount.ToString();
            
            // Add context information
            errorInfo.Context["CurrentDirectory"] = Environment.CurrentDirectory;
            errorInfo.Context["CommandLine"] = Environment.CommandLine;
            errorInfo.Context["Is64BitProcess"] = Environment.Is64BitProcess;
            errorInfo.Context["Is64BitOS"] = Environment.Is64BitOperatingSystem;
            
            return errorInfo;
        }

        /// <summary>
        /// Shows an error dialog to the user asynchronously
        /// </summary>
        /// <param name="errorInfo">The error information to display</param>
        /// <returns>True if the application should restart</returns>
        public async Task<bool> ShowErrorDialogAsync(ErrorInfo errorInfo)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var mainWindow = Application.Current.MainWindow;
                    var recoverySuggestions = GetRecoverySuggestions(errorInfo.Exception, errorInfo.Operation);
                    
                    return Views.ErrorDialog.ShowError(mainWindow, errorInfo, recoverySuggestions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to show error dialog");
                    
                    // Fallback to simple message box
                    var message = errorInfo.Exception?.Message ?? "An unexpected error occurred.";
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Saves error information to a file for diagnostics
        /// </summary>
        /// <param name="errorInfo">The error information to save</param>
        /// <returns>The path to the saved file, or null if saving failed</returns>
        public async Task<string?> SaveErrorToFileAsync(ErrorInfo errorInfo)
        {
            try
            {
                var errorDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "VoiceInputAssistant", "ErrorLogs");
                
                Directory.CreateDirectory(errorDirectory);
                
                var fileName = $"Error_{errorInfo.Timestamp:yyyyMMdd_HHmmss}_{errorInfo.CorrelationId[..8]}.json";
                var filePath = Path.Combine(errorDirectory, fileName);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                // Create a serializable version of the error info
                var serializableErrorInfo = new
                {
                    errorInfo.Timestamp,
                    errorInfo.CorrelationId,
                    errorInfo.Operation,
                    errorInfo.Severity,
                    errorInfo.IsRecoverable,
                    errorInfo.IsReported,
                    errorInfo.UserAgent,
                    errorInfo.SystemInfo,
                    errorInfo.Context,
                    errorInfo.AttemptedRecoveryActions,
                    Exception = errorInfo.Exception != null ? new
                    {
                        Type = errorInfo.Exception.GetType().FullName,
                        errorInfo.Exception.Message,
                        errorInfo.Exception.StackTrace,
                        InnerException = errorInfo.Exception.InnerException?.Message
                    } : null
                };
                
                var json = JsonSerializer.Serialize(serializableErrorInfo, options);
                await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
                
                _logger.LogDebug("Error details saved to: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save error details to file");
                return null;
            }
        }

        /// <summary>
        /// Gets or sets whether error reporting to analytics is enabled
        /// </summary>
        public bool IsErrorReportingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether detailed error information should be shown to users
        /// </summary>
        public bool ShowDetailedErrorsToUsers { get; set; } = true;
    }
}