using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for AI-powered text post-processing and enhancement
    /// </summary>
    public interface IAiPostProcessingService
    {
        /// <summary>
        /// Event raised when post-processing is completed
        /// </summary>
        event EventHandler<PostProcessingCompletedEventArgs> ProcessingCompleted;

        /// <summary>
        /// Gets whether the service is initialized and ready
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets whether processing is currently in progress
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Initializes the AI post-processing service
        /// </summary>
        /// <param name="config">Configuration for the service</param>
        Task InitializeAsync(AiPostProcessingConfig config);

        /// <summary>
        /// Shuts down the service and releases resources
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Processes text through the full AI enhancement pipeline
        /// </summary>
        /// <param name="request">Processing request with text and options</param>
        /// <returns>Processed text with improvements</returns>
        Task<PostProcessingResult> ProcessTextAsync(PostProcessingRequest request);

        /// <summary>
        /// Corrects grammar and spelling errors in text
        /// </summary>
        /// <param name="text">Text to correct</param>
        /// <param name="options">Correction options</param>
        /// <returns>Corrected text</returns>
        Task<string> CorrectGrammarAndSpellingAsync(string text, GrammarCorrectionOptions options = null);

        /// <summary>
        /// Adjusts the tone and style of text
        /// </summary>
        /// <param name="text">Text to adjust</param>
        /// <param name="targetTone">Desired tone (formal, casual, professional, etc.)</param>
        /// <param name="options">Additional tone adjustment options</param>
        /// <returns>Tone-adjusted text</returns>
        Task<string> AdjustToneAsync(string text, TextTone targetTone, ToneAdjustmentOptions options = null);

        /// <summary>
        /// Formats text according to specified formatting rules
        /// </summary>
        /// <param name="text">Text to format</param>
        /// <param name="formatType">Type of formatting to apply</param>
        /// <param name="options">Formatting options</param>
        /// <returns>Formatted text</returns>
        Task<string> FormatTextAsync(string text, TextFormatType formatType, TextFormattingOptions options = null);

        /// <summary>
        /// Expands abbreviations and acronyms in text
        /// </summary>
        /// <param name="text">Text containing abbreviations</param>
        /// <param name="context">Context to help with expansion</param>
        /// <returns>Text with expanded abbreviations</returns>
        Task<string> ExpandAbbreviationsAsync(string text, string context = null);

        /// <summary>
        /// Adds appropriate punctuation to text
        /// </summary>
        /// <param name="text">Text needing punctuation</param>
        /// <param name="options">Punctuation options</param>
        /// <returns>Text with added punctuation</returns>
        Task<string> AddPunctuationAsync(string text, PunctuationOptions options = null);

        /// <summary>
        /// Capitalizes text according to standard rules
        /// </summary>
        /// <param name="text">Text to capitalize</param>
        /// <param name="style">Capitalization style</param>
        /// <returns>Properly capitalized text</returns>
        Task<string> CapitalizeTextAsync(string text, CapitalizationStyle style = CapitalizationStyle.Sentence);

        /// <summary>
        /// Translates text to another language
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="targetLanguage">Target language code</param>
        /// <param name="sourceLanguage">Source language code (null for auto-detect)</param>
        /// <returns>Translated text</returns>
        Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = null);

        /// <summary>
        /// Summarizes long text into key points
        /// </summary>
        /// <param name="text">Text to summarize</param>
        /// <param name="maxLength">Maximum length of summary</param>
        /// <param name="style">Summarization style</param>
        /// <returns>Summarized text</returns>
        Task<string> SummarizeTextAsync(string text, int maxLength, SummarizationStyle style = SummarizationStyle.Bullets);

        /// <summary>
        /// Applies custom processing rules to text
        /// </summary>
        /// <param name="text">Text to process</param>
        /// <param name="rules">Custom processing rules</param>
        /// <returns>Processed text</returns>
        Task<string> ApplyCustomRulesAsync(string text, IEnumerable<TextProcessingRule> rules);

        /// <summary>
        /// Gets available AI models for different processing tasks
        /// </summary>
        /// <returns>List of available models</returns>
        Task<IEnumerable<AiModel>> GetAvailableModelsAsync();

        /// <summary>
        /// Sets the AI model to use for processing
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="taskType">Type of task the model will be used for</param>
        Task SetModelAsync(string modelId, AiTaskType taskType);

        /// <summary>
        /// Gets usage statistics for the AI service
        /// </summary>
        /// <returns>Usage statistics</returns>
        Task<AiUsageStats> GetUsageStatsAsync();
    }

    /// <summary>
    /// Event arguments for post-processing completion
    /// </summary>
    public class PostProcessingCompletedEventArgs : EventArgs
    {
        public PostProcessingResult Result { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// Configuration for AI post-processing service
    /// </summary>
    public class AiPostProcessingConfig
    {
        /// <summary>
        /// OpenAI API key
        /// </summary>
        public string OpenAiApiKey { get; set; }

        /// <summary>
        /// Azure OpenAI endpoint
        /// </summary>
        public string AzureEndpoint { get; set; }

        /// <summary>
        /// Azure OpenAI API key
        /// </summary>
        public string AzureApiKey { get; set; }

        /// <summary>
        /// Default model to use for text processing
        /// </summary>
        public string DefaultModel { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// Maximum tokens per request
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// Temperature for AI responses (0.0 to 1.0)
        /// </summary>
        public float Temperature { get; set; } = 0.3f;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Enable caching of processed results
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Maximum cache size in MB
        /// </summary>
        public int MaxCacheSizeMb { get; set; } = 100;

        /// <summary>
        /// Enable usage tracking
        /// </summary>
        public bool EnableUsageTracking { get; set; } = true;
    }

    /// <summary>
    /// Request for text post-processing
    /// </summary>
    public class PostProcessingRequest
    {
        /// <summary>
        /// Text to process
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Context information to help with processing
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Application context (helps with domain-specific processing)
        /// </summary>
        public string ApplicationContext { get; set; }

        /// <summary>
        /// Processing options to apply
        /// </summary>
        public PostProcessingOptions Options { get; set; } = new PostProcessingOptions();

        /// <summary>
        /// Custom processing rules to apply
        /// </summary>
        public IEnumerable<TextProcessingRule> CustomRules { get; set; }

        /// <summary>
        /// Priority of the request (affects processing order)
        /// </summary>
        public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;
    }

    /// <summary>
    /// Options for text post-processing
    /// </summary>
    public class PostProcessingOptions
    {
        /// <summary>
        /// Enable grammar and spelling correction
        /// </summary>
        public bool CorrectGrammarAndSpelling { get; set; } = true;

        /// <summary>
        /// Target tone for the text
        /// </summary>
        public TextTone TargetTone { get; set; } = TextTone.Natural;

        /// <summary>
        /// Formatting type to apply
        /// </summary>
        public TextFormatType FormatType { get; set; } = TextFormatType.None;

        /// <summary>
        /// Enable abbreviation expansion
        /// </summary>
        public bool ExpandAbbreviations { get; set; } = false;

        /// <summary>
        /// Enable automatic punctuation
        /// </summary>
        public bool AddPunctuation { get; set; } = true;

        /// <summary>
        /// Capitalization style
        /// </summary>
        public CapitalizationStyle CapitalizationStyle { get; set; } = CapitalizationStyle.Sentence;

        /// <summary>
        /// Target language for translation (null for no translation)
        /// </summary>
        public string TranslateToLanguage { get; set; }

        /// <summary>
        /// Enable text summarization for long text
        /// </summary>
        public bool SummarizeLongText { get; set; } = false;

        /// <summary>
        /// Maximum length for summarization
        /// </summary>
        public int SummaryMaxLength { get; set; } = 200;
    }

    /// <summary>
    /// Result of post-processing operation
    /// </summary>
    public class PostProcessingResult
    {
        /// <summary>
        /// Original text before processing
        /// </summary>
        public string OriginalText { get; set; }

        /// <summary>
        /// Processed text
        /// </summary>
        public string ProcessedText { get; set; }

        /// <summary>
        /// List of changes made during processing
        /// </summary>
        public IEnumerable<TextChange> Changes { get; set; } = new List<TextChange>();

        /// <summary>
        /// Processing statistics
        /// </summary>
        public ProcessingStats Stats { get; set; }

        /// <summary>
        /// Confidence score for the processing quality (0.0 to 1.0)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Whether the processing was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a change made to text during processing
    /// </summary>
    public class TextChange
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string OriginalText { get; set; }
        public string NewText { get; set; }
        public ChangeType Type { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Processing statistics
    /// </summary>
    public class ProcessingStats
    {
        public TimeSpan ProcessingTime { get; set; }
        public int TokensUsed { get; set; }
        public int ChangesCount { get; set; }
        public string ModelUsed { get; set; }
        public bool FromCache { get; set; }
    }

    /// <summary>
    /// AI model information
    /// </summary>
    public class AiModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public AiTaskType[] SupportedTasks { get; set; }
        public int MaxTokens { get; set; }
        public bool IsAvailable { get; set; }
        public decimal CostPerToken { get; set; }
    }

    /// <summary>
    /// AI usage statistics
    /// </summary>
    public class AiUsageStats
    {
        public int RequestsToday { get; set; }
        public int RequestsThisMonth { get; set; }
        public long TokensUsedToday { get; set; }
        public long TokensUsedThisMonth { get; set; }
        public decimal CostToday { get; set; }
        public decimal CostThisMonth { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public float AverageConfidenceScore { get; set; }
    }

    /// <summary>
    /// Text tone options
    /// </summary>
    public enum TextTone
    {
        Natural,
        Formal,
        Casual,
        Professional,
        Friendly,
        Authoritative,
        Empathetic,
        Humorous,
        Academic,
        Technical
    }

    /// <summary>
    /// Text formatting types
    /// </summary>
    public enum TextFormatType
    {
        None,
        Email,
        Letter,
        Report,
        Code,
        List,
        Paragraph,
        Headline,
        Social
    }

    /// <summary>
    /// Capitalization styles
    /// </summary>
    public enum CapitalizationStyle
    {
        None,
        Sentence,
        Title,
        AllCaps,
        AllLower,
        CamelCase,
        PascalCase
    }

    /// <summary>
    /// Summarization styles
    /// </summary>
    public enum SummarizationStyle
    {
        Paragraph,
        Bullets,
        Numbered,
        KeyPoints,
        Abstract
    }

    /// <summary>
    /// AI task types
    /// </summary>
    public enum AiTaskType
    {
        GrammarCorrection,
        ToneAdjustment,
        TextFormatting,
        Translation,
        Summarization,
        General
    }

    /// <summary>
    /// Types of text changes
    /// </summary>
    public enum ChangeType
    {
        Grammar,
        Spelling,
        Punctuation,
        Capitalization,
        Tone,
        Format,
        Abbreviation,
        Translation,
        Summarization,
        Custom
    }

    /// <summary>
    /// Processing priority levels
    /// </summary>
    public enum ProcessingPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Grammar correction options
    /// </summary>
    public class GrammarCorrectionOptions
    {
        public bool FixSpelling { get; set; } = true;
        public bool FixGrammar { get; set; } = true;
        public bool FixPunctuation { get; set; } = true;
        public bool PreserveStyle { get; set; } = true;
    }

    /// <summary>
    /// Tone adjustment options
    /// </summary>
    public class ToneAdjustmentOptions
    {
        public bool PreserveMeaning { get; set; } = true;
        public bool PreserveLength { get; set; } = false;
        public float IntensityLevel { get; set; } = 0.5f;
    }

    /// <summary>
    /// Text formatting options
    /// </summary>
    public class TextFormattingOptions
    {
        public bool AddHeaders { get; set; } = false;
        public bool AddBulletPoints { get; set; } = false;
        public bool AddNumbering { get; set; } = false;
        public int MaxLineLength { get; set; } = 80;
    }

    /// <summary>
    /// Punctuation options
    /// </summary>
    public class PunctuationOptions
    {
        public bool AddPeriods { get; set; } = true;
        public bool AddCommas { get; set; } = true;
        public bool AddQuestionMarks { get; set; } = true;
        public bool AddExclamationMarks { get; set; } = true;
    }
}