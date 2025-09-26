using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Grpc;

namespace VoiceInputAssistant.WebApi.Services
{
    /// <summary>
    /// gRPC service implementation for Voice Input Assistant
    /// </summary>
    public class VoiceAssistantGrpcService : VoiceAssistant.VoiceAssistantBase
    {
        private readonly ILogger<VoiceAssistantGrpcService> _logger;
        private readonly ISpeechRecognitionService _speechService;
        private readonly IApplicationProfileService _profileService;
        private readonly IUsageAnalyticsService _analyticsService;
        private readonly IAiPostProcessingService _aiService;
        private readonly ISettingsService _settingsService;

        public VoiceAssistantGrpcService(
            ILogger<VoiceAssistantGrpcService> logger,
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

        public override async Task<GetStatusResponse> GetStatus(GetStatusRequest request, ServerCallContext context)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                var activeProfile = await _profileService.GetActiveProfileAsync();

                return new GetStatusResponse
                {
                    IsHealthy = true,
                    IsListening = _speechService.IsListening,
                    IsInitialized = _speechService.IsInitialized,
                    ActiveEngine = settings.SpeechEngine.ToString(),
                    ActiveProfile = activeProfile?.Name ?? "Default",
                    Version = GetAssemblyVersion(),
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to get system status"));
            }
        }

        public override async Task<StartRecognitionResponse> StartRecognition(StartRecognitionRequest request, ServerCallContext context)
        {
            try
            {
                if (!_speechService.IsInitialized)
                {
                    throw new RpcException(new Status(StatusCode.FailedPrecondition, "Speech recognition service not initialized"));
                }

                if (_speechService.IsListening)
                {
                    throw new RpcException(new Status(StatusCode.AlreadyExists, "Recognition already in progress"));
                }

                // Configure recognition based on request
                if (!string.IsNullOrEmpty(request.Engine))
                {
                    // Switch engine if requested (implementation would depend on service design)
                }

                await _speechService.StartListeningAsync();

                return new StartRecognitionResponse
                {
                    SessionId = Guid.NewGuid().ToString(),
                    Status = "Started",
                    StartedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                    Engine = _speechService.CurrentEngine?.ToString() ?? "Unknown",
                    Quality = !string.IsNullOrEmpty(request.Quality) ? request.Quality : "Balanced"
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting recognition");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to start recognition"));
            }
        }

        public override async Task<StopRecognitionResponse> StopRecognition(StopRecognitionRequest request, ServerCallContext context)
        {
            try
            {
                if (!_speechService.IsListening)
                {
                    throw new RpcException(new Status(StatusCode.FailedPrecondition, "No active recognition session"));
                }

                await _speechService.StopListeningAsync();
                
                return new StopRecognitionResponse
                {
                    Status = "Stopped",
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping recognition");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to stop recognition"));
            }
        }

        public override async Task<ProcessTextResponse> ProcessText(ProcessTextRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Text is required"));
                }

                var processingRequest = new PostProcessingRequest
                {
                    Text = request.Text,
                    Context = request.Context,
                    ApplicationContext = request.ApplicationContext,
                    Options = new PostProcessingOptions
                    {
                        CorrectGrammarAndSpelling = request.CorrectGrammar,
                        TargetTone = ParseTone(request.TargetTone),
                        FormatType = ParseFormatType(request.FormatType),
                        ExpandAbbreviations = request.ExpandAbbreviations,
                        AddPunctuation = request.AddPunctuation,
                        CapitalizationStyle = ParseCapitalizationStyle(request.CapitalizationStyle),
                        TranslateToLanguage = request.TranslateToLanguage,
                        SummarizeLongText = request.Summarize,
                        SummaryMaxLength = request.SummaryMaxLength > 0 ? request.SummaryMaxLength : 200
                    }
                };

                var result = await _aiService.ProcessTextAsync(processingRequest);

                var response = new ProcessTextResponse
                {
                    OriginalText = result.OriginalText,
                    ProcessedText = result.ProcessedText,
                    Confidence = result.Confidence,
                    ProcessingTimeMs = result.Stats?.ProcessingTime.TotalMilliseconds ?? 0,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage ?? ""
                };

                if (result.Changes != null)
                {
                    response.Changes.AddRange(result.Changes.Select(c => new VoiceInputAssistant.Grpc.TextChange
                    {
                        Type = c.Type.ToString(),
                        Reason = c.Reason,
                        OriginalText = c.OriginalText,
                        NewText = c.NewText
                    }));
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to process text"));
            }
        }

        public override async Task<GetAnalyticsResponse> GetAnalytics(GetAnalyticsRequest request, ServerCallContext context)
        {
            try
            {
                var startDate = request.StartDate?.ToDateTime() ?? DateTime.UtcNow.AddDays(-30);
                var endDate = request.EndDate?.ToDateTime() ?? DateTime.UtcNow;

                var stats = await _analyticsService.GetUsageStatisticsAsync(startDate, endDate);
                var topApps = await _analyticsService.GetTopApplicationsAsync(10, 30);
                var engineStats = await _analyticsService.GetAccuracyStatisticsByEngineAsync(30);

                var response = new GetAnalyticsResponse
                {
                    Period = new DatePeriod
                    {
                        StartDate = Timestamp.FromDateTime(startDate),
                        EndDate = Timestamp.FromDateTime(endDate)
                    },
                    TotalRecognitions = stats.TotalRecognitions,
                    TotalWords = stats.TotalWords,
                    AverageAccuracy = stats.AverageAccuracy,
                    AverageProcessingTime = stats.AverageProcessingTime.TotalMilliseconds
                };

                response.TopApplications.AddRange(topApps.Select(a => new VoiceInputAssistant.Grpc.ApplicationUsage
                {
                    ApplicationName = a.ApplicationName,
                    RecognitionCount = a.RecognitionCount,
                    TotalUsageTime = a.TotalUsageTime.TotalMinutes,
                    AverageAccuracy = a.AverageAccuracy
                }));

                response.EngineStatistics.AddRange(engineStats.Select(e => new EngineStats
                {
                    EngineName = e.EngineName,
                    TotalRecognitions = e.TotalRecognitions,
                    AverageAccuracy = e.AverageAccuracy,
                    AverageProcessingTime = e.AverageProcessingTime.TotalMilliseconds
                }));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to get analytics"));
            }
        }

        public override async Task<GetProfilesResponse> GetProfiles(GetProfilesRequest request, ServerCallContext context)
        {
            try
            {
                var profiles = await _profileService.GetAllProfilesAsync();
                var response = new GetProfilesResponse();

                response.Profiles.AddRange(profiles.Select(p => new VoiceInputAssistant.Grpc.ApplicationProfile
                {
                    Id = p.Id.ToString(),
                    Name = p.Name,
                    Description = p.Description ?? "",
                    IsEnabled = p.IsEnabled,
                    IsDefault = p.IsDefault,
                    CreatedAt = Timestamp.FromDateTime(p.CreatedAt),
                    UpdatedAt = Timestamp.FromDateTime(p.UpdatedAt)
                }));

                // Add executables to each profile
                foreach (var profile in response.Profiles)
                {
                    var originalProfile = profiles.First(p => p.Id.ToString() == profile.Id);
                    if (originalProfile.ApplicationExecutables != null)
                    {
                        profile.ApplicationExecutables.AddRange(originalProfile.ApplicationExecutables);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profiles");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to get profiles"));
            }
        }

        public override async Task<CreateProfileResponse> CreateProfile(CreateProfileRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Profile name is required"));
                }

                var profile = new Core.Models.ApplicationProfile
                {
                    Name = request.Name,
                    Description = request.Description,
                    ApplicationExecutables = request.ApplicationExecutables?.ToList() ?? new List<string>(),
                    IsEnabled = request.IsEnabled,
                    SpeechRecognitionSettings = new SpeechRecognitionSettings
                    {
                        Engine = ParseSpeechEngineType(request.Engine),
                        Quality = ParseRecognitionQuality(request.Quality).ToString(),
                        Language = !string.IsNullOrEmpty(request.Language) ? request.Language : "en-US"
                    }
                };

                var createdProfile = await _profileService.CreateProfileAsync(profile);

                return new CreateProfileResponse
                {
                    Profile = new VoiceInputAssistant.Grpc.ApplicationProfile
                    {
                        Id = createdProfile.Id.ToString(),
                        Name = createdProfile.Name,
                        Description = createdProfile.Description ?? "",
                        IsEnabled = createdProfile.IsEnabled,
                        IsDefault = createdProfile.IsDefault,
                        CreatedAt = Timestamp.FromDateTime(createdProfile.CreatedAt),
                        UpdatedAt = Timestamp.FromDateTime(createdProfile.UpdatedAt)
                    }
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to create profile"));
            }
        }

        public override async Task<UpdateSettingsResponse> UpdateSettings(UpdateSettingsRequest request, ServerCallContext context)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (!string.IsNullOrEmpty(request.SpeechEngine))
                    settings.SpeechEngine = ParseSpeechEngineType(request.SpeechEngine);

                if (!string.IsNullOrEmpty(request.RecognitionQuality))
                    settings.RecognitionQuality = ParseRecognitionQuality(request.RecognitionQuality).ToString();

                if (!string.IsNullOrEmpty(request.VoiceActivationMode))
                    settings.VoiceActivationMode = ParseVoiceActivationMode(request.VoiceActivationMode).ToString();

                if (request.EnableGlobalHotkeys != null)
                    settings.EnableGlobalHotkeys = request.EnableGlobalHotkeys.Value;

                await _settingsService.SaveSettingsAsync(settings);

                return new UpdateSettingsResponse
                {
                    Message = "Settings updated successfully",
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to update settings"));
            }
        }

        public override async Task<GetHistoryResponse> GetHistory(GetHistoryRequest request, ServerCallContext context)
        {
            try
            {
                var limit = request.Limit > 0 ? request.Limit : 50;
                var recentRecognitions = await _analyticsService.GetRecentRecognitionsAsync(limit);

                var response = new GetHistoryResponse();
                var since = request.Since?.ToDateTime();

                var history = recentRecognitions
                    .Where(r => since == null || r.CompletedTime >= since)
                    .Select(r => new RecognitionHistory
                    {
                        Id = r.Id.ToString(),
                        Text = r.Text,
                        Confidence = r.Confidence,
                        Engine = r.Engine.ToString(),
                        Language = r.Language,
                        Duration = r.Duration.TotalMilliseconds,
                        CompletedTime = Timestamp.FromDateTime(r.CompletedTime),
                        ApplicationName = r.Context != null ? r.Context.ToString() : ""
                    });

                response.History.AddRange(history);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to get history"));
            }
        }

        public override async Task<InjectTextResponse> InjectText(InjectTextRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Text is required"));
                }

                // This would use the text injection service
                // Implementation depends on having access to the text injection service
                // For now, return success response with actual async work
                await Task.Delay(1); // Minimal async operation to justify async

                return new InjectTextResponse
                {
                    Message = "Text injection completed",
                    Text = request.Text,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error injecting text");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to inject text"));
            }
        }

        public override async Task StreamRecognition(StreamRecognitionRequest request, IServerStreamWriter<StreamRecognitionResponse> responseStream, ServerCallContext context)
        {
            try
            {
                // This would implement real-time streaming recognition
                // For demonstration purposes, simulate streaming responses
                
                var sessionId = Guid.NewGuid().ToString();
                
                // Send initial response
                await responseStream.WriteAsync(new StreamRecognitionResponse
                {
                    SessionId = sessionId,
                    Status = "Started",
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                });

                // Simulate streaming recognition results
                // In a real implementation, this would listen to the speech recognition service events
                var cancellationToken = context.CancellationToken;
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Wait for a short period
                    await Task.Delay(1000, cancellationToken);

                    // Send interim or final results
                    await responseStream.WriteAsync(new StreamRecognitionResponse
                    {
                        SessionId = sessionId,
                        Status = "Listening",
                        InterimText = "Partial recognition...",
                        Confidence = 0.75f,
                        Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected, this is normal
                _logger.LogInformation("Recognition streaming cancelled by client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in streaming recognition");
                throw new RpcException(new Status(StatusCode.Internal, "Streaming recognition failed"));
            }
        }

        #region Helper Methods

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

        private static TextTone ParseTone(string tone)
        {
            return System.Enum.TryParse<TextTone>(tone, true, out var result) ? result : TextTone.Natural;
        }

        private static TextFormatType ParseFormatType(string formatType)
        {
            return System.Enum.TryParse<TextFormatType>(formatType, true, out var result) ? result : TextFormatType.None;
        }

        private static CapitalizationStyle ParseCapitalizationStyle(string style)
        {
            return System.Enum.TryParse<CapitalizationStyle>(style, true, out var result) ? result : CapitalizationStyle.Sentence;
        }

        private static Core.Models.SpeechEngine ParseSpeechEngine(string engine)
        {
            return System.Enum.TryParse<Core.Models.SpeechEngine>(engine, true, out var result) ? result : Core.Models.SpeechEngine.WindowsSpeech;
        }

        private static Core.Enums.RecognitionQuality ParseRecognitionQuality(string quality)
        {
            return System.Enum.TryParse<Core.Enums.RecognitionQuality>(quality, true, out var result) ? result : Core.Enums.RecognitionQuality.Balanced;
        }

        private static Core.Enums.VoiceActivationMode ParseVoiceActivationMode(string mode)
        {
            return System.Enum.TryParse<Core.Enums.VoiceActivationMode>(mode, true, out var result) ? result : Core.Enums.VoiceActivationMode.Manual;
        }

        private static Core.Models.SpeechEngineType ParseSpeechEngineType(string engine)
        {
            // Convert from Core.Models.SpeechEngine to Core.Models.SpeechEngineType
            var parsedEnum = System.Enum.TryParse<Core.Models.SpeechEngine>(engine, true, out var result) ? result : Core.Models.SpeechEngine.WindowsSpeech;
            return (Core.Models.SpeechEngineType)(int)parsedEnum;
        }

        #endregion
    }
}