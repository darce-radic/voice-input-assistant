using System.ComponentModel.DataAnnotations;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Database entity for application profiles
/// </summary>
public class ApplicationProfileEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsDefault { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public List<string> ApplicationExecutables { get; set; } = new();
    
    public List<string> WindowTitlePatterns { get; set; } = new();
    
    public List<string> ProcessNamePatterns { get; set; } = new();
    
    public SpeechRecognitionSettings? SpeechRecognitionSettings { get; set; }
    
    public List<HotkeyConfig> HotkeyConfigs { get; set; } = new();
    
    public List<TextProcessingRule> TextProcessingRules { get; set; } = new();
}