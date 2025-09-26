using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services;

/// <summary>
/// Service for post-processing recognized text
/// </summary>
public class PostProcessingService : IPostProcessingService
{
    private readonly ILogger<PostProcessingService> _logger;

    /// <summary>
    /// Event fired when text processing is completed
    /// </summary>
    public event EventHandler<TextProcessedEventArgs>? TextProcessed;

    public PostProcessingService(ILogger<PostProcessingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ProcessTextAsync(string text, PostProcessingMode mode, string? context = null)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogDebug("Processing text with mode {Mode}: {Text}", mode, text);
            
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string processedText = text;

            switch (mode)
            {
                case PostProcessingMode.None:
                    // No processing
                    break;
                
                case PostProcessingMode.BasicCorrection:
                    processedText = await ApplyGrammarCorrectionAsync(processedText);
                    break;
                
                case PostProcessingMode.Advanced:
                    processedText = await ApplyGrammarCorrectionAsync(processedText);
                    processedText = await AdjustToneAsync(processedText, ToneStyle.Professional);
                    break;
                
                case PostProcessingMode.Custom:
                    // Apply any custom rules if available
                    var customRules = new List<TextProcessingRule>(); // Could be loaded from settings
                    processedText = await ApplyCustomRulesAsync(processedText, customRules);
                    break;
            }

            var endTime = DateTime.UtcNow;
            _logger.LogDebug("Text processing completed: {OriginalLength} -> {ProcessedLength} chars", 
                text.Length, processedText.Length);
            
            // Fire the event
            TextProcessed?.Invoke(this, new TextProcessedEventArgs
            {
                OriginalText = text,
                ProcessedText = processedText,
                Mode = mode,
                ProcessingTime = endTime - startTime
            });
            
            return processedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process text: {Text}", text);
            return text; // Return original on error
        }
    }

    public async Task<string> ApplyGrammarCorrectionAsync(string text)
    {
        try
        {
            await Task.Delay(10); // Simulate processing
            
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var corrected = text.Trim();
            
            // Basic corrections: capitalize first letter, ensure proper sentence ending
            if (corrected.Length > 0)
            {
                corrected = char.ToUpper(corrected[0]) + corrected.Substring(1);
            }
            
            // Ensure proper sentence ending
            if (!corrected.EndsWith(".") && !corrected.EndsWith("!") && !corrected.EndsWith("?"))
            {
                corrected += ".";
            }
            
            return corrected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply grammar correction: {Text}", text);
            return text;
        }
    }

    public async Task<string> ApplyCustomRulesAsync(string text, IEnumerable<TextProcessingRule> rules)
    {
        try
        {
            await Task.Delay(10); // Simulate processing
            
            if (string.IsNullOrWhiteSpace(text) || rules == null)
                return text;

            var processedText = text;
            
            foreach (var rule in rules.Where(r => r.IsEnabled).OrderBy(r => r.Order))
            {
                try
                {
                    if (rule.IsRegex)
                    {
                        var regex = new System.Text.RegularExpressions.Regex(
                            rule.Pattern, 
                            rule.IsCaseSensitive ? 
                                System.Text.RegularExpressions.RegexOptions.None : 
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        processedText = regex.Replace(processedText, rule.Replacement);
                    }
                    else
                    {
                        var comparison = rule.IsCaseSensitive ? 
                            StringComparison.Ordinal : 
                            StringComparison.OrdinalIgnoreCase;
                        processedText = processedText.Replace(rule.Pattern, rule.Replacement, comparison);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply rule {RuleName}: {Pattern}", rule.Name, rule.Pattern);
                }
            }
            
            return processedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply custom rules: {Text}", text);
            return text;
        }
    }

    public async Task<string> AdjustToneAsync(string text, ToneStyle toneStyle)
    {
        if (toneStyle == ToneStyle.Original)
            return text;

        try
        {
            await Task.Delay(50); // Simulate AI processing
            
            // TODO: Implement tone style adjustments using AI
            // This would typically call an external AI service
            // For now, just return the original text
            
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adjust tone for text: {Text}", text);
            return text;
        }
    }

    public async Task<IEnumerable<TextSuggestion>> GetSuggestionsAsync(string text)
    {
        try
        {
            await Task.Delay(20); // Simulate processing
            
            var suggestions = new List<TextSuggestion>();
            
            // TODO: Implement AI-powered text suggestions
            // For now, return empty suggestions
            
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get suggestions for text: {Text}", text);
            return new List<TextSuggestion>();
        }
    }

}
