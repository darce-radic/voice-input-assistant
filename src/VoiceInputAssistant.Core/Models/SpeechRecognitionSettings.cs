namespace VoiceInputAssistant.Core.Models;

public class SpeechRecognitionSettings
{
    public bool EnableTranscription { get; set; } = true;
    
    /// <summary>
    /// Speech engine to use (alias for PreferredEngine)
    /// </summary>
    public SpeechEngineType Engine { get; set; } = SpeechEngineType.WhisperLocal;
    
    /// <summary>
    /// Recognition quality setting
    /// </summary>
    public string Quality { get; set; } = "High";
    
    /// <summary>
    /// Primary language for recognition (alias for PrimaryLanguage)
    /// </summary>
    public string Language { get; set; } = "en-US";
    
    public SpeechEngineType PreferredEngine { get; set; } = SpeechEngineType.WhisperLocal;
    
    public string PrimaryLanguage { get; set; } = "en-US";
    
    public List<string> SecondaryLanguages { get; set; } = new();
    
    public bool AutoDetectLanguage { get; set; }
    
    public Dictionary<string, float> LanguageWeights { get; set; } = new();
    
    public float MinimumConfidence { get; set; } = 0.7f;
    
    public bool RequireHighConfidence { get; set; }
    
    public bool EnableProfanityFilter { get; set; }
    
    public bool EnableAutoPunctuation { get; set; } = true;
    
    public bool EnableSpeakerDiarization { get; set; }
    
    public bool EnableWordTimings { get; set; }
    
    public VoiceTriggerSettings? VoiceTrigger { get; set; }
    
    public Dictionary<string, string>? CustomEngineSettings { get; set; }
}