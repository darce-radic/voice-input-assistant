using System.ComponentModel.DataAnnotations;

namespace VoiceInputAssistant.Infrastructure.Data.Entities;

/// <summary>
/// Database entity for engine performance metrics
/// </summary>
public class EngineMetricsEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EngineName { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    
    public float AverageConfidence { get; set; }
    
    public int AverageProcessingTimeMs { get; set; }
    
    public int TotalTranscriptions { get; set; }
    
    public int SuccessfulTranscriptions { get; set; }
    
    public int FailedTranscriptions { get; set; }
    
    public float ErrorRate { get; set; }
    
    public Dictionary<string, double>? AdditionalMetrics { get; set; }
}