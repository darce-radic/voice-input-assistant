using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Models;
using VoiceInputAssistant.Services.Interfaces;

namespace VoiceInputAssistant.Extensions
{
    /// <summary>
    /// Extension methods to simplify error handling throughout the application
    /// </summary>
    public static class ErrorHandlingExtensions
    {
        /// <summary>
        /// Safely executes an action with comprehensive error handling
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="action">The action to execute</param>
        /// <param name="operation">Description of the operation being performed</param>
        /// <param name="showErrorDialog">Whether to show error dialog to user</param>
        /// <returns>True if the operation completed successfully</returns>
        public static async Task<bool> SafeExecuteAsync(this ILogger logger, Func<Task> action, string operation, bool showErrorDialog = true)
        {
            try
            {
                await action().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(logger, ex, operation, showErrorDialog).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Safely executes a function with comprehensive error handling
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="logger">The logger instance</param>
        /// <param name="func">The function to execute</param>
        /// <param name="operation">Description of the operation being performed</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <param name="showErrorDialog">Whether to show error dialog to user</param>
        /// <returns>The result of the function or the default value on error</returns>
        public static async Task<T> SafeExecuteAsync<T>(this ILogger logger, Func<Task<T>> func, string operation, T defaultValue = default!, bool showErrorDialog = true)
        {
            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(logger, ex, operation, showErrorDialog).ConfigureAwait(false);
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely executes a synchronous action with comprehensive error handling
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="action">The action to execute</param>
        /// <param name="operation">Description of the operation being performed</param>
        /// <param name="showErrorDialog">Whether to show error dialog to user</param>
        /// <returns>True if the operation completed successfully</returns>
        public static async Task<bool> SafeExecute(this ILogger logger, Action action, string operation, bool showErrorDialog = true)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(logger, ex, operation, showErrorDialog).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Safely executes a synchronous function with comprehensive error handling
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="logger">The logger instance</param>
        /// <param name="func">The function to execute</param>
        /// <param name="operation">Description of the operation being performed</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <param name="showErrorDialog">Whether to show error dialog to user</param>
        /// <returns>The result of the function or the default value on error</returns>
        public static async Task<T> SafeExecute<T>(this ILogger logger, Func<T> func, string operation, T defaultValue = default!, bool showErrorDialog = true)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(logger, ex, operation, showErrorDialog).ConfigureAwait(false);
                return defaultValue;
            }
        }

        /// <summary>
        /// Helper method to handle exceptions using the error handling service
        /// </summary>
        private static async Task<bool> HandleExceptionAsync(ILogger logger, Exception ex, string operation, bool showErrorDialog)
        {
            try
            {
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider != null)
                {
                    var errorService = serviceProvider.GetService<IErrorHandlingService>();
                    if (errorService != null)
                    {
                        return await errorService.HandleExceptionAsync(ex, operation, showErrorDialog).ConfigureAwait(false);
                    }
                }

                // Fallback logging if error service is not available
                logger.LogError(ex, "Error in operation: {Operation}", operation);
                return !(ex is OutOfMemoryException || ex is StackOverflowException || ex is AccessViolationException);
            }
            catch (Exception handlingEx)
            {
                logger.LogCritical(handlingEx, "Critical failure in error handling for operation: {Operation}", operation);
                return false;
            }
        }

        /// <summary>
        /// Creates a custom application exception with proper context
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="operation">The operation that failed</param>
        /// <param name="severity">The error severity</param>
        /// <param name="isRecoverable">Whether the error is recoverable</param>
        /// <param name="innerException">The inner exception</param>
        /// <returns>A new VoiceInputAssistantException</returns>
        public static VoiceInputAssistantException CreateApplicationException(string message, string operation, ErrorSeverity severity = ErrorSeverity.Medium, bool isRecoverable = true, Exception? innerException = null)
        {
            if (innerException != null)
            {
                return new VoiceInputAssistantException(message, innerException, operation, severity, isRecoverable);
            }
            return new VoiceInputAssistantException(message, operation, severity, isRecoverable);
        }

        /// <summary>
        /// Creates a speech recognition exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        /// <returns>A new SpeechRecognitionException</returns>
        public static SpeechRecognitionException CreateSpeechException(string message, Exception? innerException = null)
        {
            if (innerException != null)
            {
                return new SpeechRecognitionException(message, innerException);
            }
            return new SpeechRecognitionException(message);
        }

        /// <summary>
        /// Creates an audio device exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        /// <returns>A new AudioDeviceException</returns>
        public static AudioDeviceException CreateAudioException(string message, Exception? innerException = null)
        {
            if (innerException != null)
            {
                return new AudioDeviceException(message, innerException);
            }
            return new AudioDeviceException(message);
        }

        /// <summary>
        /// Creates a settings exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        /// <returns>A new SettingsException</returns>
        public static SettingsException CreateSettingsException(string message, Exception? innerException = null)
        {
            if (innerException != null)
            {
                return new SettingsException(message, innerException);
            }
            return new SettingsException(message);
        }

        /// <summary>
        /// Creates a hotkey exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        /// <returns>A new HotkeyException</returns>
        public static HotkeyException CreateHotkeyException(string message, Exception? innerException = null)
        {
            if (innerException != null)
            {
                return new HotkeyException(message, innerException);
            }
            return new HotkeyException(message);
        }
    }
}