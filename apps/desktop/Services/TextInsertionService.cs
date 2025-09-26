using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services;

/// <summary>
/// Service for inserting recognized text into applications
/// </summary>
public class TextInsertionService : ITextInsertionService
{
    private readonly ILogger<TextInsertionService> _logger;

    public event EventHandler<TextInsertedEventArgs>? TextInserted;
    public event EventHandler<TextInsertionErrorEventArgs>? TextInsertionFailed;

    public TextInsertionService(ILogger<TextInsertionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> InsertTextAsync(string text, TextInsertionMethod method = TextInsertionMethod.Automatic)
    {
        try
        {
            _logger.LogDebug("Inserting text using method {Method}: {Text}", method, text);

            bool success = false;
            switch (method)
            {
                case TextInsertionMethod.SendKeys:
                    success = await InsertViaSendKeysAsync(text);
                    break;
                
                case TextInsertionMethod.ClipboardPaste:
                    success = await InsertViaClipboardAsync(text);
                    break;
                
                case TextInsertionMethod.Automatic:
                default:
                    // Try clipboard first, fallback to SendKeys
                    success = await InsertViaClipboardAsync(text);
                    if (!success)
                        success = await InsertViaSendKeysAsync(text);
                    break;
            }

            if (success)
            {
                OnTextInserted(text, method);
            }
            else
            {
                OnTextInsertionFailed(text, method, "Text insertion failed", null);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert text: {Text}", text);
            OnTextInsertionFailed(text, method, ex.Message, ex);
            return false;
        }
    }

    public async Task<TextInsertionTestResult> TestTextInsertionAsync(TextInsertionMethod method = TextInsertionMethod.Automatic)
    {
        return await TestInsertionMethodAsync(method);
    }

    public TextInsertionMethod GetRecommendedMethod()
    {
        // Simple logic - can be enhanced based on active window analysis
        return TextInsertionMethod.ClipboardPaste;
    }

    public async Task<TextInsertionTestResult> TestInsertionMethodAsync(TextInsertionMethod method)
    {
        try
        {
            _logger.LogDebug("Testing insertion method: {Method}", method);

            await Task.Delay(100); // Simulate test

            return new TextInsertionTestResult
            {
                IsSupported = true,
                IsReliable = true,
                ResponseTime = TimeSpan.FromMilliseconds(50)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test insertion method: {Method}", method);
            
            return new TextInsertionTestResult
            {
                IsSupported = false,
                IsReliable = false,
                ErrorMessage = ex.Message,
                ResponseTime = TimeSpan.Zero
            };
        }
    }

    public Task<ActiveWindowInfo> GetActiveWindowAsync()
    {
        try
        {
            // TODO: Implement actual active window detection
                return Task.FromResult(new ActiveWindowInfo
            {
                WindowTitle = "Unknown Application",
                ProcessName = "unknown",
                WindowHandle = IntPtr.Zero
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active window info");
            throw;
        }
    }

    public void Dispose()
    {
        // No resources to dispose currently
    }

    private async Task<bool> InsertViaSendKeysAsync(string text)
    {
        try
        {
            await Task.Run(() =>
            {
                // TODO: Implement SendKeys text insertion
                System.Windows.Forms.SendKeys.SendWait(text);
            });
            
            _logger.LogDebug("Text inserted via SendKeys successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert text via SendKeys");
            return false;
        }
    }

    private async Task<bool> InsertViaClipboardAsync(string text)
    {
        try
        {
            await Task.Run(() =>
            {
                // Save current clipboard content
                var currentClipboard = System.Windows.Clipboard.GetText();
                
                try
                {
                    // Set text to clipboard
                    System.Windows.Clipboard.SetText(text);
                    
                    // Send Ctrl+V
                    System.Windows.Forms.SendKeys.SendWait("^v");
                    
                    // Small delay to let the paste complete
                    System.Threading.Thread.Sleep(100);
                    
                    // Restore original clipboard content
                    if (!string.IsNullOrEmpty(currentClipboard))
                    {
                        System.Windows.Clipboard.SetText(currentClipboard);
                    }
                }
                catch
                {
                    // Restore clipboard on error
                    if (!string.IsNullOrEmpty(currentClipboard))
                    {
                        System.Windows.Clipboard.SetText(currentClipboard);
                    }
                    throw;
                }
            });
            
            _logger.LogDebug("Text inserted via clipboard successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert text via clipboard");
            return false;
        }
    }

    private void OnTextInserted(string text, TextInsertionMethod method)
    {
        try
        {
            var eventArgs = new TextInsertedEventArgs
            {
                Text = text,
                Method = method,
                TargetApplication = "Unknown", // TODO: Get actual active application
                Timestamp = DateTime.UtcNow
            };

            TextInserted?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing text inserted event");
        }
    }

    private void OnTextInsertionFailed(string text, TextInsertionMethod method, string errorMessage, Exception? exception)
    {
        try
        {
            var eventArgs = new TextInsertionErrorEventArgs
            {
                Text = text,
                Method = method,
                ErrorMessage = errorMessage,
                Exception = exception,
                Timestamp = DateTime.UtcNow
            };

            TextInsertionFailed?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing text insertion failed event");
        }
    }
}
