using System;

namespace VoiceInputAssistant.Models;

/// <summary>
/// Voice activity detection result
/// </summary>
public class VoiceActivityResult
{
    public bool HasVoice { get; set; }
    public double Confidence { get; set; }
    public double EnergyLevel { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}