namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Types of text processing rules
/// </summary>
public enum TextProcessingRuleType
{
    /// <summary>
    /// General text replacement rule
    /// </summary>
    Replacement,
    
    /// <summary>
    /// Simple text replacement
    /// </summary>
    Replace,
    
    /// <summary>
    /// Regular expression-based text replacement
    /// </summary>
    RegexReplace,
    
    /// <summary>
    /// Grammar correction rule
    /// </summary>
    Grammar,
    
    /// <summary>
    /// Punctuation correction rule
    /// </summary>
    Punctuation,
    
    /// <summary>
    /// Capitalization rule
    /// </summary>
    Capitalization,
    
    /// <summary>
    /// Formatting rule (e.g., date, number formatting)
    /// </summary>
    Formatting,
    
    /// <summary>
    /// Application-specific rule
    /// </summary>
    ApplicationSpecific,
    
    /// <summary>
    /// Custom user-defined rule
    /// </summary>
    Custom
}