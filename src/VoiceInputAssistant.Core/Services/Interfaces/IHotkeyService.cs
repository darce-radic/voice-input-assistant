using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for managing global hotkeys and input detection
    /// </summary>
    public interface IHotkeyService
    {
        /// <summary>
        /// Event raised when a registered hotkey is pressed
        /// </summary>
        event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        /// <summary>
        /// Event raised when a mouse button is pressed (if monitoring is enabled)
        /// </summary>
        event EventHandler<MouseEventArgs> MouseButtonPressed;

        /// <summary>
        /// Event raised when a keyboard key is pressed (if monitoring is enabled)
        /// </summary>
        event EventHandler<KeyboardEventArgs> KeyPressed;

        /// <summary>
        /// Gets whether the service is initialized and monitoring
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets whether global hotkey monitoring is enabled
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Initializes the hotkey service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the hotkey service and releases all resources
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Registers a global hotkey
        /// </summary>
        /// <param name="hotkey">Hotkey configuration to register</param>
        /// <returns>True if the hotkey was registered successfully</returns>
        Task<bool> RegisterHotkeyAsync(HotkeyConfig hotkey);

        /// <summary>
        /// Unregisters a global hotkey
        /// </summary>
        /// <param name="hotkeyId">ID of the hotkey to unregister</param>
        /// <returns>True if the hotkey was unregistered successfully</returns>
        Task<bool> UnregisterHotkeyAsync(string hotkeyId);

        /// <summary>
        /// Unregisters all hotkeys
        /// </summary>
        Task UnregisterAllHotkeysAsync();

        /// <summary>
        /// Gets all currently registered hotkeys
        /// </summary>
        /// <returns>Collection of registered hotkeys</returns>
        Task<IEnumerable<HotkeyConfig>> GetRegisteredHotkeysAsync();

        /// <summary>
        /// Updates an existing hotkey registration
        /// </summary>
        /// <param name="hotkey">Updated hotkey configuration</param>
        /// <returns>True if the hotkey was updated successfully</returns>
        Task<bool> UpdateHotkeyAsync(HotkeyConfig hotkey);

        /// <summary>
        /// Checks if a hotkey combination is available for registration
        /// </summary>
        /// <param name="modifiers">Modifier keys (Ctrl, Alt, Shift, Win)</param>
        /// <param name="key">Main key</param>
        /// <returns>True if the combination is available</returns>
        Task<bool> IsHotkeyAvailableAsync(ModifierKeys modifiers, VirtualKey key);

        /// <summary>
        /// Enables or disables global input monitoring (keyboard and mouse)
        /// </summary>
        /// <param name="enable">Whether to enable monitoring</param>
        Task SetInputMonitoringAsync(bool enable);

        /// <summary>
        /// Gets the current push-to-talk state
        /// </summary>
        bool IsPushToTalkPressed { get; }

        /// <summary>
        /// Enables or disables push-to-talk monitoring
        /// </summary>
        /// <param name="hotkey">Hotkey to use for push-to-talk, or null to disable</param>
        Task SetPushToTalkHotkeyAsync(HotkeyConfig hotkey);

        /// <summary>
        /// Simulates a key press (for testing or automation)
        /// </summary>
        /// <param name="key">Key to simulate</param>
        /// <param name="modifiers">Modifier keys to include</param>
        Task SimulateKeyPressAsync(VirtualKey key, ModifierKeys modifiers = ModifierKeys.None);

        /// <summary>
        /// Gets the currently focused window information
        /// </summary>
        /// <returns>Information about the focused window</returns>
        Task<WindowInfo> GetFocusedWindowAsync();
    }

    /// <summary>
    /// Event arguments for hotkey press events
    /// </summary>
    public class HotkeyPressedEventArgs : EventArgs
    {
        public HotkeyConfig Hotkey { get; set; }
        public DateTime Timestamp { get; set; }
        public WindowInfo FocusedWindow { get; set; }
    }

    /// <summary>
    /// Event arguments for mouse events
    /// </summary>
    public class MouseEventArgs : EventArgs
    {
        public MouseButton Button { get; set; }
        public MouseAction Action { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public DateTime Timestamp { get; set; }
        public WindowInfo FocusedWindow { get; set; }
    }

    /// <summary>
    /// Event arguments for keyboard events
    /// </summary>
    public class KeyboardEventArgs : EventArgs
    {
        public VirtualKey Key { get; set; }
        public KeyAction Action { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public DateTime Timestamp { get; set; }
        public WindowInfo FocusedWindow { get; set; }
    }

    /// <summary>
    /// Modifier keys for hotkey combinations
    /// </summary>
    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    /// <summary>
    /// Virtual key codes for keyboard keys
    /// </summary>
    public enum VirtualKey
    {
        None = 0,
        LeftButton = 0x01,
        RightButton = 0x02,
        Cancel = 0x03,
        MiddleButton = 0x04,
        ExtraButton1 = 0x05,
        ExtraButton2 = 0x06,
        Back = 0x08,
        Tab = 0x09,
        LineFeed = 0x0A,
        Clear = 0x0C,
        Return = 0x0D,
        Enter = Return,
        Shift = 0x10,
        Control = 0x11,
        Menu = 0x12,
        Alt = Menu,
        Pause = 0x13,
        Capital = 0x14,
        CapsLock = Capital,
        Kana = 0x15,
        Hangeul = 0x15,
        Hangul = 0x15,
        Junja = 0x17,
        Final = 0x18,
        Hanja = 0x19,
        Kanji = 0x19,
        Escape = 0x1B,
        Convert = 0x1C,
        NonConvert = 0x1D,
        Accept = 0x1E,
        ModeChange = 0x1F,
        Space = 0x20,
        Prior = 0x21,
        PageUp = Prior,
        Next = 0x22,
        PageDown = Next,
        End = 0x23,
        Home = 0x24,
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,
        Select = 0x29,
        Print = 0x2A,
        Execute = 0x2B,
        Snapshot = 0x2C,
        PrintScreen = Snapshot,
        Insert = 0x2D,
        Delete = 0x2E,
        Help = 0x2F,
        D0 = 0x30,
        D1 = 0x31,
        D2 = 0x32,
        D3 = 0x33,
        D4 = 0x34,
        D5 = 0x35,
        D6 = 0x36,
        D7 = 0x37,
        D8 = 0x38,
        D9 = 0x39,
        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,
        LeftWindows = 0x5B,
        RightWindows = 0x5C,
        Applications = 0x5D,
        Sleep = 0x5F,
        NumPad0 = 0x60,
        NumPad1 = 0x61,
        NumPad2 = 0x62,
        NumPad3 = 0x63,
        NumPad4 = 0x64,
        NumPad5 = 0x65,
        NumPad6 = 0x66,
        NumPad7 = 0x67,
        NumPad8 = 0x68,
        NumPad9 = 0x69,
        Multiply = 0x6A,
        Add = 0x6B,
        Separator = 0x6C,
        Subtract = 0x6D,
        Decimal = 0x6E,
        Divide = 0x6F,
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,
        F13 = 0x7C,
        F14 = 0x7D,
        F15 = 0x7E,
        F16 = 0x7F,
        F17 = 0x80,
        F18 = 0x81,
        F19 = 0x82,
        F20 = 0x83,
        F21 = 0x84,
        F22 = 0x85,
        F23 = 0x86,
        F24 = 0x87,
        NumLock = 0x90,
        Scroll = 0x91,
        ScrollLock = Scroll,
        LeftShift = 0xA0,
        RightShift = 0xA1,
        LeftControl = 0xA2,
        RightControl = 0xA3,
        LeftMenu = 0xA4,
        LeftAlt = LeftMenu,
        RightMenu = 0xA5,
        RightAlt = RightMenu
    }

    /// <summary>
    /// Mouse button identifiers
    /// </summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        Extra1,
        Extra2
    }

    /// <summary>
    /// Mouse action types
    /// </summary>
    public enum MouseAction
    {
        ButtonDown,
        ButtonUp,
        Click,
        DoubleClick,
        Move,
        Wheel
    }

    /// <summary>
    /// Keyboard action types
    /// </summary>
    public enum KeyAction
    {
        KeyDown,
        KeyUp,
        KeyPress
    }

    /// <summary>
    /// Information about a window
    /// </summary>
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public string ProcessName { get; set; }
        public string ClassName { get; set; }
        public int ProcessId { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsVisible { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsMaximized { get; set; }
    }

    /// <summary>
    /// Simple rectangle structure
    /// </summary>
    public struct Rectangle
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public Rectangle(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}