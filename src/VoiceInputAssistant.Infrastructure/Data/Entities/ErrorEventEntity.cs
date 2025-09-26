using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Database entity for error events
/// </summary>
public class ErrorEventEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ErrorType { get; set; } = string.Empty;
    
    [Required]
    public string ErrorMessage { get; set; } = string.Empty;
    
    public string? StackTrace { get; set; }
    
    [MaxLength(256)]
    public string? ApplicationContext { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public Dictionary<string, string>? AdditionalData { get; set; }
    
    public int Severity { get; set; } // 0=Info, 1=Warning, 2=Error, 3=Critical
}