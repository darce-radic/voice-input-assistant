using System;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for text insertion functionality
/// </summary>
public interface ITextInsertionService
{
    /// <summary>
    /// Event fired when text is successfully inserted
    /// </summary>
    event EventHandler<TextInsertedEventArgs> TextInserted;
    
    /// <summary>
    /// Event fired when text insertion fails
    /// </summary>
    event EventHandler<TextInsertionErrorEventArgs> TextInsertionFailed;
    
    /// <summary>
    /// Insert text at the current cursor position
    /// </summary>
    /// <param name="text">Text to insert</param>
    /// <param name="method">Insertion method to use</param>
    /// <returns>True if text was successfully inserted</returns>
    Task<bool> InsertTextAsync(string text, TextInsertionMethod method = TextInsertionMethod.Automatic);
    
    /// <summary>
    /// Test if text insertion is supported for the current active window
    /// </summary>
    /// <param name="method">Method to test</param>
    /// <returns>Test result with details</returns>
    Task<TextInsertionTestResult> TestTextInsertionAsync(TextInsertionMethod method = TextInsertionMethod.Automatic);
    
    /// <summary>
    /// Get the recommended text insertion method for the current active window
    /// </summary>
    /// <returns>Recommended insertion method</returns>
    TextInsertionMethod GetRecommendedMethod();
}

/// <summary>
/// Event arguments for successful text insertion
/// </summary>
public class TextInsertedEventArgs : EventArgs
{
    public string Text { get; set; } = string.Empty;
    public TextInsertionMethod Method { get; set; }
    public string TargetApplication { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for text insertion errors
/// </summary>
public class TextInsertionErrorEventArgs : EventArgs
{
    public string Text { get; set; } = string.Empty;
    public TextInsertionMethod Method { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}