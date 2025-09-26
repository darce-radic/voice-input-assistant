using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for audio device service  
/// </summary>
public interface IAudioDeviceService
{
    Task<List<AudioDevice>> GetInputDevicesAsync();
    Task<bool> TestInputDeviceAsync(AudioDevice? device, TimeSpan testDuration);
}