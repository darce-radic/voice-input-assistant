using System.Collections.Generic;

namespace VoiceInputAssistant.Core.Models;

public class SpeechRecognitionResult
{
    public string Text { get; set; } = string.Empty;
    
    public float Confidence { get; set; }
    
    public string Language { get; set; } = string.Empty;
    
    public SpeechEngineType Engine { get; set; }
    
    public bool Success { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public bool IsFinal { get; set; }
    
    public string? ApplicationName { get; set; }
    
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Time when the recognition was completed
    /// </summary>
    public DateTime CompletedTime { get; set; } = DateTime.UtcNow;
    
    public TimeSpan Duration { get; set; }
    
    public int WordCount { get; set; }
    
    /// <summary>
    /// Time taken to process the speech recognition
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
    
    /// <summary>
    /// Whether the recognition was successful (alias for Success property)
    /// </summary>
    public bool IsSuccessful => Success;
    
    /// <summary>
    /// Unique identifier for this result
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Context information about the recognition
    /// </summary>
    public string Context { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional metadata about the recognition
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
