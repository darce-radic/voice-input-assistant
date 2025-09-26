namespace VoiceInputAssistant.Core.Models;

public class VoiceTriggerSettings
{
    public string WakeWord { get; set; } = "hey assistant";
    
    public float WakeWordSensitivity { get; set; } = 0.8f;
    
    public int TimeoutSeconds { get; set; } = 10;
    
    public bool RequireConfirmation { get; set; }
}