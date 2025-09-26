using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.Core.Services
{
    /// <summary>
    /// AI-powered text post-processing service using OpenAI GPT models
    /// </summary>
    public class AiPostProcessingService : IAiPostProcessingService, IDisposable
    {
        private readonly ILogger<AiPostProcessingService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, CachedResult> _cache;
        private readonly SemaphoreSlim _requestSemaphore;
        private readonly System.Threading.Timer _cacheCleanupTimer;
        
        private AiPostProcessingConfig _config;
        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isProcessing;
        private AiUsageStats _usageStats;
        private Dictionary<AiTaskType, string> _modelMappings;

        public event EventHandler<PostProcessingCompletedEventArgs> ProcessingCompleted;

        public bool IsInitialized => _isInitialized;
        public bool IsProcessing => _isProcessing;

        public AiPostProcessingService(ILogger<AiPostProcessingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            
            _cache = new ConcurrentDictionary<string, CachedResult>();
            _requestSemaphore = new SemaphoreSlim(3, 3); // Allow max 3 concurrent requests
            
            // Clean up cache every 5 minutes
            _cacheCleanupTimer = new System.Threading.Timer(CleanupCache, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            _usageStats = new AiUsageStats();
            _modelMappings = new Dictionary<AiTaskType, string>();
            
            _logger.LogDebug("AiPostProcessingService initialized");
        }

        public async Task InitializeAsync(AiPostProcessingConfig config)
        {
            if (_isInitialized)
                return;

            _config = config ?? throw new ArgumentNullException(nameof(config));

            try
            {
                // Set up HTTP client headers
                if (!string.IsNullOrEmpty(_config.OpenAiApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.OpenAiApiKey}");
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "VoiceInputAssistant/1.0");
                }

                // Initialize model mappings
                _modelMappings[AiTaskType.GrammarCorrection] = _config.DefaultModel;
                _modelMappings[AiTaskType.ToneAdjustment] = _config.DefaultModel;
                _modelMappings[AiTaskType.TextFormatting] = _config.DefaultModel;
                _modelMappings[AiTaskType.Translation] = _config.DefaultModel;
                _modelMappings[AiTaskType.Summarization] = _config.DefaultModel;
                _modelMappings[AiTaskType.General] = _config.DefaultModel;

                // Test API connection
                await TestApiConnectionAsync();

                _isInitialized = true;
                _logger.LogInformation("AI Post-processing service initialized with model: {Model}", _config.DefaultModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AI post-processing service");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_isInitialized || _isDisposed)
                return;

            try
            {
                // Wait for any ongoing requests to complete
                while (_isProcessing)
                {
                    await Task.Delay(100);
                }

                _isInitialized = false;
                _logger.LogInformation("AI Post-processing service shut down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI post-processing service shutdown");
            }
        }

        public async Task<PostProcessingResult> ProcessTextAsync(PostProcessingRequest request)
        {
            if (!_isInitialized || request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return new PostProcessingResult
                {
                    OriginalText = request?.Text ?? string.Empty,
                    ProcessedText = request?.Text ?? string.Empty,
                    Success = false,
                    ErrorMessage = "Invalid request or service not initialized"
                };
            }

            var stopwatch = Stopwatch.StartNew();
            var changes = new List<TextChange>();
            string processedText = request.Text;

            try
            {
                await _requestSemaphore.WaitAsync();
                _isProcessing = true;

                // Check cache first
                var cacheKey = GenerateCacheKey(request);
                if (_config.EnableCaching && _cache.TryGetValue(cacheKey, out var cachedResult) && !cachedResult.IsExpired)
                {
                    return cachedResult.Result;
                }

                // Apply processing steps based on options
                if (request.Options.CorrectGrammarAndSpelling)
                {
                    var corrected = await CorrectGrammarAndSpellingAsync(processedText);
                    if (corrected != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = corrected,
                            Type = ChangeType.Grammar,
                            Reason = "Grammar and spelling correction"
                        });
                        processedText = corrected;
                    }
                }

                if (request.Options.AddPunctuation)
                {
                    var punctuated = await AddPunctuationAsync(processedText);
                    if (punctuated != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = punctuated,
                            Type = ChangeType.Punctuation,
                            Reason = "Added punctuation"
                        });
                        processedText = punctuated;
                    }
                }

                if (request.Options.CapitalizationStyle != CapitalizationStyle.None)
                {
                    var capitalized = await CapitalizeTextAsync(processedText, request.Options.CapitalizationStyle);
                    if (capitalized != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = capitalized,
                            Type = ChangeType.Capitalization,
                            Reason = $"Applied {request.Options.CapitalizationStyle} capitalization"
                        });
                        processedText = capitalized;
                    }
                }

                if (request.Options.TargetTone != TextTone.Natural)
                {
                    var toneAdjusted = await AdjustToneAsync(processedText, request.Options.TargetTone);
                    if (toneAdjusted != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = toneAdjusted,
                            Type = ChangeType.Tone,
                            Reason = $"Adjusted tone to {request.Options.TargetTone}"
                        });
                        processedText = toneAdjusted;
                    }
                }

                if (request.Options.FormatType != TextFormatType.None)
                {
                    var formatted = await FormatTextAsync(processedText, request.Options.FormatType);
                    if (formatted != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = formatted,
                            Type = ChangeType.Format,
                            Reason = $"Applied {request.Options.FormatType} formatting"
                        });
                        processedText = formatted;
                    }
                }

                if (request.Options.ExpandAbbreviations)
                {
                    var expanded = await ExpandAbbreviationsAsync(processedText, request.Context);
                    if (expanded != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = expanded,
                            Type = ChangeType.Abbreviation,
                            Reason = "Expanded abbreviations"
                        });
                        processedText = expanded;
                    }
                }

                if (!string.IsNullOrEmpty(request.Options.TranslateToLanguage))
                {
                    var translated = await TranslateTextAsync(processedText, request.Options.TranslateToLanguage);
                    if (translated != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = translated,
                            Type = ChangeType.Translation,
                            Reason = $"Translated to {request.Options.TranslateToLanguage}"
                        });
                        processedText = translated;
                    }
                }

                if (request.Options.SummarizeLongText && processedText.Length > request.Options.SummaryMaxLength * 2)
                {
                    var summarized = await SummarizeTextAsync(processedText, request.Options.SummaryMaxLength);
                    if (summarized != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = summarized,
                            Type = ChangeType.Summarization,
                            Reason = "Summarized long text"
                        });
                        processedText = summarized;
                    }
                }

                // Apply custom rules
                if (request.CustomRules?.Any() == true)
                {
                    var customProcessed = await ApplyCustomRulesAsync(processedText, request.CustomRules);
                    if (customProcessed != processedText)
                    {
                        changes.Add(new TextChange
                        {
                            OriginalText = processedText,
                            NewText = customProcessed,
                            Type = ChangeType.Custom,
                            Reason = "Applied custom processing rules"
                        });
                        processedText = customProcessed;
                    }
                }

                stopwatch.Stop();

                var result = new PostProcessingResult
                {
                    OriginalText = request.Text,
                    ProcessedText = processedText,
                    Changes = changes,
                    Stats = new ProcessingStats
                    {
                        ProcessingTime = stopwatch.Elapsed,
                        TokensUsed = EstimateTokenCount(request.Text + processedText),
                        ChangesCount = changes.Count,
                        ModelUsed = _config.DefaultModel,
                        FromCache = false
                    },
                    Confidence = CalculateConfidence(changes),
                    Success = true
                };

                // Cache the result
                if (_config.EnableCaching)
                {
                    _cache.TryAdd(cacheKey, new CachedResult { Result = result, Timestamp = DateTime.UtcNow });
                }

                // Update usage statistics
                if (_config.EnableUsageTracking)
                {
                    UpdateUsageStats(result.Stats);
                }

                // Raise completion event
                ProcessingCompleted?.Invoke(this, new PostProcessingCompletedEventArgs
                {
                    Result = result,
                    ProcessingTime = stopwatch.Elapsed
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text post-processing");

                var errorResult = new PostProcessingResult
                {
                    OriginalText = request.Text,
                    ProcessedText = processedText,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Stats = new ProcessingStats
                    {
                        ProcessingTime = stopwatch.Elapsed,
                        ModelUsed = _config.DefaultModel
                    }
                };

                ProcessingCompleted?.Invoke(this, new PostProcessingCompletedEventArgs
                {
                    Result = errorResult,
                    ProcessingTime = stopwatch.Elapsed,
                    Error = ex
                });

                return errorResult;
            }
            finally
            {
                _isProcessing = false;
                _requestSemaphore.Release();
            }
        }

        public async Task<string> CorrectGrammarAndSpellingAsync(string text, GrammarCorrectionOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            options ??= new GrammarCorrectionOptions();

            var prompt = BuildGrammarCorrectionPrompt(text, options);
            return await SendOpenAiRequestAsync(prompt, "grammar_correction");
        }

        public async Task<string> AdjustToneAsync(string text, TextTone targetTone, ToneAdjustmentOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            options ??= new ToneAdjustmentOptions();

            var prompt = BuildToneAdjustmentPrompt(text, targetTone, options);
            return await SendOpenAiRequestAsync(prompt, "tone_adjustment");
        }

        public async Task<string> FormatTextAsync(string text, TextFormatType formatType, TextFormattingOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(text) || formatType == TextFormatType.None)
                return text;

            options ??= new TextFormattingOptions();

            var prompt = BuildFormattingPrompt(text, formatType, options);
            return await SendOpenAiRequestAsync(prompt, "formatting");
        }

        public async Task<string> ExpandAbbreviationsAsync(string text, string context = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var prompt = BuildAbbreviationExpansionPrompt(text, context);
            return await SendOpenAiRequestAsync(prompt, "abbreviation_expansion");
        }

        public async Task<string> AddPunctuationAsync(string text, PunctuationOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            options ??= new PunctuationOptions();

            var prompt = BuildPunctuationPrompt(text, options);
            return await SendOpenAiRequestAsync(prompt, "punctuation");
        }

        public async Task<string> CapitalizeTextAsync(string text, CapitalizationStyle style = CapitalizationStyle.Sentence)
        {
            if (string.IsNullOrWhiteSpace(text) || style == CapitalizationStyle.None)
                return text;

            // Handle simple cases without AI
            switch (style)
            {
                case CapitalizationStyle.AllCaps:
                    return text.ToUpperInvariant();
                case CapitalizationStyle.AllLower:
                    return text.ToLowerInvariant();
                case CapitalizationStyle.Sentence:
                    return CapitalizeSentences(text);
                case CapitalizationStyle.Title:
                    return CapitalizeTitleCase(text);
                default:
                    var prompt = BuildCapitalizationPrompt(text, style);
                    return await SendOpenAiRequestAsync(prompt, "capitalization");
            }
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = null)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
                return text;

            var prompt = BuildTranslationPrompt(text, targetLanguage, sourceLanguage);
            return await SendOpenAiRequestAsync(prompt, "translation");
        }

        public async Task<string> SummarizeTextAsync(string text, int maxLength, SummarizationStyle style = SummarizationStyle.Bullets)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
                return text;

            var prompt = BuildSummarizationPrompt(text, maxLength, style);
            return await SendOpenAiRequestAsync(prompt, "summarization");
        }

        public async Task<string> ApplyCustomRulesAsync(string text, IEnumerable<TextProcessingRule> rules)
        {
            if (string.IsNullOrWhiteSpace(text) || rules?.Any() != true)
                return text;

            var processedText = text;
            
            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                try
                {
                    switch (rule.RuleType)
                    {
                        case TextProcessingRuleType.Replace:
                            if (!string.IsNullOrEmpty(rule.Pattern) && rule.Replacement != null)
                            {
                                processedText = processedText.Replace(rule.Pattern, rule.Replacement);
                            }
                            break;
                            
                        case TextProcessingRuleType.RegexReplace:
                            if (!string.IsNullOrEmpty(rule.Pattern) && rule.Replacement != null)
                            {
                                processedText = System.Text.RegularExpressions.Regex.Replace(
                                    processedText, rule.Pattern, rule.Replacement);
                            }
                            break;
                            
                        case TextProcessingRuleType.Custom:
                            // For custom rules, use AI to apply the transformation
                            var prompt = $"Apply this rule to the text: {rule.Description}\n\nText: {processedText}\n\nApply the rule and return only the modified text:";
                            processedText = await SendOpenAiRequestAsync(prompt, "custom_rule");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply custom rule: {RuleName}", rule.Name);
                }
            }

            return processedText;
        }

        public async Task<IEnumerable<AiModel>> GetAvailableModelsAsync()
        {
            // In a real implementation, this would query the OpenAI API for available models
            return new List<AiModel>
            {
                new AiModel { Id = "gpt-3.5-turbo", Name = "GPT-3.5 Turbo", Description = "Fast and efficient for most tasks", SupportedTasks = Enum.GetValues<AiTaskType>(), MaxTokens = 4096, IsAvailable = true, CostPerToken = 0.0000015m },
                new AiModel { Id = "gpt-4", Name = "GPT-4", Description = "Most capable model for complex tasks", SupportedTasks = Enum.GetValues<AiTaskType>(), MaxTokens = 8192, IsAvailable = true, CostPerToken = 0.00003m },
                new AiModel { Id = "gpt-4-turbo", Name = "GPT-4 Turbo", Description = "Latest GPT-4 model with improved performance", SupportedTasks = Enum.GetValues<AiTaskType>(), MaxTokens = 128000, IsAvailable = true, CostPerToken = 0.00001m }
            };
        }

        public async Task SetModelAsync(string modelId, AiTaskType taskType)
        {
            if (string.IsNullOrEmpty(modelId))
                return;

            var models = await GetAvailableModelsAsync();
            var model = models.FirstOrDefault(m => m.Id == modelId && m.SupportedTasks.Contains(taskType));
            
            if (model?.IsAvailable == true)
            {
                _modelMappings[taskType] = modelId;
                _logger.LogDebug("Set model {ModelId} for task type {TaskType}", modelId, taskType);
            }
        }

        public async Task<AiUsageStats> GetUsageStatsAsync()
        {
            return _usageStats;
        }

        private async Task<string> SendOpenAiRequestAsync(string prompt, string operation)
        {
            try
            {
                var requestBody = new
                {
                    model = _config.DefaultModel,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant that improves text. Return only the improved text without any additional commentary or formatting." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature,
                    top_p = 1,
                    frequency_penalty = 0,
                    presence_penalty = 0
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    
                    var choices = responseObj.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var message = choices[0].GetProperty("message");
                        var result = message.GetProperty("content").GetString()?.Trim();
                        
                        _logger.LogDebug("OpenAI {Operation} completed successfully", operation);
                        return result ?? prompt; // Return original if AI returned empty
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("OpenAI API error for {Operation}: {StatusCode} - {Error}", 
                        operation, response.StatusCode, errorContent);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("OpenAI request timeout for {Operation}", operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API for {Operation}", operation);
            }

            return prompt; // Return original text if AI call fails
        }

        private async Task TestApiConnectionAsync()
        {
            try
            {
                var testResult = await SendOpenAiRequestAsync("Test", "connection_test");
                _logger.LogDebug("API connection test successful");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API connection test failed - service will still function with local processing");
            }
        }

        private string BuildGrammarCorrectionPrompt(string text, GrammarCorrectionOptions options)
        {
            var corrections = new List<string>();
            if (options.FixSpelling) corrections.Add("spelling");
            if (options.FixGrammar) corrections.Add("grammar");
            if (options.FixPunctuation) corrections.Add("punctuation");

            return $"Correct the {string.Join(", ", corrections)} in this text{(options.PreserveStyle ? " while preserving the original style" : "")}:\n\n{text}";
        }

        private string BuildToneAdjustmentPrompt(string text, TextTone targetTone, ToneAdjustmentOptions options)
        {
            var preservations = new List<string>();
            if (options.PreserveMeaning) preservations.Add("meaning");
            if (options.PreserveLength) preservations.Add("length");

            var preserveText = preservations.Any() ? $" while preserving the {string.Join(" and ", preservations)}" : "";
            
            return $"Adjust the tone of this text to be {targetTone.ToString().ToLowerInvariant()}{preserveText}:\n\n{text}";
        }

        private string BuildFormattingPrompt(string text, TextFormatType formatType, TextFormattingOptions options)
        {
            return $"Format this text as a {formatType.ToString().ToLowerInvariant()}:\n\n{text}";
        }

        private string BuildAbbreviationExpansionPrompt(string text, string context)
        {
            var contextPart = !string.IsNullOrEmpty(context) ? $"\n\nContext: {context}" : "";
            return $"Expand any abbreviations and acronyms in this text:{contextPart}\n\n{text}";
        }

        private string BuildPunctuationPrompt(string text, PunctuationOptions options)
        {
            return $"Add appropriate punctuation to this text:\n\n{text}";
        }

        private string BuildCapitalizationPrompt(string text, CapitalizationStyle style)
        {
            return $"Apply {style} capitalization to this text:\n\n{text}";
        }

        private string BuildTranslationPrompt(string text, string targetLanguage, string sourceLanguage)
        {
            var sourcePart = !string.IsNullOrEmpty(sourceLanguage) ? $" from {sourceLanguage}" : "";
            return $"Translate this text{sourcePart} to {targetLanguage}:\n\n{text}";
        }

        private string BuildSummarizationPrompt(string text, int maxLength, SummarizationStyle style)
        {
            return $"Summarize this text in {maxLength} characters or less using {style.ToString().ToLowerInvariant()} format:\n\n{text}";
        }

        private static string CapitalizeSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    result.Append(capitalizeNext ? char.ToUpper(c) : char.ToLower(c));
                    capitalizeNext = false;
                }
                else
                {
                    result.Append(c);
                    if (c == '.' || c == '!' || c == '?')
                        capitalizeNext = true;
                }
            }

            return result.ToString();
        }

        private static string CapitalizeTitleCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var titleCased = words.Select(word =>
            {
                if (word.Length == 0)
                    return word;
                
                var lowerArticles = new[] { "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };
                
                if (lowerArticles.Contains(word.ToLowerInvariant()))
                    return word.ToLowerInvariant();
                
                return char.ToUpper(word[0]) + word.Substring(1).ToLowerInvariant();
            });

            var result = string.Join(" ", titleCased);
            
            // Always capitalize first word
            if (result.Length > 0)
                result = char.ToUpper(result[0]) + result.Substring(1);
                
            return result;
        }

        private string GenerateCacheKey(PostProcessingRequest request)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(request.Text.GetHashCode());
            keyBuilder.Append(request.Options.GetHashCode());
            keyBuilder.Append(request.Context?.GetHashCode() ?? 0);
            keyBuilder.Append(request.ApplicationContext?.GetHashCode() ?? 0);
            
            return keyBuilder.ToString();
        }

        private static int EstimateTokenCount(string text)
        {
            // Rough estimation: ~4 characters per token for English text
            return Math.Max(1, text.Length / 4);
        }

        private static float CalculateConfidence(List<TextChange> changes)
        {
            if (!changes.Any())
                return 1.0f;

            // Simple confidence calculation based on number and type of changes
            var baseConfidence = 0.9f;
            var deductionPerChange = 0.1f / Math.Max(1, changes.Count);
            
            return Math.Max(0.1f, baseConfidence - (changes.Count * deductionPerChange));
        }

        private void UpdateUsageStats(ProcessingStats stats)
        {
            var today = DateTime.UtcNow.Date;
            var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // This is a simplified implementation - in production, you'd want to persist this data
            _usageStats.RequestsToday++;
            _usageStats.RequestsThisMonth++;
            _usageStats.TokensUsedToday += stats.TokensUsed;
            _usageStats.TokensUsedThisMonth += stats.TokensUsed;
            
            // Update averages (simplified)
            _usageStats.AverageProcessingTime = TimeSpan.FromMilliseconds(
                (_usageStats.AverageProcessingTime.TotalMilliseconds + stats.ProcessingTime.TotalMilliseconds) / 2);
        }

        private void CleanupCache(object state)
        {
            try
            {
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                // Limit cache size
                if (_cache.Count > 1000) // Arbitrary limit
                {
                    var oldestKeys = _cache
                        .OrderBy(kvp => kvp.Value.Timestamp)
                        .Take(_cache.Count - 1000)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in oldestKeys)
                    {
                        _cache.TryRemove(key, out _);
                    }
                }

                _logger.LogDebug("Cache cleanup completed. Removed {ExpiredCount} expired entries", expiredKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                ShutdownAsync().GetAwaiter().GetResult();
                _cacheCleanupTimer?.Dispose();
                _requestSemaphore?.Dispose();
                _httpClient?.Dispose();
                
                _isDisposed = true;
                _logger?.LogDebug("AiPostProcessingService disposed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing AiPostProcessingService");
            }
        }

        private class CachedResult
        {
            public PostProcessingResult Result { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsExpired => DateTime.UtcNow - Timestamp > TimeSpan.FromHours(24);
        }
    }
}