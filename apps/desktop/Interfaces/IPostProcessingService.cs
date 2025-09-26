using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for text post-processing functionality
/// </summary>
public interface IPostProcessingService
{
    /// <summary>
    /// Event fired when text processing is completed
    /// </summary>
    event EventHandler<TextProcessedEventArgs> TextProcessed;
    
    /// <summary>
    /// Process recognized text based on the specified mode
    /// </summary>
    /// <param name="text">Original recognized text</param>
    /// <param name="mode">Post-processing mode</param>
    /// <param name="context">Optional context information</param>
    /// <returns>Processed text</returns>
    Task<string> ProcessTextAsync(string text, PostProcessingMode mode, string? context = null);
    
    /// <summary>
    /// Apply grammar and punctuation corrections
    /// </summary>
    /// <param name="text">Text to correct</param>
    /// <returns>Corrected text</returns>
    Task<string> ApplyGrammarCorrectionAsync(string text);
    
    /// <summary>
    /// Apply custom text replacement rules
    /// </summary>
    /// <param name="text">Text to process</param>
    /// <param name="rules">Replacement rules</param>
    /// <returns>Processed text</returns>
    Task<string> ApplyCustomRulesAsync(string text, IEnumerable<TextProcessingRule> rules);
    
    /// <summary>
    /// Adjust text tone based on specified style
    /// </summary>
    /// <param name="text">Text to adjust</param>
    /// <param name="toneStyle">Target tone style</param>
    /// <returns>Adjusted text</returns>
    Task<string> AdjustToneAsync(string text, ToneStyle toneStyle);
    
    /// <summary>
    /// Get suggestions for improving recognized text
    /// </summary>
    /// <param name="text">Original text</param>
    /// <returns>List of improvement suggestions</returns>
    Task<IEnumerable<TextSuggestion>> GetSuggestionsAsync(string text);
}

/// <summary>
/// Event arguments for text processing completion
/// </summary>
public class TextProcessedEventArgs : EventArgs
{
    public string OriginalText { get; set; } = string.Empty;
    public string ProcessedText { get; set; } = string.Empty;
    public PostProcessingMode Mode { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Text improvement suggestion
/// </summary>
public class TextSuggestion
{
    public string OriginalText { get; set; } = string.Empty;
    public string SuggestedText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int StartPosition { get; set; }
    public int Length { get; set; }
}