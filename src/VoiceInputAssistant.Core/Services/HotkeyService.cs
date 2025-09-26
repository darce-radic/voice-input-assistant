using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using EventsNamespace = VoiceInputAssistant.Core.Events;

namespace VoiceInputAssistant.Core.Services
{
    /// <summary>
    /// Windows implementation of the hotkey service using Win32 APIs
    /// </summary>
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly ILogger<HotkeyService> _logger;
        private readonly ConcurrentDictionary<string, RegisteredHotkey> _registeredHotkeys;
        private readonly object _hookLock = new object();
        
        // Win32 API constants
        private const int WM_HOTKEY = 0x0312;
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;

        // Hook handles
        private IntPtr _keyboardHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;
        private LowLevelKeyboardProc _keyboardProc;
        private LowLevelMouseProc _mouseProc;
        
        // State tracking
        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isInputMonitoringEnabled;
        private int _nextHotkeyId = 1000;
        private HotkeyConfig _pushToTalkHotkey;
        private bool _isPushToTalkPressed;
        
        // Hidden window for receiving hotkey messages
        private HotkeyWindow _hotkeyWindow;

public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;
public event EventHandler<VoiceInputAssistant.Core.Services.Interfaces.MouseEventArgs> MouseButtonPressed;
public event EventHandler<KeyboardEventArgs> KeyPressed;

        public bool IsInitialized => _isInitialized;
        public bool IsMonitoring => _isInitialized && _registeredHotkeys.Count > 0;
        public bool IsPushToTalkPressed => _isPushToTalkPressed;

        public HotkeyService(ILogger<HotkeyService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredHotkeys = new ConcurrentDictionary<string, RegisteredHotkey>();
            
            // Create delegates for hooks to prevent garbage collection
            _keyboardProc = KeyboardHookProc;
            _mouseProc = MouseHookProc;
            
            _logger.LogDebug("HotkeyService initialized");
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                // Create hidden window for receiving hotkey messages
                await Task.Run(() =>
                {
                    _hotkeyWindow = new HotkeyWindow(this);
                });

                _isInitialized = true;
                _logger.LogInformation("HotkeyService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize HotkeyService");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_isInitialized || _isDisposed)
                return;

            try
            {
                // Unregister all hotkeys
                await UnregisterAllHotkeysAsync();
                
                // Stop input monitoring
                await SetInputMonitoringAsync(false);
                
                // Destroy window
                _hotkeyWindow?.DestroyHandle();
                _hotkeyWindow = null;

                _isInitialized = false;
                _logger.LogInformation("HotkeyService shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during HotkeyService shutdown");
            }
        }

        public async Task<bool> RegisterHotkeyAsync(HotkeyConfig hotkey)
        {
            if (!_isInitialized || hotkey == null)
                return false;

            try
            {
                // Check if hotkey is already registered
                var hotkeyIdStr = hotkey.Id.ToString();
                if (_registeredHotkeys.ContainsKey(hotkeyIdStr))
                {
                    await UnregisterHotkeyAsync(hotkeyIdStr);
                }

                var hotkeyId = _nextHotkeyId++;
                var modifiers = ConvertToWin32Modifiers(hotkey.Modifiers);
                var vkCode = (uint)hotkey.Key;

                bool success = RegisterHotKey(_hotkeyWindow.Handle, hotkeyId, modifiers, vkCode);
                
                if (success)
                {
                    var registeredHotkey = new RegisteredHotkey
                    {
                        Config = hotkey,
                        Id = hotkeyId,
                        Handle = _hotkeyWindow.Handle
                    };
                    
                    _registeredHotkeys.TryAdd(hotkeyIdStr, registeredHotkey);
                    _logger.LogDebug("Registered hotkey: {Hotkey} with ID {Id}", hotkey.ToString(), hotkeyId);
                    return true;
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogWarning("Failed to register hotkey {Hotkey}: Win32 error {Error}", hotkey.ToString(), error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering hotkey {Hotkey}", hotkey?.ToString());
                return false;
            }
        }

        public async Task<bool> UnregisterHotkeyAsync(string hotkeyId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(hotkeyId))
                return false;

            try
            {
                if (_registeredHotkeys.TryRemove(hotkeyId, out var registeredHotkey))
                {
                    bool success = UnregisterHotKey(registeredHotkey.Handle, registeredHotkey.Id);
                    
                    if (success)
                    {
                        _logger.LogDebug("Unregistered hotkey: {Hotkey}", registeredHotkey.Config.ToString());
                        return true;
                    }
                    else
                    {
                        var error = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to unregister hotkey {Hotkey}: Win32 error {Error}", 
                            registeredHotkey.Config.ToString(), error);
                        return false;
                    }
                }
                
                return true; // Already unregistered
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering hotkey {HotkeyId}", hotkeyId);
                return false;
            }
        }

        public async Task UnregisterAllHotkeysAsync()
        {
            var tasks = _registeredHotkeys.Keys.Select(id => UnregisterHotkeyAsync(id));
            await Task.WhenAll(tasks);
        }

        public async Task<IEnumerable<HotkeyConfig>> GetRegisteredHotkeysAsync()
        {
            return await Task.FromResult(_registeredHotkeys.Values.Select(rh => rh.Config).ToList());
        }

        public async Task<bool> UpdateHotkeyAsync(HotkeyConfig hotkey)
        {
            if (hotkey == null)
                return false;

            // Simply unregister and re-register
            await UnregisterHotkeyAsync(hotkey.Id);
            return await RegisterHotkeyAsync(hotkey);
        }

public async Task<bool> IsHotkeyAvailableAsync(ModifierKeys modifiers, VirtualKey key)
        {
            try
            {
                var testHotkey = new HotkeyConfig
                {
                    Id = Guid.NewGuid().ToString(),
                    Modifiers = modifiers,
                    Key = key
                };

                // Try to register temporarily
                if (await RegisterHotkeyAsync(testHotkey))
                {
                    await UnregisterHotkeyAsync(testHotkey.Id);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task SetInputMonitoringAsync(bool enable)
        {
            if (_isInputMonitoringEnabled == enable)
                return;

            try
            {
                if (enable)
                {
                    await Task.Run(() =>
                    {
                        lock (_hookLock)
                        {
                            // Install keyboard hook
                            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc,
                                GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                            
                            // Install mouse hook
                            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc,
                                GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                        }
                    });

                    if (_keyboardHook != IntPtr.Zero && _mouseHook != IntPtr.Zero)
                    {
                        _isInputMonitoringEnabled = true;
                        _logger.LogDebug("Input monitoring enabled");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to install input hooks");
                    }
                }
                else
                {
                    lock (_hookLock)
                    {
                        if (_keyboardHook != IntPtr.Zero)
                        {
                            UnhookWindowsHookEx(_keyboardHook);
                            _keyboardHook = IntPtr.Zero;
                        }
                        
                        if (_mouseHook != IntPtr.Zero)
                        {
                            UnhookWindowsHookEx(_mouseHook);
                            _mouseHook = IntPtr.Zero;
                        }
                    }
                    
                    _isInputMonitoringEnabled = false;
                    _logger.LogDebug("Input monitoring disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting input monitoring to {Enabled}", enable);
            }
        }

        public async Task SetPushToTalkHotkeyAsync(HotkeyConfig hotkey)
        {
            _pushToTalkHotkey = hotkey;
            
            if (hotkey != null && !_isInputMonitoringEnabled)
            {
                await SetInputMonitoringAsync(true);
            }
            
            _logger.LogDebug("Push-to-talk hotkey set to: {Hotkey}", hotkey?.ToString() ?? "None");
        }

        public async Task SimulateKeyPressAsync(VirtualKey key, ModifierKeys modifiers = ModifierKeys.None)
        {
            try
            {
                await Task.Run(() =>
                {
                    var inputs = new List<INPUT>();
                    
                    // Press modifier keys
                    if (modifiers.HasFlag(ModifierKeys.Control))
                        inputs.Add(CreateKeyInput(VirtualKey.Control, false));
                    if (modifiers.HasFlag(ModifierKeys.Alt))
                        inputs.Add(CreateKeyInput(VirtualKey.Alt, false));
                    if (modifiers.HasFlag(ModifierKeys.Shift))
                        inputs.Add(CreateKeyInput(VirtualKey.Shift, false));
                    if (modifiers.HasFlag(ModifierKeys.Windows))
                        inputs.Add(CreateKeyInput(VirtualKey.LeftWindows, false));
                    
                    // Press main key
                    inputs.Add(CreateKeyInput(key, false));
                    
                    // Release main key
                    inputs.Add(CreateKeyInput(key, true));
                    
                    // Release modifier keys (in reverse order)
                    if (modifiers.HasFlag(ModifierKeys.Windows))
                        inputs.Add(CreateKeyInput(VirtualKey.LeftWindows, true));
                    if (modifiers.HasFlag(ModifierKeys.Shift))
                        inputs.Add(CreateKeyInput(VirtualKey.Shift, true));
                    if (modifiers.HasFlag(ModifierKeys.Alt))
                        inputs.Add(CreateKeyInput(VirtualKey.Alt, true));
                    if (modifiers.HasFlag(ModifierKeys.Control))
                        inputs.Add(CreateKeyInput(VirtualKey.Control, true));
                    
                    SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                });
                
                _logger.LogDebug("Simulated key press: {Key} with modifiers {Modifiers}", key, modifiers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating key press: {Key} with modifiers {Modifiers}", key, modifiers);
            }
        }

public async Task<WindowInfo> GetFocusedWindowAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    var hwnd = GetForegroundWindow();
                    if (hwnd == IntPtr.Zero)
                        return null;

                    var windowInfo = new WindowInfo
                    {
                        Handle = hwnd
                    };

                    // Get window title
                    var titleBuilder = new StringBuilder(256);
                    GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
                    windowInfo.Title = titleBuilder.ToString();

                    // Get class name
                    var classBuilder = new StringBuilder(256);
                    GetClassName(hwnd, classBuilder, classBuilder.Capacity);
                    windowInfo.ClassName = classBuilder.ToString();

                    // Get process information
                    GetWindowThreadProcessId(hwnd, out uint processId);
                    windowInfo.ProcessId = (int)processId;

                    try
                    {
                        var process = Process.GetProcessById((int)processId);
                        windowInfo.ProcessName = process.ProcessName;
                    }
                    catch
                    {
                        windowInfo.ProcessName = "Unknown";
                    }

                    // Get window bounds
                    if (GetWindowRect(hwnd, out RECT rect))
                    {
                        windowInfo.Bounds = new VoiceInputAssistant.Core.Services.Interfaces.Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
                    }

                    // Get window state
                    windowInfo.IsVisible = IsWindowVisible(hwnd);
                    windowInfo.IsMinimized = IsIconic(hwnd);
                    windowInfo.IsMaximized = IsZoomed(hwnd);

                    return windowInfo;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting focused window information");
                return null;
            }
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    var vkCode = Marshal.ReadInt32(lParam);
                    var key = (VirtualKey)vkCode;
                    var action = wParam.ToInt32() == WM_KEYDOWN ? KeyAction.KeyDown : KeyAction.KeyUp;
                    
                    // Check for push-to-talk
                    if (_pushToTalkHotkey != null && key == _pushToTalkHotkey.Key)
                    {
                        var modifiersPressed = GetCurrentModifiers();
                        if (modifiersPressed == _pushToTalkHotkey.Modifiers)
                        {
                            _isPushToTalkPressed = action == KeyAction.KeyDown;
                        }
                    }

                    // Raise keyboard event
                    KeyPressed?.Invoke(this, new KeyboardEventArgs
                    {
                        Key = key,
                        Modifiers = GetCurrentModifiers(),
                        Action = action,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in keyboard hook procedure");
                }
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    var mouseStruct = Marshal.PtrToStructure<POINT>(lParam);
                    var button = GetMouseButtonFromMessage(wParam.ToInt32());
                    var action = GetMouseActionFromMessage(wParam.ToInt32());

                    if (button != null && action != null)
                    {
                        MouseButtonPressed?.Invoke(this, new VoiceInputAssistant.Core.Services.Interfaces.MouseEventArgs
                        {
                            Button = button.Value,
                            Action = action.Value,
                            X = mouseStruct.X,
                            Y = mouseStruct.Y,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in mouse hook procedure");
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void OnHotkeyPressed(int id)
        {
            try
            {
                var registeredHotkey = _registeredHotkeys.Values.FirstOrDefault(rh => rh.Id == id);
                if (registeredHotkey != null)
                {
                    var eventArgs = new HotkeyPressedEventArgs
                    {
                        Hotkey = registeredHotkey.Config,
                        Timestamp = DateTime.UtcNow
                    };

                    HotkeyPressed?.Invoke(this, eventArgs);
                    _logger.LogDebug("Hotkey pressed: {Hotkey}", registeredHotkey.Config.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling hotkey press for ID {Id}", id);
            }
        }

        private static uint ConvertToWin32Modifiers(ModifierKeys modifiers)
        {
            uint result = 0;
            
            if (modifiers.HasFlag(ModifierKeys.Alt))
                result |= 0x0001; // MOD_ALT
            if (modifiers.HasFlag(ModifierKeys.Control))
                result |= 0x0002; // MOD_CONTROL
            if (modifiers.HasFlag(ModifierKeys.Shift))
                result |= 0x0004; // MOD_SHIFT
            if (modifiers.HasFlag(ModifierKeys.Windows))
                result |= 0x0008; // MOD_WIN
                
            return result;
        }
        
        private static uint ConvertToWin32Modifiers(System.Windows.Forms.Keys keys)
        {
            uint result = 0;
            
            if (keys.HasFlag(System.Windows.Forms.Keys.Alt))
                result |= 0x0001; // MOD_ALT
            if (keys.HasFlag(System.Windows.Forms.Keys.Control))
                result |= 0x0002; // MOD_CONTROL
            if (keys.HasFlag(System.Windows.Forms.Keys.Shift))
                result |= 0x0004; // MOD_SHIFT
            if (keys.HasFlag(System.Windows.Forms.Keys.LWin) || keys.HasFlag(System.Windows.Forms.Keys.RWin))
                result |= 0x0008; // MOD_WIN
                
            return result;
        }

        private static ModifierKeys GetCurrentModifiers()
        {
            var modifiers = ModifierKeys.None;
            
            if ((GetAsyncKeyState((int)VirtualKey.Control) & 0x8000) != 0)
                modifiers |= ModifierKeys.Control;
            if ((GetAsyncKeyState((int)VirtualKey.Alt) & 0x8000) != 0)
                modifiers |= ModifierKeys.Alt;
            if ((GetAsyncKeyState((int)VirtualKey.Shift) & 0x8000) != 0)
                modifiers |= ModifierKeys.Shift;
            if ((GetAsyncKeyState((int)VirtualKey.LeftWindows) & 0x8000) != 0 || 
                (GetAsyncKeyState((int)VirtualKey.RightWindows) & 0x8000) != 0)
                modifiers |= ModifierKeys.Windows;
                
            return modifiers;
        }

        private static MouseButton? GetMouseButtonFromMessage(int message)
        {
            return message switch
            {
                WM_LBUTTONDOWN or WM_LBUTTONUP => MouseButton.Left,
                WM_RBUTTONDOWN or WM_RBUTTONUP => MouseButton.Right,
                WM_MBUTTONDOWN or WM_MBUTTONUP => MouseButton.Middle,
                _ => null
            };
        }

        private static MouseAction? GetMouseActionFromMessage(int message)
        {
            return message switch
            {
                WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN => MouseAction.ButtonDown,
                WM_LBUTTONUP or WM_RBUTTONUP or WM_MBUTTONUP => MouseAction.ButtonUp,
                _ => null
            };
        }

        private static INPUT CreateKeyInput(VirtualKey key, bool keyUp)
        {
            return new INPUT
            {
                Type = 1, // INPUT_KEYBOARD
                Data = new INPUTUNION
                {
                    Keyboard = new KEYBDINPUT
                    {
                        Vk = (ushort)key,
                        Scan = 0,
                        Flags = keyUp ? 2u : 0u, // KEYEVENTF_KEYUP
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                ShutdownAsync().GetAwaiter().GetResult();
                _isDisposed = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing HotkeyService");
            }
        }

        // Win32 API declarations
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // Delegates
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Structures
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint Type;
            public INPUTUNION Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        // Helper classes
        private class RegisteredHotkey
        {
            public HotkeyConfig Config { get; set; }
            public int Id { get; set; }
            public IntPtr Handle { get; set; }
        }

        private class HotkeyWindow : NativeWindow
        {
            private readonly HotkeyService _service;

            public HotkeyWindow(HotkeyService service)
            {
                _service = service;
                
                // Create the window handle immediately
                CreateParams cp = new CreateParams
                {
                    Caption = "HotkeyWindow",
                    ClassName = null,
                    X = 0,
                    Y = 0,
                    Width = 0,
                    Height = 0,
                    Style = 0, // Hidden window
                    ExStyle = 0,
                    Parent = IntPtr.Zero
                };
                
                CreateHandle(cp);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    _service.OnHotkeyPressed(m.WParam.ToInt32());
                }
                else
                {
                    base.WndProc(ref m);
                }
            }
        }
    }
}