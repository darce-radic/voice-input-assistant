using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Enums;
using VoiceInputAssistant.WebApi.Controllers;

namespace VoiceInputAssistant.WebApi.Tests.Controllers
{
    public class VoiceAssistantControllerTests
    {
        private readonly Mock<ILogger<VoiceAssistantController>> _loggerMock;
        private readonly Mock<ISpeechRecognitionService> _speechServiceMock;
        private readonly Mock<IApplicationProfileService> _profileServiceMock;
        private readonly Mock<IUsageAnalyticsService> _analyticsServiceMock;
        private readonly Mock<IAiPostProcessingService> _aiServiceMock;
        private readonly Mock<ISettingsService> _settingsServiceMock;
        private readonly VoiceAssistantController _controller;

        public VoiceAssistantControllerTests()
        {
            _loggerMock = new Mock<ILogger<VoiceAssistantController>>();
            _speechServiceMock = new Mock<ISpeechRecognitionService>();
            _profileServiceMock = new Mock<IApplicationProfileService>();
            _analyticsServiceMock = new Mock<IUsageAnalyticsService>();
            _aiServiceMock = new Mock<IAiPostProcessingService>();
            _settingsServiceMock = new Mock<ISettingsService>();

            _controller = new VoiceAssistantController(
                _loggerMock.Object,
                _speechServiceMock.Object,
                _profileServiceMock.Object,
                _analyticsServiceMock.Object,
                _aiServiceMock.Object,
                _settingsServiceMock.Object);
        }

        #region GetStatus Tests

        [Fact]
        public async Task GetStatus_WhenServiceIsHealthy_ShouldReturnOkWithStatus()
        {
            // Arrange
            var settings = new ApplicationSettings
            {
                SpeechEngine = SpeechEngine.AzureCognitiveServices,
                RecognitionQuality = RecognitionQuality.High
            };
            var profile = new ApplicationProfile
            {
                Name = "Test Profile"
            };

            _settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(settings);
            _profileServiceMock.Setup(x => x.GetActiveProfileAsync()).ReturnsAsync(profile);
            _speechServiceMock.Setup(x => x.IsListening).Returns(true);
            _speechServiceMock.Setup(x => x.IsInitialized).Returns(true);

            // Act
            var result = await _controller.GetStatus();

            // Assert
            result.Should().BeOfType<ActionResult<SystemStatusResponse>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<SystemStatusResponse>().Subject;

            response.IsHealthy.Should().BeTrue();
            response.IsListening.Should().BeTrue();
            response.IsInitialized.Should().BeTrue();
            response.ActiveEngine.Should().Be("AzureCognitiveServices");
            response.ActiveProfile.Should().Be("Test Profile");
            response.Version.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetStatus_WhenServiceThrows_ShouldReturn500()
        {
            // Arrange
            _settingsServiceMock.Setup(x => x.GetSettingsAsync())
                .ThrowsAsync(new InvalidOperationException("Service error"));

            // Act
            var result = await _controller.GetStatus();

            // Assert
            result.Result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(500);
        }

        #endregion

        #region StartRecognition Tests

        [Fact]
        public async Task StartRecognition_WhenServiceNotInitialized_ShouldReturnBadRequest()
        {
            // Arrange
            _speechServiceMock.Setup(x => x.IsInitialized).Returns(false);

            var request = new StartRecognitionRequest
            {
                Engine = "AzureCognitiveServices",
                Quality = "High"
            };

            // Act
            var result = await _controller.StartRecognition(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task StartRecognition_WhenAlreadyListening_ShouldReturnConflict()
        {
            // Arrange
            _speechServiceMock.Setup(x => x.IsInitialized).Returns(true);
            _speechServiceMock.Setup(x => x.IsListening).Returns(true);

            var request = new StartRecognitionRequest
            {
                Engine = "AzureCognitiveServices",
                Quality = "High"
            };

            // Act
            var result = await _controller.StartRecognition(request);

            // Assert
            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task StartRecognition_WhenValidRequest_ShouldReturnOkWithSession()
        {
            // Arrange
            _speechServiceMock.Setup(x => x.IsInitialized).Returns(true);
            _speechServiceMock.Setup(x => x.IsListening).Returns(false);
            _speechServiceMock.Setup(x => x.CurrentEngine).Returns(SpeechEngine.AzureCognitiveServices);
            _speechServiceMock.Setup(x => x.StartListeningAsync()).Returns(Task.CompletedTask);

            var request = new StartRecognitionRequest
            {
                Engine = "AzureCognitiveServices",
                Quality = "High"
            };

            // Act
            var result = await _controller.StartRecognition(request);

            // Assert
            result.Should().BeOfType<ActionResult<RecognitionSessionResponse>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RecognitionSessionResponse>().Subject;

            response.SessionId.Should().NotBeEmpty();
            response.Status.Should().Be("Started");
            response.Engine.Should().Be("AzureCognitiveServices");
            response.Quality.Should().Be("High");
        }

        #endregion

        #region StopRecognition Tests

        [Fact]
        public async Task StopRecognition_WhenNotListening_ShouldReturnBadRequest()
        {
            // Arrange
            _speechServiceMock.Setup(x => x.IsListening).Returns(false);

            // Act
            var result = await _controller.StopRecognition();

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task StopRecognition_WhenListening_ShouldReturnOk()
        {
            // Arrange
            _speechServiceMock.Setup(x => x.IsListening).Returns(true);
            _speechServiceMock.Setup(x => x.StopListeningAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.StopRecognition();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region ProcessText Tests

        [Fact]
        public async Task ProcessText_WhenTextIsEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new TextProcessingRequest
            {
                Text = ""
            };

            // Act
            var result = await _controller.ProcessText(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessText_WhenValidRequest_ShouldReturnProcessedText()
        {
            // Arrange
            var request = new TextProcessingRequest
            {
                Text = "hello world",
                CorrectGrammar = true,
                TargetTone = "Professional",
                FormatType = "Sentence"
            };

            var aiResult = new PostProcessingResult
            {
                OriginalText = "hello world",
                ProcessedText = "Hello, world.",
                Success = true,
                Confidence = 0.95f,
                Changes = new List<TextChange>
                {
                    new TextChange
                    {
                        Type = TextChangeType.Capitalization,
                        Reason = "Capitalized first word",
                        OriginalText = "hello",
                        NewText = "Hello"
                    }
                },
                Stats = new ProcessingStats
                {
                    ProcessingTime = TimeSpan.FromMilliseconds(150)
                }
            };

            _aiServiceMock.Setup(x => x.ProcessTextAsync(It.IsAny<PostProcessingRequest>()))
                .ReturnsAsync(aiResult);

            // Act
            var result = await _controller.ProcessText(request);

            // Assert
            result.Should().BeOfType<ActionResult<TextProcessingResponse>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<TextProcessingResponse>().Subject;

            response.OriginalText.Should().Be("hello world");
            response.ProcessedText.Should().Be("Hello, world.");
            response.Success.Should().BeTrue();
            response.Confidence.Should().Be(0.95f);
            response.Changes.Should().HaveCount(1);
            response.ProcessingTimeMs.Should().Be(150);
        }

        #endregion

        #region GetAnalytics Tests

        [Fact]
        public async Task GetAnalytics_WhenCalled_ShouldReturnAnalyticsData()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            var stats = new UsageStatistics
            {
                TotalRecognitions = 1000,
                TotalWords = 15000,
                AverageAccuracy = 94.5,
                AverageProcessingTime = TimeSpan.FromMilliseconds(200)
            };

            var topApps = new List<ApplicationUsageStatistics>
            {
                new ApplicationUsageStatistics
                {
                    ApplicationName = "Microsoft Word",
                    RecognitionCount = 500,
                    TotalUsageTime = TimeSpan.FromHours(10),
                    AverageAccuracy = 96.0
                }
            };

            var engineStats = new List<EngineAccuracyStatistics>
            {
                new EngineAccuracyStatistics
                {
                    EngineName = "Azure Cognitive Services",
                    TotalRecognitions = 800,
                    AverageAccuracy = 95.5,
                    AverageProcessingTime = TimeSpan.FromMilliseconds(180)
                }
            };

            _analyticsServiceMock.Setup(x => x.GetUsageStatisticsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(stats);
            _analyticsServiceMock.Setup(x => x.GetTopApplicationsAsync(10, 30))
                .ReturnsAsync(topApps);
            _analyticsServiceMock.Setup(x => x.GetAccuracyStatisticsByEngineAsync(30))
                .ReturnsAsync(engineStats);

            // Act
            var result = await _controller.GetAnalytics(startDate, endDate);

            // Assert
            result.Should().BeOfType<ActionResult<AnalyticsResponse>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AnalyticsResponse>().Subject;

            response.TotalRecognitions.Should().Be(1000);
            response.TotalWords.Should().Be(15000);
            response.AverageAccuracy.Should().Be(94.5);
            response.AverageProcessingTime.Should().Be(200);
            response.TopApplications.Should().HaveCount(1);
            response.EngineStatistics.Should().HaveCount(1);
        }

        #endregion

        #region GetProfiles Tests

        [Fact]
        public async Task GetProfiles_WhenCalled_ShouldReturnProfiles()
        {
            // Arrange
            var profiles = new List<ApplicationProfile>
            {
                new ApplicationProfile
                {
                    Id = Guid.NewGuid(),
                    Name = "Development Profile",
                    Description = "Profile for development tools",
                    ApplicationExecutables = new List<string> { "devenv.exe", "code.exe" },
                    IsEnabled = true,
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _profileServiceMock.Setup(x => x.GetAllProfilesAsync()).ReturnsAsync(profiles);

            // Act
            var result = await _controller.GetProfiles();

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<ApplicationProfileDto>>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ApplicationProfileDto>>().Subject;

            var profileList = response.ToList();
            profileList.Should().HaveCount(1);
            profileList.First().Name.Should().Be("Development Profile");
            profileList.First().ApplicationExecutables.Should().Contain("devenv.exe");
        }

        #endregion

        #region CreateProfile Tests

        [Fact]
        public async Task CreateProfile_WhenNameIsEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CreateProfileRequest
            {
                Name = "",
                Description = "Test profile"
            };

            // Act
            var result = await _controller.CreateProfile(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateProfile_WhenValidRequest_ShouldReturnCreatedProfile()
        {
            // Arrange
            var request = new CreateProfileRequest
            {
                Name = "New Profile",
                Description = "Test profile",
                ApplicationExecutables = new List<string> { "test.exe" },
                IsEnabled = true,
                Engine = "AzureCognitiveServices",
                Quality = "High",
                Language = "en-US"
            };

            var createdProfile = new ApplicationProfile
            {
                Id = Guid.NewGuid(),
                Name = "New Profile",
                Description = "Test profile",
                ApplicationExecutables = new List<string> { "test.exe" },
                IsEnabled = true,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _profileServiceMock.Setup(x => x.CreateProfileAsync(It.IsAny<ApplicationProfile>()))
                .ReturnsAsync(createdProfile);

            // Act
            var result = await _controller.CreateProfile(request);

            // Assert
            result.Should().BeOfType<ActionResult<ApplicationProfileDto>>();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<ApplicationProfileDto>().Subject;

            response.Name.Should().Be("New Profile");
            response.Description.Should().Be("Test profile");
            response.IsEnabled.Should().BeTrue();
        }

        #endregion

        #region UpdateSettings Tests

        [Fact]
        public async Task UpdateSettings_WhenValidRequest_ShouldUpdateSettings()
        {
            // Arrange
            var existingSettings = new ApplicationSettings
            {
                SpeechEngine = SpeechEngine.WindowsSpeechRecognition,
                RecognitionQuality = RecognitionQuality.Balanced,
                VoiceActivationMode = VoiceActivationMode.Manual,
                EnableGlobalHotkeys = false
            };

            var request = new UpdateSettingsRequest
            {
                SpeechEngine = "AzureCognitiveServices",
                RecognitionQuality = "High",
                VoiceActivationMode = "Continuous",
                EnableGlobalHotkeys = true
            };

            _settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(existingSettings);
            _settingsServiceMock.Setup(x => x.SaveSettingsAsync(It.IsAny<ApplicationSettings>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSettings(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            _settingsServiceMock.Verify(x => x.SaveSettingsAsync(It.Is<ApplicationSettings>(s =>
                s.SpeechEngine == SpeechEngine.AzureCognitiveServices &&
                s.RecognitionQuality == RecognitionQuality.High &&
                s.VoiceActivationMode == VoiceActivationMode.Continuous &&
                s.EnableGlobalHotkeys == true
            )), Times.Once);
        }

        #endregion

        #region GetHistory Tests

        [Fact]
        public async Task GetHistory_WhenCalled_ShouldReturnHistory()
        {
            // Arrange
            var recognitions = new List<RecognitionResult>
            {
                new RecognitionResult
                {
                    Id = Guid.NewGuid(),
                    Text = "Test recognition",
                    Confidence = 0.95,
                    Engine = SpeechEngine.AzureCognitiveServices,
                    Language = "en-US",
                    Duration = TimeSpan.FromSeconds(2),
                    CompletedTime = DateTime.UtcNow,
                    Context = new RecognitionContext
                    {
                        ApplicationName = "Microsoft Word"
                    }
                }
            };

            _analyticsServiceMock.Setup(x => x.GetRecentRecognitionsAsync(50))
                .ReturnsAsync(recognitions);

            // Act
            var result = await _controller.GetHistory(50);

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<RecognitionHistoryDto>>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<IEnumerable<RecognitionHistoryDto>>().Subject;

            var historyList = response.ToList();
            historyList.Should().HaveCount(1);
            historyList.First().Text.Should().Be("Test recognition");
            historyList.First().Confidence.Should().Be(0.95);
        }

        #endregion

        #region InjectText Tests

        [Fact]
        public async Task InjectText_WhenTextIsEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new InjectTextRequest
            {
                Text = ""
            };

            // Act
            var result = await _controller.InjectText(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task InjectText_WhenValidRequest_ShouldReturnOk()
        {
            // Arrange
            var request = new InjectTextRequest
            {
                Text = "Hello, world!",
                TargetApplication = "notepad.exe"
            };

            // Act
            var result = await _controller.InjectText(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion
    }
}