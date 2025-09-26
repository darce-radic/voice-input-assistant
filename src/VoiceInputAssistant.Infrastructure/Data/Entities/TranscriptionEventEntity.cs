using System.ComponentModel.DataAnnotations;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Database entity for transcription events
/// </summary>
public class TranscriptionEventEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string Text { get; set; } = string.Empty;
    
    public string? ProcessedText { get; set; }
    
    [MaxLength(256)]
    public string? ApplicationName { get; set; }
    
    public SpeechEngineType Engine { get; set; }
    
    public float ConfidenceScore { get; set; }
    
    public int ProcessingTimeMs { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string? LanguageCode { get; set; }
    
    public Dictionary<string, string>? Metadata { get; set; }
}