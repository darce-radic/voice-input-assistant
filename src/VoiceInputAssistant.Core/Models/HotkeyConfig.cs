using System;
using System.Collections.Generic;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Models;

public class HotkeyConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public VirtualKey Key { get; set; }
    public ModifierKeys Modifiers { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
    public bool HandleOnKeyUp { get; set; }
    public string? Command { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    
    /// <summary>
    /// Creates a copy of the HotkeyConfig
    /// </summary>
    public HotkeyConfig Clone()
    {
        return new HotkeyConfig
        {
            Id = Id,
            Name = Name,
            Key = Key,
            Modifiers = Modifiers,
            Description = Description,
            IsEnabled = IsEnabled,
            HandleOnKeyUp = HandleOnKeyUp,
            Command = Command,
            Parameters = Parameters != null ? new Dictionary<string, string>(Parameters) : null
        };
    }
}
