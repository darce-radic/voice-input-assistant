using System.Text.Json.Serialization;

namespace VoiceInputAssistant.Core.Models;

public class ApplicationProfile
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool IsDefault { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public List<string> ApplicationExecutables { get; set; } = new();
    
    public List<string> WindowTitlePatterns { get; set; } = new();
    
    public List<string> ProcessNamePatterns { get; set; } = new();
    
    public SpeechRecognitionSettings? SpeechRecognitionSettings { get; set; } = new();
    
    public List<HotkeyConfig> HotkeyConfigs { get; set; } = new();
    
    public List<TextProcessingRule> TextProcessingRules { get; set; } = new();
    
    [JsonIgnore]
    public bool IsActive { get; internal set; }
    
    public ApplicationProfile Clone()
    {
        return new ApplicationProfile
        {
            Id = Id,
            Name = Name,
            Description = Description,
            IsDefault = IsDefault,
            IsEnabled = IsEnabled,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            ApplicationExecutables = ApplicationExecutables.ToList(),
            WindowTitlePatterns = WindowTitlePatterns.ToList(),
            ProcessNamePatterns = ProcessNamePatterns.ToList(),
            SpeechRecognitionSettings = SpeechRecognitionSettings == null ? null : new SpeechRecognitionSettings
            {
                EnableTranscription = SpeechRecognitionSettings.EnableTranscription,
                PreferredEngine = SpeechRecognitionSettings.PreferredEngine,
                PrimaryLanguage = SpeechRecognitionSettings.PrimaryLanguage,
                SecondaryLanguages = SpeechRecognitionSettings.SecondaryLanguages.ToList(),
                AutoDetectLanguage = SpeechRecognitionSettings.AutoDetectLanguage,
                LanguageWeights = new Dictionary<string, float>(SpeechRecognitionSettings.LanguageWeights),
                MinimumConfidence = SpeechRecognitionSettings.MinimumConfidence,
                RequireHighConfidence = SpeechRecognitionSettings.RequireHighConfidence,
                EnableProfanityFilter = SpeechRecognitionSettings.EnableProfanityFilter,
                EnableAutoPunctuation = SpeechRecognitionSettings.EnableAutoPunctuation,
                EnableSpeakerDiarization = SpeechRecognitionSettings.EnableSpeakerDiarization,
                EnableWordTimings = SpeechRecognitionSettings.EnableWordTimings,
                VoiceTrigger = SpeechRecognitionSettings.VoiceTrigger == null ? null : new VoiceTriggerSettings
                {
                    WakeWord = SpeechRecognitionSettings.VoiceTrigger.WakeWord,
                    WakeWordSensitivity = SpeechRecognitionSettings.VoiceTrigger.WakeWordSensitivity,
                    TimeoutSeconds = SpeechRecognitionSettings.VoiceTrigger.TimeoutSeconds,
                    RequireConfirmation = SpeechRecognitionSettings.VoiceTrigger.RequireConfirmation
                },
                CustomEngineSettings = SpeechRecognitionSettings.CustomEngineSettings == null ? null :
                    new Dictionary<string, string>(SpeechRecognitionSettings.CustomEngineSettings)
            },
            HotkeyConfigs = HotkeyConfigs.Select(h => new HotkeyConfig
            {
                Id = h.Id,
                Name = h.Name,
                Key = h.Key,
                Modifiers = h.Modifiers,
                Description = h.Description,
                IsEnabled = h.IsEnabled,
                HandleOnKeyUp = h.HandleOnKeyUp,
                Command = h.Command,
                Parameters = h.Parameters == null ? null : new Dictionary<string, string>(h.Parameters)
            }).ToList(),
            TextProcessingRules = TextProcessingRules.Select(r => new TextProcessingRule
            {
                Id = r.Id,
                Name = r.Name,
                Pattern = r.Pattern,
                Replacement = r.Replacement,
                IsRegex = r.IsRegex,
                IsEnabled = r.IsEnabled,
                Priority = r.Priority,
                Description = r.Description,
                Applications = r.Applications?.ToArray()
            }).ToList(),
            IsActive = IsActive
        };
    }
}