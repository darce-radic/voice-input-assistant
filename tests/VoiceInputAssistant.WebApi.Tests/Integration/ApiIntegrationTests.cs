using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Enums;
using VoiceInputAssistant.WebApi.Controllers;

namespace VoiceInputAssistant.WebApi.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Mock<ISpeechRecognitionService> _speechServiceMock;
        private readonly Mock<IApplicationProfileService> _profileServiceMock;
        private readonly Mock<IUsageAnalyticsService> _analyticsServiceMock;
        private readonly Mock<IAiPostProcessingService> _aiServiceMock;
        private readonly Mock<ISettingsService> _settingsServiceMock;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _speechServiceMock = new Mock<ISpeechRecognitionService>();
            _profileServiceMock = new Mock<IApplicationProfileService>();
            _analyticsServiceMock = new Mock<IUsageAnalyticsService>();
            _aiServiceMock = new Mock<IAiPostProcessingService>();
            _settingsServiceMock = new Mock<ISettingsService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace services with mocks
                    services.AddScoped(_ => _speechServiceMock.Object);
                    services.AddScoped(_ => _profileServiceMock.Object);
                    services.AddScoped(_ => _analyticsServiceMock.Object);
                    services.AddScoped(_ => _aiServiceMock.Object);
                    services.AddScoped(_ => _settingsServiceMock.Object);
                });
            });

            _client = _factory.CreateClient();
        }

        private string GenerateJwtToken()
        {
            var key = "VoiceInputAssistant_SuperSecretKey_2024_MinimumLength32Characters!";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "testuser"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", "Admin")
            };

            var token = new JwtSecurityToken(
                issuer: "VoiceInputAssistant",
                audience: "VoiceInputAssistant",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetupAuthenticatedClient()
        {
            var token = GenerateJwtToken();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        #region Authentication Tests

        [Fact]
        public async Task GetStatus_WithoutAuth_ShouldReturn401()
        {
            // Act
            var response = await _client.GetAsync("/api/voiceassistant/status");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetStatus_WithValidAuth_ShouldReturn200()
        {
            // Arrange
            SetupAuthenticatedClient();
            
            var settings = new ApplicationSettings
            {
                SpeechEngine = SpeechEngine.AzureCognitiveServices,
                RecognitionQuality = RecognitionQuality.High
            };

            _settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(settings);
            _profileServiceMock.Setup(x => x.GetActiveProfileAsync()).ReturnsAsync((ApplicationProfile)null);
            _speechServiceMock.Setup(x => x.IsListening).Returns(false);
            _speechServiceMock.Setup(x => x.IsInitialized).Returns(true);

            // Act
            var response = await _client.GetAsync("/api/voiceassistant/status");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Status Endpoint Tests

        [Fact]
        public async Task GetStatus_WithValidAuth_ShouldReturnCorrectStatus()
        {
            // Arrange
            SetupAuthenticatedClient();
            
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
            var response = await _client.GetAsync("/api/voiceassistant/status");
            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<SystemStatusResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            status.Should().NotBeNull();
            status.IsHealthy.Should().BeTrue();
            status.IsListening.Should().BeTrue();
            status.IsInitialized.Should().BeTrue();
            status.ActiveEngine.Should().Be("AzureCognitiveServices");
            status.ActiveProfile.Should().Be("Test Profile");
        }

        #endregion

        #region Recognition Endpoint Tests

        [Fact]
        public async Task StartRecognition_WhenNotInitialized_ShouldReturn400()
        {
            // Arrange
            SetupAuthenticatedClient();
            _speechServiceMock.Setup(x => x.IsInitialized).Returns(false);

            var request = new StartRecognitionRequest
            {
                Engine = "AzureCognitiveServices",
                Quality = "High"
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/recognition/start", stringContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task StartRecognition_WhenValidRequest_ShouldReturn200()
        {
            // Arrange
            SetupAuthenticatedClient();
            _speechServiceMock.Setup(x => x.IsInitialized).Returns(true);
            _speechServiceMock.Setup(x => x.IsListening).Returns(false);
            _speechServiceMock.Setup(x => x.CurrentEngine).Returns(SpeechEngine.AzureCognitiveServices);
            _speechServiceMock.Setup(x => x.StartListeningAsync()).Returns(Task.CompletedTask);

            var request = new StartRecognitionRequest
            {
                Engine = "AzureCognitiveServices",
                Quality = "High"
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/recognition/start", stringContent);
            var content = await response.Content.ReadAsStringAsync();
            var sessionResponse = JsonSerializer.Deserialize<RecognitionSessionResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            sessionResponse.Should().NotBeNull();
            sessionResponse.Status.Should().Be("Started");
            sessionResponse.Engine.Should().Be("AzureCognitiveServices");
        }

        [Fact]
        public async Task StopRecognition_WhenNotListening_ShouldReturn400()
        {
            // Arrange
            SetupAuthenticatedClient();
            _speechServiceMock.Setup(x => x.IsListening).Returns(false);

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/recognition/stop", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Text Processing Tests

        [Fact]
        public async Task ProcessText_WithEmptyText_ShouldReturn400()
        {
            // Arrange
            SetupAuthenticatedClient();

            var request = new TextProcessingRequest
            {
                Text = ""
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/text/process", stringContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ProcessText_WithValidText_ShouldReturn200()
        {
            // Arrange
            SetupAuthenticatedClient();

            var request = new TextProcessingRequest
            {
                Text = "hello world",
                CorrectGrammar = true
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
                }
            };

            _aiServiceMock.Setup(x => x.ProcessTextAsync(It.IsAny<PostProcessingRequest>()))
                .ReturnsAsync(aiResult);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/text/process", stringContent);
            var content = await response.Content.ReadAsStringAsync();
            var textResponse = JsonSerializer.Deserialize<TextProcessingResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            textResponse.Should().NotBeNull();
            textResponse.ProcessedText.Should().Be("Hello, world.");
            textResponse.Success.Should().BeTrue();
        }

        #endregion

        #region Analytics Tests

        [Fact]
        public async Task GetAnalytics_ShouldReturn200WithData()
        {
            // Arrange
            SetupAuthenticatedClient();

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
            var response = await _client.GetAsync("/api/voiceassistant/analytics");
            var content = await response.Content.ReadAsStringAsync();
            var analytics = JsonSerializer.Deserialize<AnalyticsResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            analytics.Should().NotBeNull();
            analytics.TotalRecognitions.Should().Be(1000);
            analytics.TotalWords.Should().Be(15000);
            analytics.TopApplications.Should().HaveCount(1);
        }

        #endregion

        #region Profile Management Tests

        [Fact]
        public async Task GetProfiles_ShouldReturn200WithProfiles()
        {
            // Arrange
            SetupAuthenticatedClient();

            var profiles = new List<ApplicationProfile>
            {
                new ApplicationProfile
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Profile",
                    Description = "Test Description",
                    ApplicationExecutables = new List<string> { "test.exe" },
                    IsEnabled = true,
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _profileServiceMock.Setup(x => x.GetAllProfilesAsync()).ReturnsAsync(profiles);

            // Act
            var response = await _client.GetAsync("/api/voiceassistant/profiles");
            var content = await response.Content.ReadAsStringAsync();
            var profileDtos = JsonSerializer.Deserialize<List<ApplicationProfileDto>>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            profileDtos.Should().NotBeNull();
            profileDtos.Should().HaveCount(1);
            profileDtos.First().Name.Should().Be("Test Profile");
        }

        [Fact]
        public async Task CreateProfile_WithEmptyName_ShouldReturn400()
        {
            // Arrange
            SetupAuthenticatedClient();

            var request = new CreateProfileRequest
            {
                Name = "",
                Description = "Test profile"
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/profiles", stringContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProfile_WithValidData_ShouldReturn201()
        {
            // Arrange
            SetupAuthenticatedClient();

            var request = new CreateProfileRequest
            {
                Name = "New Profile",
                Description = "Test profile",
                ApplicationExecutables = new List<string> { "test.exe" },
                IsEnabled = true
            };

            var createdProfile = new ApplicationProfile
            {
                Id = Guid.NewGuid(),
                Name = "New Profile",
                Description = "Test profile",
                ApplicationExecutables = new List<string> { "test.exe" },
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _profileServiceMock.Setup(x => x.CreateProfileAsync(It.IsAny<ApplicationProfile>()))
                .ReturnsAsync(createdProfile);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/profiles", stringContent);
            var content = await response.Content.ReadAsStringAsync();
            var profileDto = JsonSerializer.Deserialize<ApplicationProfileDto>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            profileDto.Should().NotBeNull();
            profileDto.Name.Should().Be("New Profile");
        }

        #endregion

        #region Settings Tests

        [Fact]
        public async Task UpdateSettings_WithValidData_ShouldReturn200()
        {
            // Arrange
            SetupAuthenticatedClient();

            var existingSettings = new ApplicationSettings
            {
                SpeechEngine = SpeechEngine.WindowsSpeechRecognition,
                RecognitionQuality = RecognitionQuality.Balanced,
                EnableGlobalHotkeys = false
            };

            var request = new UpdateSettingsRequest
            {
                SpeechEngine = "AzureCognitiveServices",
                RecognitionQuality = "High",
                EnableGlobalHotkeys = true
            };

            _settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(existingSettings);
            _settingsServiceMock.Setup(x => x.SaveSettingsAsync(It.IsAny<ApplicationSettings>()))
                .Returns(Task.CompletedTask);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/api/voiceassistant/settings", stringContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Health Check Tests

        [Fact]
        public async Task HealthCheck_ShouldReturn200()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Content Type and CORS Tests

        [Fact]
        public async Task ApiEndpoints_ShouldAcceptJsonContent()
        {
            // Arrange
            SetupAuthenticatedClient();

            var request = new TextProcessingRequest
            {
                Text = "test"
            };

            _aiServiceMock.Setup(x => x.ProcessTextAsync(It.IsAny<PostProcessingRequest>()))
                .ReturnsAsync(new PostProcessingResult
                {
                    OriginalText = "test",
                    ProcessedText = "Test.",
                    Success = true
                });

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/voiceassistant/text/process", stringContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }

        #endregion
    }

    // DTO classes for integration tests
    public class SystemStatusResponse
    {
        public bool IsHealthy { get; set; }
        public bool IsListening { get; set; }
        public bool IsInitialized { get; set; }
        public string ActiveEngine { get; set; } = "";
        public string ActiveProfile { get; set; } = "";
        public string Version { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class StartRecognitionRequest
    {
        public string? Engine { get; set; }
        public string? Quality { get; set; }
        public string? Language { get; set; }
        public int? TimeoutSeconds { get; set; }
    }

    public class RecognitionSessionResponse
    {
        public Guid SessionId { get; set; }
        public string Status { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public string Engine { get; set; } = "";
        public string Quality { get; set; } = "";
    }

    public class TextProcessingRequest
    {
        public string Text { get; set; } = "";
        public string? Context { get; set; }
        public string? ApplicationContext { get; set; }
        public bool? CorrectGrammar { get; set; }
        public string? TargetTone { get; set; }
        public string? FormatType { get; set; }
        public bool? ExpandAbbreviations { get; set; }
        public bool? AddPunctuation { get; set; }
        public string? CapitalizationStyle { get; set; }
        public string? TranslateToLanguage { get; set; }
        public bool? Summarize { get; set; }
        public int? SummaryMaxLength { get; set; }
    }

    public class TextProcessingResponse
    {
        public string OriginalText { get; set; } = "";
        public string ProcessedText { get; set; } = "";
        public List<TextChangeDto> Changes { get; set; } = new();
        public float Confidence { get; set; }
        public double ProcessingTimeMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TextChangeDto
    {
        public string Type { get; set; } = "";
        public string Reason { get; set; } = "";
        public string OriginalText { get; set; } = "";
        public string NewText { get; set; } = "";
    }

    public class AnalyticsResponse
    {
        public int TotalRecognitions { get; set; }
        public int TotalWords { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
        public List<ApplicationUsageDto> TopApplications { get; set; } = new();
        public List<EngineStatsDto> EngineStatistics { get; set; } = new();
    }

    public class ApplicationUsageDto
    {
        public string ApplicationName { get; set; } = "";
        public int RecognitionCount { get; set; }
        public double TotalUsageTime { get; set; }
        public double AverageAccuracy { get; set; }
    }

    public class EngineStatsDto
    {
        public string EngineName { get; set; } = "";
        public int TotalRecognitions { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageProcessingTime { get; set; }
    }

    public class ApplicationProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public List<string> ApplicationExecutables { get; set; } = new();
        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateProfileRequest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public List<string>? ApplicationExecutables { get; set; }
        public bool? IsEnabled { get; set; }
        public string? Engine { get; set; }
        public string? Quality { get; set; }
        public string? Language { get; set; }
    }

    public class UpdateSettingsRequest
    {
        public string? SpeechEngine { get; set; }
        public string? RecognitionQuality { get; set; }
        public string? VoiceActivationMode { get; set; }
        public bool? EnableGlobalHotkeys { get; set; }
        public bool? EnableAnalytics { get; set; }
        public string? Theme { get; set; }
    }
}