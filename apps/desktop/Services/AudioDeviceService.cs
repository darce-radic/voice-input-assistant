using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Interfaces;

namespace VoiceInputAssistant.Services;

/// <summary>
/// Basic implementation of audio device service
/// </summary>
public class AudioDeviceService : IAudioDeviceService
{
    private readonly ILogger<AudioDeviceService> _logger;

    public AudioDeviceService(ILogger<AudioDeviceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<AudioDevice>> GetInputDevicesAsync()
    {
        _logger.LogDebug("Getting available input devices");
        await Task.Delay(10);
        
        return new List<AudioDevice>
        {
            new AudioDevice { Id = Guid.NewGuid().ToString(), Name = "Default Microphone", IsDefault = true },
            new AudioDevice { Id = Guid.NewGuid().ToString(), Name = "Microphone (USB Audio)" },
            new AudioDevice { Id = Guid.NewGuid().ToString(), Name = "Headset Microphone" }
        };
    }

    public async Task<bool> TestInputDeviceAsync(AudioDevice? device, TimeSpan testDuration)
    {
        _logger.LogDebug("Testing input device: {DeviceName} for {Duration}ms", 
            device?.Name ?? "Default", testDuration.TotalMilliseconds);
        
        await Task.Delay((int)testDuration.TotalMilliseconds);
        
        // Simulate successful test
        return true;
    }
}