using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Interfaces;
using VoiceInputAssistant.WebApi.Models;

namespace VoiceInputAssistant.WebApi.Controllers;

/// <summary>
/// Controller for speech transcription and adaptive learning
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpeechController : ControllerBase
{
    private readonly ILogger<SpeechController> _logger;
    private readonly ISpeechRecognitionService _speechService;
    private readonly IAdaptiveLearningService _adaptiveLearningService;

    public SpeechController(
        ILogger<SpeechController> logger,
        ISpeechRecognitionService speechService,
        IAdaptiveLearningService adaptiveLearningService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _adaptiveLearningService = adaptiveLearningService ?? throw new ArgumentNullException(nameof(adaptiveLearningService));
    }

    /// <summary>
    /// Transcribe audio data to text
    /// </summary>
    /// <param name="request">Transcription request with audio data</param>
    /// <returns>Transcription result</returns>
    [HttpPost("transcribe")]
    public async Task<ActionResult<TranscriptionResponse>> TranscribeAudio([FromBody] TranscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AudioData))
            {
                return BadRequest(new { error = "Audio data is required" });
            }

            // Initialize speech service if needed
            if (!_speechService.IsInitialized)
            {
                await _speechService.InitializeAsync();
            }

            // Convert base64 audio data to bytes
            byte[] audioBytes;
            try
            {
                audioBytes = Convert.FromBase64String(request.AudioData);
            }
            catch (FormatException)
            {
                return BadRequest(new { error = "Invalid base64 audio data" });
            }

            // Perform transcription
            var result = await _speechService.TranscribeAsync(audioBytes);
            var sessionId = Guid.NewGuid().ToString();

            // Store transcription session for potential corrections
            if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
            {
                try
                {
                    // Apply adaptive learning improvements if available
                    var userId = GetCurrentUserId();
                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        var suggestions = await _adaptiveLearningService.GetContextSuggestionsAsync(
                            result.Text, 
                            request.ApplicationContext ?? "general", 
                            userId);

                        if (suggestions?.Any() == true)
                        {
                            // For now, just log suggestions - in a full implementation,
                            // you might want to apply high-confidence suggestions automatically
                            _logger.LogInformation("Found {Count} correction suggestions for user {UserId}",
                                suggestions.Count(), userId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get adaptive learning suggestions");
                    // Don't fail the transcription if adaptive learning fails
                }
            }

            return Ok(new TranscriptionResponse
            {
                Text = result.Text,
                Confidence = result.Confidence,
                Language = result.Language,
                Engine = result.Engine.ToString(),
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                Timestamp = result.Timestamp,
                SessionId = sessionId,
                WordCount = result.WordCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transcription");
            return StatusCode(500, new { error = "Transcription failed" });
        }
    }

    /// <summary>
    /// Submit correction for a transcription to improve adaptive learning
    /// </summary>
    /// <param name="request">Correction request</param>
    /// <returns>Correction processing result</returns>
    [HttpPost("correct")]
    public async Task<ActionResult<TranscriptionCorrectionResponse>> SubmitCorrection([FromBody] TranscriptionCorrectionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OriginalText) || 
                string.IsNullOrWhiteSpace(request.CorrectedText))
            {
                return BadRequest(new { error = "Both original and corrected text are required" });
            }

            var userId = GetCurrentUserId() ?? request.UserId ?? "anonymous";
            var learningUpdates = new List<string>();

            // Create transcription correction using the Interfaces version
            var correction = new VoiceInputAssistant.Core.Interfaces.TranscriptionCorrection
            {
                OriginalText = request.OriginalText,
                CorrectedText = request.CorrectedText,
                ApplicationContext = request.ApplicationContext ?? "general",
                Timestamp = DateTimeOffset.UtcNow.DateTime,
                ConfidenceScore = 1.0 // User corrections have high confidence
            };

            // Submit to adaptive learning service
            await _adaptiveLearningService.ProcessTranscriptionCorrectionAsync(correction);
            learningUpdates.Add("Transcription correction processed");

            // Update speech profile if needed
            try
            {
                var profile = new SpeechProfile
                {
                    UserId = userId,
                    ImprovementRate = 0.95f, // Will be calculated by the service
                    LastModelUpdate = DateTimeOffset.UtcNow.DateTime,
                    CreatedAt = DateTimeOffset.UtcNow.DateTime
                };

                await _adaptiveLearningService.UpdateSpeechProfileAsync(profile);
                learningUpdates.Add("Speech profile updated");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update speech profile for user {UserId}", userId);
                // Don't fail the correction if profile update fails
            }

            return Ok(new TranscriptionCorrectionResponse
            {
                Success = true,
                Message = "Correction processed successfully",
                LearningUpdates = learningUpdates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription correction");
            return StatusCode(500, new TranscriptionCorrectionResponse
            {
                Success = false,
                Message = "Failed to process correction"
            });
        }
    }

    /// <summary>
    /// Submit feedback on transcription quality
    /// </summary>
    /// <param name="request">Feedback request</param>
    /// <returns>Feedback processing result</returns>
    [HttpPost("feedback")]
    public async Task<ActionResult<TranscriptionFeedbackResponse>> SubmitFeedback([FromBody] TranscriptionFeedbackRequest request)
    {
        try
        {
            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { error = "Rating must be between 1 and 5" });
            }

            var userId = GetCurrentUserId() ?? request.UserId ?? "anonymous";

            // Create user feedback
            var feedback = new UserFeedback
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = request.SessionId,
                Rating = request.Rating,
                Comment = request.Comment,
                Timestamp = DateTimeOffset.UtcNow,
                Type = FeedbackType.TranscriptionQuality
            };

            // Submit to adaptive learning service
            await _adaptiveLearningService.ProcessUserFeedbackAsync(feedback);

            return Ok(new TranscriptionFeedbackResponse
            {
                Success = true,
                Message = "Feedback recorded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription feedback");
            return StatusCode(500, new TranscriptionFeedbackResponse
            {
                Success = false,
                Message = "Failed to process feedback"
            });
        }
    }

    /// <summary>
    /// Get speech engine status
    /// </summary>
    /// <returns>Engine status information</returns>
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetSpeechStatus()
    {
        try
        {
            var status = await _speechService.GetStatusAsync();

            return Ok(new
            {
                IsAvailable = status.IsAvailable,
                Engine = status.Engine.ToString(),
                EngineVersion = status.EngineVersion,
                RequiresNetwork = status.RequiresNetwork,
                SupportedLanguages = status.SupportedLanguages,
                SupportsInterimResults = status.SupportsInterimResults,
                SupportsSpeakerDiarization = status.SupportsSpeakerDiarization,
                StatusMessage = status.StatusMessage,
                LastChecked = status.LastChecked,
                IsListening = _speechService.IsListening,
                IsInitialized = _speechService.IsInitialized
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting speech status");
            return StatusCode(500, new { error = "Failed to get speech status" });
        }
    }

    /// <summary>
    /// Get adaptive learning statistics for the current user
    /// </summary>
    /// <returns>Learning statistics</returns>
    [HttpGet("learning/stats")]
    public async Task<ActionResult<object>> GetLearningStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { error = "User identification required" });
            }

            // Get learning statistics
            var stats = await _adaptiveLearningService.GetLearningStatsAsync(userId);

            return Ok(new
            {
                UserId = userId,
                TotalCorrections = stats.TotalCorrections,
                AccuracyImprovement = stats.AccuracyImprovement,
                LastUpdateTimestamp = stats.LastUpdateTimestamp,
                CommonMistakePatterns = stats.CommonMistakePatterns,
                VocabularySize = stats.VocabularySize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting learning statistics");
            return StatusCode(500, new { error = "Failed to get learning statistics" });
        }
    }

    private string? GetCurrentUserId()
    {
        return User?.FindFirst("userId")?.Value ?? User?.FindFirst("sub")?.Value;
    }
}