using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Views
{
    /// <summary>
    /// Interaction logic for ErrorDialog.xaml
    /// </summary>
    public partial class ErrorDialog : Window
    {
        private ErrorInfo? _errorInfo;
        private readonly List<RecoverySuggestion> _recoverySuggestions;

        /// <summary>
        /// Gets whether the application should restart after this dialog closes
        /// </summary>
        public bool ShouldRestartApplication { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ErrorDialog class
        /// </summary>
        public ErrorDialog()
        {
            InitializeComponent();
            _recoverySuggestions = new List<RecoverySuggestion>();
            ShouldRestartApplication = false;
        }

        /// <summary>
        /// Displays error information in the dialog
        /// </summary>
        /// <param name="errorInfo">The error information to display</param>
        /// <param name="recoverySuggestions">Optional recovery suggestions</param>
        public void DisplayError(ErrorInfo errorInfo, List<RecoverySuggestion>? recoverySuggestions = null)
        {
            _errorInfo = errorInfo;
            
            if (recoverySuggestions != null)
            {
                _recoverySuggestions.Clear();
                _recoverySuggestions.AddRange(recoverySuggestions.OrderBy(r => r.Priority));
            }

            UpdateErrorDisplay();
        }

        /// <summary>
        /// Creates and shows an error dialog for the specified error
        /// </summary>
        /// <param name="owner">The parent window</param>
        /// <param name="errorInfo">The error information</param>
        /// <param name="recoverySuggestions">Optional recovery suggestions</param>
        /// <returns>True if the application should restart</returns>
        public static bool ShowError(Window? owner, ErrorInfo errorInfo, List<RecoverySuggestion>? recoverySuggestions = null)
        {
            var dialog = new ErrorDialog
            {
                Owner = owner
            };
            
            dialog.DisplayError(errorInfo, recoverySuggestions);
            dialog.ShowDialog();
            
            return dialog.ShouldRestartApplication;
        }

        /// <summary>
        /// Updates the error display with current error information
        /// </summary>
        private void UpdateErrorDisplay()
        {
            if (_errorInfo == null) return;

            // Set window title based on severity
            Title = _errorInfo.Severity switch
            {
                ErrorSeverity.Critical => "Critical Error",
                ErrorSeverity.High => "Error",
                ErrorSeverity.Medium => "Error",
                ErrorSeverity.Low => "Warning",
                _ => "Error"
            };

            // Update icon and colors based on severity
            UpdateIconAndColors(_errorInfo.Severity);

            // Update header text
            ErrorTitleText.Text = GetErrorTitle(_errorInfo.Severity);
            ErrorOperationText.Text = string.IsNullOrWhiteSpace(_errorInfo.Operation) 
                ? "Operation: Unknown" 
                : $"Operation: {_errorInfo.Operation}";

            // Update error message
            ErrorMessageText.Text = _errorInfo.Exception?.Message ?? "An unexpected error occurred.";

            // Update recovery suggestions
            if (_recoverySuggestions.Any())
            {
                RecoverySuggestionsCard.Visibility = Visibility.Visible;
                RecoverySuggestionsList.ItemsSource = _recoverySuggestions;
            }
            else
            {
                RecoverySuggestionsCard.Visibility = Visibility.Collapsed;
            }

            // Update technical details
            UpdateTechnicalDetails();

            // Show restart button for critical errors
            RestartApplicationButton.Visibility = _errorInfo.Severity == ErrorSeverity.Critical 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Updates the icon and colors based on error severity
        /// </summary>
        /// <param name="severity">The error severity</param>
        private void UpdateIconAndColors(ErrorSeverity severity)
        {
            var iconKind = severity switch
            {
                ErrorSeverity.Critical => PackIconKind.AlertOctagon,
                ErrorSeverity.High => PackIconKind.AlertCircle,
                ErrorSeverity.Medium => PackIconKind.Alert,
                ErrorSeverity.Low => PackIconKind.Information,
                _ => PackIconKind.AlertCircle
            };

            var iconBrush = severity switch
            {
                ErrorSeverity.Critical => new SolidColorBrush(Colors.DarkRed),
                ErrorSeverity.High => (Brush)FindResource("MaterialDesignValidationErrorBrush"),
                ErrorSeverity.Medium => new SolidColorBrush(Colors.Orange),
                ErrorSeverity.Low => new SolidColorBrush(Colors.RoyalBlue),
                _ => (Brush)FindResource("MaterialDesignValidationErrorBrush")
            };

            ErrorIcon.Kind = iconKind;
            ErrorIcon.Foreground = iconBrush;
        }

        /// <summary>
        /// Gets the appropriate error title for the severity level
        /// </summary>
        /// <param name="severity">The error severity</param>
        /// <returns>The error title</returns>
        private static string GetErrorTitle(ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Critical => "Critical Error - Application May Need to Restart",
                ErrorSeverity.High => "An Error Occurred",
                ErrorSeverity.Medium => "An Error Occurred",
                ErrorSeverity.Low => "Warning",
                _ => "An Error Occurred"
            };
        }

        /// <summary>
        /// Updates the technical details section
        /// </summary>
        private void UpdateTechnicalDetails()
        {
            if (_errorInfo?.Exception != null)
            {
                ExceptionTypeText.Text = _errorInfo.Exception.GetType().FullName ?? "Unknown";
                StackTraceText.Text = _errorInfo.Exception.StackTrace ?? "No stack trace available";
                CorrelationIdText.Text = _errorInfo.CorrelationId;

                ExceptionTypePanel.Visibility = Visibility.Visible;
                StackTracePanel.Visibility = Visibility.Visible;
            }
            else
            {
                ExceptionTypePanel.Visibility = Visibility.Collapsed;
                StackTracePanel.Visibility = Visibility.Collapsed;
                CorrelationIdText.Text = _errorInfo?.CorrelationId ?? Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Handles the OK button click event
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles the Copy Details button click event
        /// </summary>
        private void CopyDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var errorDetails = CreateErrorDetailsText();
                Clipboard.SetText(errorDetails);
                
                // Show brief feedback
                ShowCopyFeedback(CopyDetailsButton);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy error details: {ex.Message}", 
                    "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the Copy Correlation ID button click event
        /// </summary>
        private void CopyCorrelationIdButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(CorrelationIdText.Text))
                {
                    Clipboard.SetText(CorrelationIdText.Text);
                    ShowCopyFeedback(CopyCorrelationIdButton);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy correlation ID: {ex.Message}", 
                    "Copy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the Restart Application button click event
        /// </summary>
        private void RestartApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to restart the application? Any unsaved changes will be lost.",
                "Confirm Restart",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ShouldRestartApplication = true;
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Shows visual feedback when content is copied to clipboard
        /// </summary>
        /// <param name="button">The button that was clicked</param>
        private static void ShowCopyFeedback(System.Windows.Controls.Button button)
        {
            var originalContent = button.Content;
            button.Content = "Copied!";
            button.IsEnabled = false;

            // Reset after 1 second
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                button.Content = originalContent;
                button.IsEnabled = true;
            };
            
            timer.Start();
        }

        /// <summary>
        /// Creates a formatted text representation of the error details
        /// </summary>
        /// <returns>The formatted error details text</returns>
        private string CreateErrorDetailsText()
        {
            if (_errorInfo == null)
                return "No error information available.";

            var details = new System.Text.StringBuilder();
            details.AppendLine("=== ERROR DETAILS ===");
            details.AppendLine($"Timestamp: {_errorInfo.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            details.AppendLine($"Correlation ID: {_errorInfo.CorrelationId}");
            details.AppendLine($"Operation: {_errorInfo.Operation}");
            details.AppendLine($"Severity: {_errorInfo.Severity}");
            details.AppendLine($"Is Recoverable: {_errorInfo.IsRecoverable}");
            details.AppendLine();

            if (_errorInfo.Exception != null)
            {
                details.AppendLine("=== EXCEPTION INFORMATION ===");
                details.AppendLine($"Type: {_errorInfo.Exception.GetType().FullName}");
                details.AppendLine($"Message: {_errorInfo.Exception.Message}");
                details.AppendLine();

                if (!string.IsNullOrEmpty(_errorInfo.Exception.StackTrace))
                {
                    details.AppendLine("=== STACK TRACE ===");
                    details.AppendLine(_errorInfo.Exception.StackTrace);
                    details.AppendLine();
                }

                if (_errorInfo.Exception.InnerException != null)
                {
                    details.AppendLine("=== INNER EXCEPTION ===");
                    details.AppendLine($"Type: {_errorInfo.Exception.InnerException.GetType().FullName}");
                    details.AppendLine($"Message: {_errorInfo.Exception.InnerException.Message}");
                    details.AppendLine();
                }
            }

            if (_errorInfo.SystemInfo.Any())
            {
                details.AppendLine("=== SYSTEM INFORMATION ===");
                foreach (var kvp in _errorInfo.SystemInfo)
                {
                    details.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
                details.AppendLine();
            }

            if (_errorInfo.Context.Any())
            {
                details.AppendLine("=== ADDITIONAL CONTEXT ===");
                foreach (var kvp in _errorInfo.Context)
                {
                    try
                    {
                        var value = kvp.Value switch
                        {
                            string str => str,
                            _ => JsonSerializer.Serialize(kvp.Value, new JsonSerializerOptions 
                            { 
                                WriteIndented = true,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                            })
                        };
                        details.AppendLine($"{kvp.Key}: {value}");
                    }
                    catch
                    {
                        details.AppendLine($"{kvp.Key}: <serialization failed>");
                    }
                }
                details.AppendLine();
            }

            if (_recoverySuggestions.Any())
            {
                details.AppendLine("=== RECOVERY SUGGESTIONS ===");
                for (int i = 0; i < _recoverySuggestions.Count; i++)
                {
                    var suggestion = _recoverySuggestions[i];
                    details.AppendLine($"{i + 1}. {suggestion.Description}");
                    if (!string.IsNullOrEmpty(suggestion.Details))
                    {
                        details.AppendLine($"   Details: {suggestion.Details}");
                    }
                }
            }

            return details.ToString();
        }
    }
}