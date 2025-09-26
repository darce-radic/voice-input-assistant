namespace VoiceInputAssistant.Core.Models;

public class TextProcessingRule
{
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public TextProcessingRuleType RuleType { get; set; } = TextProcessingRuleType.Replacement;
    
    public string Pattern { get; set; } = string.Empty;
    
    public string Replacement { get; set; } = string.Empty;
    
    public bool IsRegex { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public int Priority { get; set; }
    
    public string? Description { get; set; }
    
    public string[]? Applications { get; set; }
}