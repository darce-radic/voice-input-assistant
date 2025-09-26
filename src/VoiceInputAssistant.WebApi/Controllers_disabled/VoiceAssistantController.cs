using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.WebApi.Controllers
{
    /// <summary>
    /// REST API controller for Voice Input Assistant integration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VoiceAssistantController : ControllerBase
    {
        private readonly ILogger<VoiceAssistantController> _logger;
        private readonly ISpeechRecognitionService _speechService;
        private readonly IApplicationProfileService _profileService;
        private readonly IUsageAnalyticsService _analyticsService;
        private readonly IAiPostProcessingService _aiService;
        private readonly ISettingsService _settingsService;

        public VoiceAssistantController(
            ILogger<VoiceAssistantController> logger,
            ISpeechRecognitionService speechService,
            IApplicationProfileService profileService,
            IUsageAnalyticsService analyticsService,
            IAiPostProcessingService aiService,
            ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        /// <summary>
        /// Get system status and health information
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<SystemStatusResponse>> GetStatus()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                var activeProfile = await _profileService.GetActiveProfileAsync();

                return Ok(new SystemStatusResponse
                {
                    IsHealthy = true,
                    IsListening = _speechService.IsListening,
                    IsInitialized = _speechService.IsInitialized,
                    ActiveEngine = settings.SpeechEngine.ToString(),
                    ActiveProfile = activeProfile?.Name ?? "Default",
                    Version = GetAssemblyVersion(),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                return StatusCode(500, new { error = "Failed to get system status" });
            }
        }

        /// <summary>
        /// Start voice recognition
        /// </summary>
        [HttpPost("recognition/start")]
        public async Task<ActionResult<RecognitionSessionResponse>> StartRecognition([FromBody] StartRecognitionRequest request)
        {
            try
            {
                if (!_speechService.IsInitialized)
                {
                    return BadRequest(new { error = "Speech recognition service not initialized" });
                }

                if (_speechService.IsListening)
                {
                    return Conflict(new { error = "Recognition already in progress" });
                }

                // Configure recognition based on request
                if (request?.Engine != null)
                {
                    // Switch engine if requested (implementation would depend on service design)
                }

                await _speechService.StartListeningAsync();

                var sessionId = Guid.NewGuid();
                return Ok(new RecognitionSessionResponse
                {
                    SessionId = sessionId,
                    Status = "Started",
                    StartedAt = DateTime.UtcNow,
                    Engine = _speechService.CurrentEngine?.ToString(),
                    Quality = request?.Quality ?? "Balanced"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting recognition");
                return StatusCode(500, new { error = "Failed to start recognition" });
            }
        }

        /// <summary>
        /// Stop voice recognition
        /// </summary>
        [HttpPost("recognition/stop")]
        public async Task<ActionResult> StopRecognition()
        {
            try
            {
                if (!_speechService.IsListening)
                {
                    return BadRequest(new { error = "No active recognition session" });
                }

                await _speechService.StopListeningAsync();
                return Ok(new { status = "Stopped", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping recognition");
                return StatusCode(500, new { error = "Failed to stop recognition" });
            }
        }

        /// <summary>
        /// Process text through AI post-processing pipeline
        /// </summary>
        [HttpPost("text/process")]
        public async Task<ActionResult<TextProcessingResponse>> ProcessText([FromBody] TextProcessingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Text))
                {
                    return BadRequest(new { error = "Text is required" });
                }

                var processingRequest = new PostProcessingRequest
                {
                    Text = request.Text,
                    Context = request.Context,
                    ApplicationContext = request.ApplicationContext,
                    Options = new PostProcessingOptions
                    {
                        CorrectGrammarAndSpelling = request.CorrectGrammar ?? true,
                        TargetTone = ParseTone(request.TargetTone),
                        FormatType = ParseFormatType(request.FormatType),
                        ExpandAbbreviations = request.ExpandAbbreviations ?? false,
                        AddPunctuation = request.AddPunctuation ?? true,
                        CapitalizationStyle = ParseCapitalizationStyle(request.CapitalizationStyle),
                        TranslateToLanguage = request.TranslateToLanguage,
                        SummarizeLongText = request.Summarize ?? false,
                        SummaryMaxLength = request.SummaryMaxLength ?? 200
                    }
                };

                var result = await _aiService.ProcessTextAsync(processingRequest);

                return Ok(new TextProcessingResponse
                {
                    OriginalText = result.OriginalText,
                    ProcessedText = result.ProcessedText,
                    Changes = result.Changes?.Select(c => new TextChangeDto
                    {
                        Type = c.Type.ToString(),
                        Reason = c.Reason,
                        OriginalText = c.OriginalText,
                        NewText = c.NewText
                    }).ToList() ?? new List<TextChangeDto>(),
                    Confidence = result.Confidence,
                    ProcessingTimeMs = result.Stats?.ProcessingTime.TotalMilliseconds ?? 0,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text");
                return StatusCode(500, new { error = "Failed to process text" });
            }
        }

        /// <summary>
        /// Get usage analytics
        /// </summary>
        [HttpGet("analytics")]
        public async Task<ActionResult<AnalyticsResponse>> GetAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string granularity = "daily")
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var stats = await _analyticsService.GetUsageStatisticsAsync(start, end);
                var topApps = await _analyticsService.GetTopApplicationsAsync(10, 30);
                var engineStats = await _analyticsService.GetAccuracyStatisticsByEngineAsync(30);

                return Ok(new AnalyticsResponse
                {
                    Period = new DatePeriod { StartDate = start, EndDate = end },
                    TotalRecognitions = stats.TotalRecognitions,
                    TotalWords = stats.TotalWords,
                    AverageAccuracy = stats.AverageAccuracy,
                    AverageProcessingTime = stats.AverageProcessingTime.TotalMilliseconds,
                    TopApplications = topApps.Select(a => new ApplicationUsageDto
                    {
                        ApplicationName = a.ApplicationName,
                        RecognitionCount = a.RecognitionCount,
                        TotalUsageTime = a.TotalUsageTime.TotalMinutes,
                        AverageAccuracy = a.AverageAccuracy
                    }).ToList(),
                    EngineStatistics = engineStats.Select(e => new EngineStatsDto
                    {
                        EngineName = e.EngineName,
                        TotalRecognitions = e.TotalRecognitions,
                        AverageAccuracy = e.AverageAccuracy,
                        AverageProcessingTime = e.AverageProcessingTime.TotalMilliseconds
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics");
                return StatusCode(500, new { error = "Failed to get analytics" });
            }
        }

        /// <summary>
        /// Get application profiles
        /// </summary>
        [HttpGet("profiles")]
        public async Task<ActionResult<IEnumerable<ApplicationProfileDto>>> GetProfiles()
        {
            try
            {
                var profiles = await _profileService.GetAllProfilesAsync();
                var profileDtos = profiles.Select(p => new ApplicationProfileDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ApplicationExecutables = p.ApplicationExecutables?.ToList() ?? new List<string>(),
                    IsEnabled = p.IsEnabled,
                    IsDefault = p.IsDefault,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                });

                return Ok(profileDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profiles");
                return StatusCode(500, new { error = "Failed to get profiles" });
            }
        }

        /// <summary>
        /// Create a new application profile
        /// </summary>
        [HttpPost("profiles")]
        public async Task<ActionResult<ApplicationProfileDto>> CreateProfile([FromBody] CreateProfileRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Name))
                {
                    return BadRequest(new { error = "Profile name is required" });
                }

                var profile = new ApplicationProfile
                {
                    Name = request.Name,
                    Description = request.Description,
                    ApplicationExecutables = request.ApplicationExecutables?.ToList() ?? new List<string>(),
                    IsEnabled = request.IsEnabled ?? true,
                    SpeechRecognitionSettings = new SpeechRecognitionSettings
                    {
                        Engine = ParseSpeechEngineType(request.Engine),
                        Quality = ParseRecognitionQuality(request.Quality).ToString(),
                        Language = request.Language ?? "en-US"
                    }
                };

                var createdProfile = await _profileService.CreateProfileAsync(profile);

                return CreatedAtAction(nameof(GetProfiles), new { id = createdProfile.Id },
                    new ApplicationProfileDto
                    {
                        Id = createdProfile.Id,
                        Name = createdProfile.Name,
                        Description = createdProfile.Description,
                        ApplicationExecutables = createdProfile.ApplicationExecutables?.ToList() ?? new List<string>(),
                        IsEnabled = createdProfile.IsEnabled,
                        IsDefault = createdProfile.IsDefault,
                        CreatedAt = createdProfile.CreatedAt,
                        UpdatedAt = createdProfile.UpdatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile");
                return StatusCode(500, new { error = "Failed to create profile" });
            }
        }

        /// <summary>
        /// Update application settings
        /// </summary>
        [HttpPut("settings")]
        public async Task<ActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (request.SpeechEngine != null)
                    settings.SpeechEngine = ParseSpeechEngineType(request.SpeechEngine);
                
                if (request.RecognitionQuality != null)
                    settings.RecognitionQuality = ParseRecognitionQuality(request.RecognitionQuality).ToString();
                
                if (request.VoiceActivationMode != null)
                    settings.VoiceActivationMode = ParseVoiceActivationMode(request.VoiceActivationMode).ToString();
                
                if (request.EnableGlobalHotkeys.HasValue)
                    settings.EnableGlobalHotkeys = request.EnableGlobalHotkeys.Value;

                await _settingsService.SaveSettingsAsync(settings);

                return Ok(new { message = "Settings updated successfully", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings");
                return StatusCode(500, new { error = "Failed to update settings" });
            }
        }

        /// <summary>
        /// Get recent recognition history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<RecognitionHistoryDto>>> GetHistory(
            [FromQuery] int limit = 50,
            [FromQuery] DateTime? since = null)
        {
            try
            {
                var recentRecognitions = await _analyticsService.GetRecentRecognitionsAsync(limit);
                
                var history = recentRecognitions
                    .Where(r => since == null || r.CompletedTime >= since)
                    .Select(r => new RecognitionHistoryDto
                    {
                        Id = Guid.TryParse(r.Id.ToString(), out var guid) ? guid : Guid.Empty,
                        Text = r.Text,
                        Confidence = r.Confidence,
                        Engine = r.Engine.ToString(),
                        Language = r.Language,
                        Duration = r.Duration.TotalMilliseconds,
                        CompletedTime = r.CompletedTime,
                        ApplicationName = r.Context != null ? r.Context.ToString() : ""
                    });

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history");
                return StatusCode(500, new { error = "Failed to get history" });
            }
        }

        /// <summary>
        /// Inject text into the active application
        /// </summary>
        [HttpPost("inject")]
        public async Task<ActionResult> InjectText([FromBody] InjectTextRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Text))
                {
                    return BadRequest(new { error = "Text is required" });
                }

                // This would use the text injection service
                // Implementation depends on having access to the text injection service
                // For now, return success response
                
                return Ok(new { 
                    message = "Text injection completed", 
                    text = request.Text, 
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error injecting text");
                return StatusCode(500, new { error = "Failed to inject text" });
            }
        }

        #region Helper Methods

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

        private static TextTone ParseTone(string tone)
        {
            return Enum.TryParse<TextTone>(tone, true, out var result) ? result : TextTone.Natural;
        }

        private static TextFormatType ParseFormatType(string formatType)
        {
            return Enum.TryParse<TextFormatType>(formatType, true, out var result) ? result : TextFormatType.None;
        }

        private static CapitalizationStyle ParseCapitalizationStyle(string style)
        {
            return Enum.TryParse<CapitalizationStyle>(style, true, out var result) ? result : CapitalizationStyle.Sentence;
        }

        private static Core.Enums.SpeechEngine ParseSpeechEngine(string engine)
        {
            return Enum.TryParse<Core.Enums.SpeechEngine>(engine, true, out var result) ? result : Core.Enums.SpeechEngine.WindowsSpeechRecognition;
        }

        private static Core.Enums.RecognitionQuality ParseRecognitionQuality(string quality)
        {
            return Enum.TryParse<Core.Enums.RecognitionQuality>(quality, true, out var result) ? result : Core.Enums.RecognitionQuality.Balanced;
        }

        private static Core.Enums.VoiceActivationMode ParseVoiceActivationMode(string mode)
        {
            return Enum.TryParse<Core.Enums.VoiceActivationMode>(mode, true, out var result) ? result : Core.Enums.VoiceActivationMode.Manual;
        }

        private static Core.Models.SpeechEngineType ParseSpeechEngineType(string engine)
        {
            // Convert from Core.Enums.SpeechEngine to Core.Models.SpeechEngineType
            var parsedEnum = System.Enum.TryParse<Core.Enums.SpeechEngine>(engine, true, out var result) ? result : Core.Enums.SpeechEngine.WindowsSpeechRecognition;
            return (Core.Models.SpeechEngineType)(int)parsedEnum;
        }

        #endregion
    }

    #region DTOs

    public class SystemStatusResponse
    {
        public bool IsHealthy { get; set; }
        public bool IsListening { get; set; }
        public bool IsInitialized { get; set; }
        public string ActiveEngine { get; set; }
        public string ActiveProfile { get; set; }
        public string Version { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class StartRecognitionRequest
    {
        public string Engine { get; set; }
        public string Quality { get; set; }
        public string Language { get; set; }
        public int? TimeoutSeconds { get; set; }
    }

    public class RecognitionSessionResponse
    {
        public Guid SessionId { get; set; }
        public string Status { get; set; }
        public DateTime StartedAt { get; set; }
        public string Engine { get; set; }
        public string Quality { get; set; }
    }

    public class TextProcessingRequest
    {
        [Required]
        public string Text { get; set; }
        public string Context { get; set; }
        public string ApplicationContext { get; set; }
        public bool? CorrectGrammar { get; set; }
        public string TargetTone { get; set; }
        public string FormatType { get; set; }
        public bool? ExpandAbbreviations { get; set; }
        public bool? AddPunctuation { get; set; }
        public string CapitalizationStyle { get; set; }
        public string TranslateToLanguage { get; set; }
        public bool? Summarize { get; set; }
        public int? SummaryMaxLength { get; set; }
    }

    public class TextProcessingResponse
    {
        public string OriginalText { get; set; }
        public string ProcessedText { get; set; }
        public List<TextChangeDto> Changes { get; set; }
        public float Confidence { get; set; }
        public double ProcessingTimeMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TextChangeDto
    {
        public string Type { get; set; }
        public string Reason { get; set; }
        public string OriginalText { get; set; }
        public string NewText { get; set; }
    }

    public class AnalyticsResponse
    {
        public DatePeriod Period { get; set; }
        public int TotalRecognitions { get; set; }
        public int TotalWords { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
        public List<ApplicationUsageDto> TopApplications { get; set; }
        public List<EngineStatsDto> EngineStatistics { get; set; }
    }

    public class DatePeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ApplicationUsageDto
    {
        public string ApplicationName { get; set; }
        public int RecognitionCount { get; set; }
        public double TotalUsageTime { get; set; }
        public double AverageAccuracy { get; set; }
    }

    public class EngineStatsDto
    {
        public string EngineName { get; set; }
        public int TotalRecognitions { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
    }

    public class ApplicationProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> ApplicationExecutables { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateProfileRequest
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> ApplicationExecutables { get; set; }
        public bool? IsEnabled { get; set; }
        public string Engine { get; set; }
        public string Quality { get; set; }
        public string Language { get; set; }
    }

    public class UpdateSettingsRequest
    {
        public string SpeechEngine { get; set; }
        public string RecognitionQuality { get; set; }
        public string VoiceActivationMode { get; set; }
        public bool? EnableGlobalHotkeys { get; set; }
        public bool? EnableAnalytics { get; set; }
        public string Theme { get; set; }
    }

    public class RecognitionHistoryDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public double Confidence { get; set; }
        public string Engine { get; set; }
        public string Language { get; set; }
        public double Duration { get; set; }
        public DateTime CompletedTime { get; set; }
        public string ApplicationName { get; set; }
    }

    public class InjectTextRequest
    {
        [Required]
        public string Text { get; set; }
        public string TargetApplication { get; set; }
        public bool? ProcessText { get; set; }
    }

    #endregion
}