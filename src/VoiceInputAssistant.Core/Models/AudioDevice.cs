namespace VoiceInputAssistant.Core.Models;

public class AudioDevice
{
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public bool IsDefault { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public bool IsInput { get; set; }
    
    public int Volume { get; set; }
    
    public bool IsMuted { get; set; }
}