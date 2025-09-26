using System;
using System.Collections.Generic;

namespace VoiceInputAssistant.Models
{
    /// <summary>
    /// Represents error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Low severity - minor issues that don't affect functionality
        /// </summary>
        Low = 0,

        /// <summary>
        /// Medium severity - issues that may affect some functionality
        /// </summary>
        Medium = 1,

        /// <summary>
        /// High severity - significant issues that affect major functionality
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical severity - application-breaking issues
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Represents error types for UI presentation
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Informational message
        /// </summary>
        Information = 0,

        /// <summary>
        /// Warning message
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error message
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical error message
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// Contains comprehensive information about an error
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>
        /// Gets or sets the exception that caused the error
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the operation that was being performed when the error occurred
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the error
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets whether the error is recoverable
        /// </summary>
        public bool IsRecoverable { get; set; }

        /// <summary>
        /// Gets or sets the user agent string for the application
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets system information relevant to the error
        /// </summary>
        public Dictionary<string, string> SystemInfo { get; set; } = new();

        /// <summary>
        /// Gets or sets additional context information about the error
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// Gets or sets the correlation ID for tracking related errors
        /// </summary>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets whether the error has been reported to analytics
        /// </summary>
        public bool IsReported { get; set; }

        /// <summary>
        /// Gets or sets any recovery actions that were attempted
        /// </summary>
        public List<string> AttemptedRecoveryActions { get; set; } = new();
    }

    /// <summary>
    /// Represents an error recovery suggestion
    /// </summary>
    public class RecoverySuggestion
    {
        /// <summary>
        /// Gets or sets a description of the suggested action
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the action to perform for this recovery suggestion
        /// </summary>
        public Action? Action { get; set; }

        /// <summary>
        /// Gets or sets whether this action requires elevated privileges
        /// </summary>
        public bool RequiresElevation { get; set; }

        /// <summary>
        /// Gets or sets the priority of this suggestion (lower numbers = higher priority)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets additional details about the suggestion
        /// </summary>
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of an error recovery attempt
    /// </summary>
    public class RecoveryResult
    {
        /// <summary>
        /// Gets or sets whether the recovery was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets a message describing the result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets any exception that occurred during recovery
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets whether another recovery attempt should be made
        /// </summary>
        public bool ShouldRetry { get; set; }

        /// <summary>
        /// Gets or sets the delay before retrying (if ShouldRetry is true)
        /// </summary>
        public TimeSpan? RetryDelay { get; set; }
    }

    /// <summary>
    /// Base class for application-specific exceptions
    /// </summary>
    public class VoiceInputAssistantException : Exception
    {
        /// <summary>
        /// Gets the error severity
        /// </summary>
        public ErrorSeverity Severity { get; }

        /// <summary>
        /// Gets whether the error is recoverable
        /// </summary>
        public bool IsRecoverable { get; }

        /// <summary>
        /// Gets the operation that caused the error
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Initializes a new instance of VoiceInputAssistantException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="operation">Operation that caused the error</param>
        /// <param name="severity">Error severity</param>
        /// <param name="isRecoverable">Whether the error is recoverable</param>
        public VoiceInputAssistantException(
            string message, 
            string operation, 
            ErrorSeverity severity = ErrorSeverity.Medium, 
            bool isRecoverable = true) 
            : base(message)
        {
            Operation = operation;
            Severity = severity;
            IsRecoverable = isRecoverable;
        }

        /// <summary>
        /// Initializes a new instance of VoiceInputAssistantException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        /// <param name="operation">Operation that caused the error</param>
        /// <param name="severity">Error severity</param>
        /// <param name="isRecoverable">Whether the error is recoverable</param>
        public VoiceInputAssistantException(
            string message, 
            Exception innerException, 
            string operation, 
            ErrorSeverity severity = ErrorSeverity.Medium, 
            bool isRecoverable = true) 
            : base(message, innerException)
        {
            Operation = operation;
            Severity = severity;
            IsRecoverable = isRecoverable;
        }
    }

    /// <summary>
    /// Exception thrown when speech recognition fails
    /// </summary>
    public class SpeechRecognitionException : VoiceInputAssistantException
    {
        public SpeechRecognitionException(string message) 
            : base(message, "Speech Recognition", ErrorSeverity.Medium, true)
        {
        }

        public SpeechRecognitionException(string message, Exception innerException) 
            : base(message, innerException, "Speech Recognition", ErrorSeverity.Medium, true)
        {
        }
    }

    /// <summary>
    /// Exception thrown when audio device operations fail
    /// </summary>
    public class AudioDeviceException : VoiceInputAssistantException
    {
        public AudioDeviceException(string message) 
            : base(message, "Audio Device", ErrorSeverity.High, true)
        {
        }

        public AudioDeviceException(string message, Exception innerException) 
            : base(message, innerException, "Audio Device", ErrorSeverity.High, true)
        {
        }
    }

    /// <summary>
    /// Exception thrown when settings operations fail
    /// </summary>
    public class SettingsException : VoiceInputAssistantException
    {
        public SettingsException(string message) 
            : base(message, "Settings", ErrorSeverity.Low, true)
        {
        }

        public SettingsException(string message, Exception innerException) 
            : base(message, innerException, "Settings", ErrorSeverity.Low, true)
        {
        }
    }

    /// <summary>
    /// Exception thrown when profile operations fail
    /// </summary>
    public class ProfileException : VoiceInputAssistantException
    {
        public ProfileException(string message) 
            : base(message, "Profile Management", ErrorSeverity.Medium, true)
        {
        }

        public ProfileException(string message, Exception innerException) 
            : base(message, innerException, "Profile Management", ErrorSeverity.Medium, true)
        {
        }
    }

    /// <summary>
    /// Exception thrown when hotkey operations fail
    /// </summary>
    public class HotkeyException : VoiceInputAssistantException
    {
        public HotkeyException(string message) 
            : base(message, "Hotkey Management", ErrorSeverity.Medium, true)
        {
        }

        public HotkeyException(string message, Exception innerException) 
            : base(message, innerException, "Hotkey Management", ErrorSeverity.Medium, true)
        {
        }
    }
}