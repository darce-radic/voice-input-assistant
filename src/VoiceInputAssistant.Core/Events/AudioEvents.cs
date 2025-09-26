using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Events;

public class AudioDeviceEventArgs : EventArgs
{
    public AudioDevice Device { get; }
    public bool IsRemoved { get; }

    public AudioDeviceEventArgs(AudioDevice device, bool isRemoved = false)
    {
        Device = device;
        IsRemoved = isRemoved;
    }
}

public class AudioLevelEventArgs : EventArgs
{
    public float Level { get; }

    public AudioLevelEventArgs(float level)
    {
        Level = level;
    }
}

public class VoiceActivityEventArgs : EventArgs
{
    public bool IsSpeechDetected { get; }
    public float Confidence { get; }
    public float Energy { get; }

    public VoiceActivityEventArgs(bool isSpeechDetected, float confidence, float energy)
    {
        IsSpeechDetected = isSpeechDetected;
        Confidence = confidence;
        Energy = energy;
    }
}

public class HotkeyPressedEventArgs : EventArgs
{
    public HotkeyConfig Hotkey { get; }
    public bool IsPressed { get; }

    public HotkeyPressedEventArgs(HotkeyConfig hotkey, bool isPressed)
    {
        Hotkey = hotkey;
        IsPressed = isPressed;
    }
}

public class MouseButtonEventArgs : EventArgs
{
    public MouseButton Button { get; }
    public bool IsPressed { get; }
    
    public MouseButtonEventArgs(MouseButton button, bool isPressed)
    {
        Button = button;
        IsPressed = isPressed;
    }
}

public class KeyPressedEventArgs : EventArgs
{
    public VirtualKey Key { get; }
    public ModifierKeys Modifiers { get; }
    public bool IsPressed { get; }

    public KeyPressedEventArgs(VirtualKey key, ModifierKeys modifiers, bool isPressed)
    {
        Key = key;
        Modifiers = modifiers;
        IsPressed = isPressed;
    }
}

