using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services;
using VoiceInputAssistant.Core.Services.Interfaces;
using Xunit;

namespace VoiceInputAssistant.Core.Tests;

/// <summary>
/// Integration tests for speech recognition services
/// </summary>
public class SpeechRecognitionIntegrationTests
{
    private readonly Mock<ILogger<LocalWhisperService>> _localWhisperLogger;
    private readonly Mock<ILogger<AzureSpeechRecognitionService>> _azureLogger;
    private readonly Mock<ILogger<OpenAIWhisperService>> _openAILogger;
    private readonly Mock<ILogger<SpeechRecognitionServiceFactory>> _factoryLogger;
    private readonly Mock<ISpeechRecognitionSettings> _mockSettings;
    private readonly HttpClient _httpClient;

    public SpeechRecognitionIntegrationTests()
    {
        _localWhisperLogger = new Mock<ILogger<LocalWhisperService>>();
        _azureLogger = new Mock<ILogger<AzureSpeechRecognitionService>>();
        _openAILogger = new Mock<ILogger<OpenAIWhisperService>>();
        _factoryLogger = new Mock<ILogger<SpeechRecognitionServiceFactory>>();
        _mockSettings = new Mock<ISpeechRecognitionSettings>();
        _httpClient = new HttpClient();

        // Setup default settings
        _mockSettings.Setup(s => s.Language).Returns("en-US");
        _mockSettings.Setup(s => s.Quality).Returns(RecognitionQuality.Balanced);
        _mockSettings.Setup(s => s.PreferredEngine).Returns(SpeechEngineType.WhisperLocal);
        _mockSettings.Setup(s => s.ContinuousRecognition).Returns(false);
        _mockSettings.Setup(s => s.EnableInterimResults).Returns(false);
        _mockSettings.Setup(s => s.TimeoutSeconds).Returns(30);
    }

    [Fact]
    public async Task LocalWhisperService_ShouldInitializeSuccessfully()
    {
        // Arrange
        var service = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);

        // Act
        await service.InitializeAsync();

        // Assert
        service.IsInitialized.Should().BeTrue();
        service.CurrentEngine.Should().Be(SpeechEngineType.WhisperLocal);
    }

    [Fact]
    public async Task LocalWhisperService_ShouldTranscribeAudioData()
    {
        // Arrange
        var service = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);
        await service.InitializeAsync();

        var testAudioData = GenerateTestAudioData(5000);

        // Act
        var result = await service.TranscribeAsync(testAudioData);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Text.Should().NotBeNullOrEmpty();
        result.Engine.Should().Be(SpeechEngineType.WhisperLocal);
        result.Confidence.Should().BeGreaterThan(0);
        result.Language.Should().NotBeNullOrEmpty();
        result.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task AzureSpeechService_ShouldHandleMissingCredentials()
    {
        // Arrange
        var service = new AzureSpeechRecognitionService(_azureLogger.Object, _mockSettings.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync());
        exception.Message.Should().Contain("subscription key is required");
    }

    [Fact]
    public async Task OpenAIWhisperService_ShouldHandleMissingApiKey()
    {
        // Arrange
        var service = new OpenAIWhisperService(_openAILogger.Object, _mockSettings.Object, _httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync());
        exception.Message.Should().Contain("API key is required");
    }

    [Fact]
    public async Task SpeechRecognitionServiceFactory_ShouldInitializeWithBestAvailableEngine()
    {
        // Arrange
        var azureService = new AzureSpeechRecognitionService(_azureLogger.Object, _mockSettings.Object);
        var openAIService = new OpenAIWhisperService(_openAILogger.Object, _mockSettings.Object, _httpClient);
        var localService = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);

        var factory = new SpeechRecognitionServiceFactory(
            _factoryLogger.Object,
            _mockSettings.Object,
            azureService,
            openAIService,
            localService);

        // Act
        await factory.InitializeAsync();

        // Assert
        factory.IsInitialized.Should().BeTrue();
        factory.CurrentEngineType.Should().Be(SpeechEngineType.WhisperLocal); // Should fallback to local
        factory.CurrentService.Should().NotBeNull();
    }

    [Fact]
    public async Task SpeechRecognitionServiceFactory_ShouldSwitchEnginesSuccessfully()
    {
        // Arrange
        var azureService = new AzureSpeechRecognitionService(_azureLogger.Object, _mockSettings.Object);
        var openAIService = new OpenAIWhisperService(_openAILogger.Object, _mockSettings.Object, _httpClient);
        var localService = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);

        var factory = new SpeechRecognitionServiceFactory(
            _factoryLogger.Object,
            _mockSettings.Object,
            azureService,
            openAIService,
            localService);

        await factory.InitializeAsync(SpeechEngineType.WhisperLocal);

        // Act
        await factory.SwitchEngineAsync(SpeechEngineType.WhisperLocal);

        // Assert
        factory.CurrentEngineType.Should().Be(SpeechEngineType.WhisperLocal);
        factory.CurrentService.Should().Be(localService);
    }

    [Fact]
    public async Task SpeechRecognitionServiceFactory_ShouldGetStatusForAllEngines()
    {
        // Arrange
        var azureService = new AzureSpeechRecognitionService(_azureLogger.Object, _mockSettings.Object);
        var openAIService = new OpenAIWhisperService(_openAILogger.Object, _mockSettings.Object, _httpClient);
        var localService = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);

        var factory = new SpeechRecognitionServiceFactory(
            _factoryLogger.Object,
            _mockSettings.Object,
            azureService,
            openAIService,
            localService);

        // Act
        var statuses = await factory.GetAllEngineStatusAsync();

        // Assert
        statuses.Should().HaveCount(3);
        statuses.Should().Contain(s => s.Engine == SpeechEngineType.WhisperLocal);
        statuses.Should().Contain(s => s.Engine == SpeechEngineType.AzureSpeech);
        statuses.Should().Contain(s => s.Engine == SpeechEngineType.OpenAIWhisper);
    }

    [Fact]
    public async Task SpeechRecognitionServiceFactory_ShouldTranscribeWithCurrentEngine()
    {
        // Arrange
        var azureService = new AzureSpeechRecognitionService(_azureLogger.Object, _mockSettings.Object);
        var openAIService = new OpenAIWhisperService(_openAILogger.Object, _mockSettings.Object, _httpClient);
        var localService = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);

        var factory = new SpeechRecognitionServiceFactory(
            _factoryLogger.Object,
            _mockSettings.Object,
            azureService,
            openAIService,
            localService);

        await factory.InitializeAsync(SpeechEngineType.WhisperLocal);

        var testAudioData = GenerateTestAudioData(3000);

        // Act
        var result = await factory.TranscribeAsync(testAudioData);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Engine.Should().Be(SpeechEngineType.WhisperLocal);
        result.Text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LocalWhisperService_ShouldDetectLanguageFromText()
    {
        // Arrange
        var service = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);
        await service.InitializeAsync();

        // Test different audio sizes to get different simulated responses
        var shortAudio = GenerateTestAudioData(500);   // Should get "Good morning."
        var mediumAudio = GenerateTestAudioData(3000); // Should get longer response

        // Act
        var shortResult = await service.TranscribeAsync(shortAudio);
        var mediumResult = await service.TranscribeAsync(mediumAudio);

        // Assert
        shortResult.Text.Should().NotBeEmpty();
        mediumResult.Text.Should().NotBeEmpty();
        mediumResult.Text.Length.Should().BeGreaterThan(shortResult.Text.Length);
        
        // Both should detect English
        shortResult.Language.Should().Be("en");
        mediumResult.Language.Should().Be("en");
    }

    [Fact]
    public async Task SpeechRecognitionServices_ShouldHandleErrorsGracefully()
    {
        // Test that services handle errors gracefully when not initialized
        var localService = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);
        var azureService = new AzureSpeechRecognitionService(_azureLogger.Object, _mockSettings.Object);
        var openAIService = new OpenAIWhisperService(_openAILogger.Object, _mockSettings.Object, _httpClient);

        var testAudioData = GenerateTestAudioData(1000);

        // Act & Assert - All should return error results when not initialized
        var localResult = await localService.TranscribeAsync(testAudioData);
        var azureResult = await azureService.TranscribeAsync(testAudioData);
        var openAIResult = await openAIService.TranscribeAsync(testAudioData);

        localResult.Success.Should().BeFalse();
        azureResult.Success.Should().BeFalse();
        openAIResult.Success.Should().BeFalse();

        localResult.ErrorMessage.Should().Contain("not initialized");
        azureResult.ErrorMessage.Should().Contain("not initialized");
        openAIResult.ErrorMessage.Should().Contain("not initialized");
    }

    [Fact]
    public async Task LocalWhisperService_ShouldProvideRealisticProcessingTime()
    {
        // Arrange
        var service = new LocalWhisperService(_localWhisperLogger.Object, _mockSettings.Object);
        await service.InitializeAsync();

        var testAudioData = GenerateTestAudioData(10000); // Larger audio file

        // Act
        var result = await service.TranscribeAsync(testAudioData);

        // Assert
        result.ProcessingTime.Should().BeGreaterThan(TimeSpan.FromMilliseconds(50));
        result.ProcessingTime.Should().BeLessThan(TimeSpan.FromSeconds(5));
        result.WordCount.Should().BeGreaterThan(0);
    }

    private static byte[] GenerateTestAudioData(int size)
    {
        // Generate some pseudo-random audio data for testing
        var data = new byte[size];
        var random = new Random(42); // Fixed seed for reproducible tests
        
        for (int i = 0; i < size; i++)
        {
            // Generate somewhat realistic audio-like data (not pure random)
            data[i] = (byte)(128 + (int)(127 * Math.Sin(i * 0.1) * Math.Cos(i * 0.05)));
        }
        
        return data;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}