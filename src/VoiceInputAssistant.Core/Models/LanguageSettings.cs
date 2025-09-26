namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Language configuration settings for speech recognition
/// </summary>
public class LanguageSettings
{
    /// <summary>
    /// Primary language to use for recognition
    /// </summary>
    public string PrimaryLanguage { get; set; } = "en-US";
    
    /// <summary>
    /// Additional languages to consider during recognition
    /// </summary>
    public List<string> SecondaryLanguages { get; set; } = new();
    
    /// <summary>
    /// Whether to automatically detect the spoken language
    /// </summary>
    public bool AutoDetectLanguage { get; set; }
    
    /// <summary>
    /// Confidence weights for each supported language
    /// </summary>
    public Dictionary<string, float> LanguageWeights { get; set; } = new();
}