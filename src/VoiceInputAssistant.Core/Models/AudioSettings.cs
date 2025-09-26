namespace VoiceInputAssistant.Core.Models;

public class AudioSettings
{
    public string? PreferredInputDevice { get; set; }
    
    public string? PreferredOutputDevice { get; set; }
    
    public int InputVolume { get; set; } = 100;
    
    public int OutputVolume { get; set; } = 100;
    
    public bool MuteInput { get; set; }
    
    public bool MuteOutput { get; set; }
    
    public int NoiseThreshold { get; set; } = 10;
    
    public bool AutoAdjustNoiseThreshold { get; set; } = true;
    
    /// <summary>
    /// Creates a copy of the AudioSettings
    /// </summary>
    public AudioSettings Clone()
    {
        return new AudioSettings
        {
            PreferredInputDevice = PreferredInputDevice,
            PreferredOutputDevice = PreferredOutputDevice,
            InputVolume = InputVolume,
            OutputVolume = OutputVolume,
            MuteInput = MuteInput,
            MuteOutput = MuteOutput,
            NoiseThreshold = NoiseThreshold,
            AutoAdjustNoiseThreshold = AutoAdjustNoiseThreshold
        };
    }
}
