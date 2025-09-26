using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;
using VoiceInputAssistant.Services.Win32;

namespace VoiceInputAssistant.Services;

/// <summary>
/// Service for injecting text into Windows applications using various methods
/// </summary>
public class TextInjectionService : ITextInjectionService
{
    private readonly ILogger<TextInjectionService> _logger;
    private readonly IMetricsService _metricsService;
    
    // Protected field detection patterns
    private static readonly string[] ProtectedClassNames = {
        "Edit", "RichEdit", "RichEdit20A", "RichEdit20W", "RichEdit50W",
        "RICHEDIT_CLASS", "RICHEDIT", "SysListView32"
    };
    
    private static readonly string[] PasswordIndicators = {
        "password", "pwd", "pass", "pin", "secret", "key", "token",
        "credentials", "auth", "login", "signin", "security"
    };

    public TextInjectionService(ILogger<TextInjectionService> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<bool> InsertTextAsync(string text, TextInsertionMethod method = TextInsertionMethod.Automatic, 
        IntPtr? targetWindow = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogWarning("Attempted to insert empty or null text");
            return false;
        }

        var stopwatch = Stopwatch.StartNew();
        var targetHandle = targetWindow ?? Win32Api.GetForegroundWindow();

        try
        {
            _logger.LogDebug("Inserting text using method {Method} to window {Window}: '{Text}'", 
                method, targetHandle, text.Length > 50 ? text.Substring(0, 50) + "..." : text);

            // Check for protected fields first
            if (await IsProtectedFieldAsync(targetHandle))
            {
                _logger.LogWarning("Attempted to insert text into protected field - operation blocked");
                _metricsService.RecordUserAction("TextInjectionBlocked");
                return false;
            }

            // Determine the best method if automatic
            if (method == TextInsertionMethod.Automatic)
            {
                method = await GetBestInsertionMethodAsync(targetHandle);
            }

            bool success = method switch
            {
                TextInsertionMethod.SendKeys => await InsertViaKeyboardSimulationAsync(text, targetHandle, cancellationToken),
                TextInsertionMethod.ClipboardPaste => await InsertViaClipboardAsync(text, targetHandle, cancellationToken),
                TextInsertionMethod.Win32Messages => await InsertViaWin32MessagesAsync(text, targetHandle, cancellationToken),
                TextInsertionMethod.UIAutomation => await InsertViaUIAutomationAsync(text, targetHandle, cancellationToken),
                _ => await InsertViaKeyboardSimulationAsync(text, targetHandle, cancellationToken) // Default fallback
            };

            stopwatch.Stop();
            
            // Record metrics
            _metricsService.RecordPerformanceMetric($"TextInjection_{method}", stopwatch.Elapsed);
            _metricsService.RecordUserAction("TextInserted");

            if (success)
            {
                _logger.LogDebug("Successfully inserted {Length} characters using {Method} in {ElapsedMs}ms", 
                    text.Length, method, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("Failed to insert text using method {Method}", method);
            }

            return success;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error inserting text using method {Method}", method);
            _metricsService.RecordError("TextInjectionService", $"Insert failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsProtectedFieldAsync(IntPtr? windowHandle = null)
    {
        var handle = windowHandle ?? Win32Api.GetForegroundWindow();
        
        try
        {
            // Get window class name
            var className = GetWindowClassName(handle);
            
            // Get window title
            var windowTitle = GetWindowTitle(handle);
            
            // Check for password field indicators
            var titleLower = windowTitle.ToLowerInvariant();
            var classLower = className.ToLowerInvariant();
            
            // Check window title for password indicators
            foreach (var indicator in PasswordIndicators)
            {
                if (titleLower.Contains(indicator))
                {
                    _logger.LogDebug("Protected field detected by title indicator: {Indicator} in '{Title}'", 
                        indicator, windowTitle);
                    return true;
                }
            }
            
            // Check for specific password control classes
            if (classLower.Contains("password") || classLower.Contains("secure"))
            {
                _logger.LogDebug("Protected field detected by class name: {ClassName}", className);
                return true;
            }
            
            // Try to detect password fields using window properties
            if (await IsPasswordFieldByPropertiesAsync(handle))
            {
                return true;
            }
            
            // Additional checks for common applications
            if (await IsKnownSecureApplicationAsync(handle))
            {
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for protected field");
            // When in doubt, assume it's protected for safety
            return true;
        }
    }

    public async Task<TextInsertionMethod> GetBestInsertionMethodAsync(IntPtr? windowHandle = null)
    {
        var handle = windowHandle ?? Win32Api.GetForegroundWindow();
        
        try
        {
            var className = GetWindowClassName(handle);
            var processName = GetProcessName(handle);
            
            // Application-specific recommendations
            if (processName.ToLowerInvariant().Contains("notepad"))
            {
                return TextInsertionMethod.SendKeys; // Notepad works well with keyboard simulation
            }
            
            if (processName.ToLowerInvariant().Contains("chrome") || 
                processName.ToLowerInvariant().Contains("firefox") || 
                processName.ToLowerInvariant().Contains("edge"))
            {
                return TextInsertionMethod.ClipboardPaste; // Browsers often work better with clipboard
            }
            
            if (className.Contains("RichEdit") || className.Contains("RICHEDIT"))
            {
                return TextInsertionMethod.Win32Messages; // Rich text controls support direct messaging
            }
            
            // Default to clipboard paste as it's most reliable across applications
            return TextInsertionMethod.ClipboardPaste;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining best insertion method, using default");
            return TextInsertionMethod.ClipboardPaste;
        }
    }

    public async Task<TextInsertionTestResult> TestInsertionMethodAsync(TextInsertionMethod method, IntPtr? windowHandle = null)
    {
        var handle = windowHandle ?? Win32Api.GetForegroundWindow();
        var stopwatch = Stopwatch.StartNew();
        var result = new TextInsertionTestResult();
        
        try
        {
            const string testText = "TEST";
            
            // Attempt to insert test text
            var success = method switch
            {
                TextInsertionMethod.SendKeys => await InsertViaKeyboardSimulationAsync(testText, handle, CancellationToken.None),
                TextInsertionMethod.ClipboardPaste => await InsertViaClipboardAsync(testText, handle, CancellationToken.None),
                TextInsertionMethod.Win32Messages => await InsertViaWin32MessagesAsync(testText, handle, CancellationToken.None),
                TextInsertionMethod.UIAutomation => await InsertViaUIAutomationAsync(testText, handle, CancellationToken.None),
                _ => false
            };
            
            stopwatch.Stop();
            
            result.IsSupported = success;
            result.IsReliable = success; // Simple test - in production you might do more extensive testing
            result.ResponseTime = stopwatch.Elapsed;
            
            if (success)
            {
                _logger.LogDebug("Test successful for method {Method} in {ElapsedMs}ms", method, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                result.ErrorMessage = $"Method {method} failed to insert test text";
                _logger.LogDebug("Test failed for method {Method}: {Error}", method, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSupported = false;
            result.IsReliable = false;
            result.ErrorMessage = ex.Message;
            result.ResponseTime = stopwatch.Elapsed;
            
            _logger.LogWarning(ex, "Error testing insertion method {Method}", method);
        }
        
        return result;
    }

    private async Task<bool> InsertViaKeyboardSimulationAsync(string text, IntPtr targetWindow, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure the target window is focused
            Win32Api.SetForegroundWindow(targetWindow);
            await Task.Delay(50, cancellationToken); // Small delay for window activation
            
            // Convert text to input events
            var inputs = new List<Input>();
            
            foreach (char c in text)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                
                // Use Unicode input for better international character support
                inputs.Add(new Input
                {
                    Type = Win32Api.INPUT_KEYBOARD,
                    Union = new InputUnion
                    {
                        Keyboard = new KeyboardInput
                        {
                            VirtualKey = 0,
                            ScanCode = c,
                            Flags = Win32Api.KEYEVENTF_UNICODE,
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        }
                    }
                });
            }
            
            // Send the input events
            var result = Win32Api.SendInput((uint)inputs.Count, inputs.ToArray(), Input.Size);
            return result == inputs.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in keyboard simulation text insertion");
            return false;
        }
    }

    private async Task<bool> InsertViaClipboardAsync(string text, IntPtr targetWindow, CancellationToken cancellationToken)
    {
        string? originalClipboard = null;
        
        try
        {
            // Save current clipboard content
            if (Clipboard.ContainsText())
            {
                originalClipboard = Clipboard.GetText();
            }
            
            // Set our text to clipboard
            Clipboard.SetText(text);
            
            // Ensure the target window is focused
            Win32Api.SetForegroundWindow(targetWindow);
            await Task.Delay(50, cancellationToken);
            
            // Send Ctrl+V to paste
            var inputs = new Input[]
            {
                // Ctrl down
                new Input
                {
                    Type = Win32Api.INPUT_KEYBOARD,
                    Union = new InputUnion
                    {
                        Keyboard = new KeyboardInput
                        {
                            VirtualKey = Win32Api.VK_CONTROL,
                            ScanCode = 0,
                            Flags = 0,
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        }
                    }
                },
                // V down
                new Input
                {
                    Type = Win32Api.INPUT_KEYBOARD,
                    Union = new InputUnion
                    {
                        Keyboard = new KeyboardInput
                        {
                            VirtualKey = Win32Api.VK_V,
                            ScanCode = 0,
                            Flags = 0,
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        }
                    }
                },
                // V up
                new Input
                {
                    Type = Win32Api.INPUT_KEYBOARD,
                    Union = new InputUnion
                    {
                        Keyboard = new KeyboardInput
                        {
                            VirtualKey = Win32Api.VK_V,
                            ScanCode = 0,
                            Flags = Win32Api.KEYEVENTF_KEYUP,
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        }
                    }
                },
                // Ctrl up
                new Input
                {
                    Type = Win32Api.INPUT_KEYBOARD,
                    Union = new InputUnion
                    {
                        Keyboard = new KeyboardInput
                        {
                            VirtualKey = Win32Api.VK_CONTROL,
                            ScanCode = 0,
                            Flags = Win32Api.KEYEVENTF_KEYUP,
                            Time = 0,
                            ExtraInfo = IntPtr.Zero
                        }
                    }
                }
            };
            
            var result = Win32Api.SendInput((uint)inputs.Length, inputs, Input.Size);
            await Task.Delay(100, cancellationToken); // Wait for paste to complete
            
            return result == inputs.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in clipboard text insertion");
            return false;
        }
        finally
        {
            try
            {
                // Restore original clipboard content
                if (originalClipboard != null)
                {
                    Clipboard.SetText(originalClipboard);
                }
                else
                {
                    Clipboard.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore clipboard content");
            }
        }
    }

    private async Task<bool> InsertViaWin32MessagesAsync(string text, IntPtr targetWindow, CancellationToken cancellationToken)
    {
        try
        {
            // Try WM_SETTEXT first (works for simple edit controls)
            var textPtr = Marshal.StringToHGlobalUni(text);
            try
            {
                var result = Win32Api.SendMessage(targetWindow, Win32Api.WM_SETTEXT, IntPtr.Zero, textPtr);
                if (result != IntPtr.Zero)
                {
                    return true;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(textPtr);
            }
            
            // Fallback to character-by-character WM_CHAR messages
            foreach (char c in text)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                
                Win32Api.SendMessage(targetWindow, Win32Api.WM_CHAR, new IntPtr(c), IntPtr.Zero);
                await Task.Delay(1, cancellationToken); // Small delay between characters
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Win32 message text insertion");
            return false;
        }
    }

    private async Task<bool> InsertViaUIAutomationAsync(string text, IntPtr targetWindow, CancellationToken cancellationToken)
    {
        try
        {
            // UI Automation implementation would go here
            // This is a placeholder - full implementation would use System.Windows.Automation
            _logger.LogDebug("UI Automation text insertion not fully implemented yet");
            
            // Fallback to clipboard method for now
            return await InsertViaClipboardAsync(text, targetWindow, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UI Automation text insertion");
            return false;
        }
    }

    private async Task<bool> IsPasswordFieldByPropertiesAsync(IntPtr handle)
    {
        try
        {
            // This is a simplified check - in a full implementation you might check
            // for edit control styles that indicate password fields
            var className = GetWindowClassName(handle);
            
            // Check for password-style edit controls
            if (className.ToLowerInvariant().Contains("edit"))
            {
                // In a full implementation, you would check the ES_PASSWORD style
                // For now, we'll do a basic heuristic check
                var windowTitle = GetWindowTitle(handle);
                return windowTitle.ToLowerInvariant().Contains("password");
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsKnownSecureApplicationAsync(IntPtr handle)
    {
        try
        {
            var processName = GetProcessName(handle).ToLowerInvariant();
            
            // Known secure applications that we should be cautious with
            var secureApps = new[] { "keepass", "lastpass", "1password", "dashlane", "bitwarden", "roboform" };
            
            foreach (var app in secureApps)
            {
                if (processName.Contains(app))
                {
                    _logger.LogDebug("Detected secure application: {ProcessName}", processName);
                    return true;
                }
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string GetWindowClassName(IntPtr handle)
    {
        var className = new StringBuilder(256);
        Win32Api.GetClassName(handle, className, className.Capacity);
        return className.ToString();
    }

    private string GetWindowTitle(IntPtr handle)
    {
        var length = Win32Api.GetWindowTextLength(handle);
        if (length == 0) return string.Empty;
        
        var title = new StringBuilder(length + 1);
        Win32Api.GetWindowText(handle, title, title.Capacity);
        return title.ToString();
    }

    private string GetProcessName(IntPtr handle)
    {
        try
        {
            Win32Api.GetWindowThreadProcessId(handle, out uint processId);
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }
}