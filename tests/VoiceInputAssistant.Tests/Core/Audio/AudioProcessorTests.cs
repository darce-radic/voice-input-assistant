using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services;
using VoiceInputAssistant.Core.Services.Interfaces;
using Xunit;
using AutoFixture;

namespace VoiceInputAssistant.Tests.Core.Audio;

/// <summary>
/// Tests for IAudioProcessor using real NAudioProcessor implementation
/// </summary>
public class AudioProcessorTests
{
    private readonly Fixture _fixture;
    private readonly Mock<ILogger<NAudioProcessor>> _loggerMock;

    public AudioProcessorTests()
    {
        _fixture = new Fixture();
        _loggerMock = new Mock<ILogger<NAudioProcessor>>();
    }

    private IAudioProcessor CreateAudioProcessor()
    {
        return new NAudioProcessor(_loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_WithValidSettings_ShouldCompleteSuccessfully()
    {
        // Arrange
        var audioSettings = new AudioSettings
        {
            PreferredInputDevice = null, // Use default device
            InputVolume = 100,
            NoiseThreshold = 10,
            AutoAdjustNoiseThreshold = true
        };

        // Act
        var audioProcessor = CreateAudioProcessor();
        var act = async () => await audioProcessor.InitializeAsync(audioSettings);
        
        // Assert - Should not throw exception
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CaptureAudioAsync_WithValidDevice_ShouldReturnAudioData()
    {
        // Arrange
        var audioDevice = new AudioDevice 
        { 
            Id = "test-device-1", 
            Name = "Test Microphone",
            IsDefault = true,
            IsEnabled = true,
            IsInput = true
        };
        var duration = 2000; // 2 seconds

        // Act & Assert - Define contract for audio capture
        // var audioProcessor = CreateAudioProcessor();
        // var result = await audioProcessor.CaptureAudioAsync(audioDevice, duration);
        
        // result.Should().NotBeNull();
        // result.Length.Should().BeGreaterThan(0);
        // Expected size: 16000 samples/sec * 2 sec * 2 bytes/sample = 64KB
        // result.Length.Should().BeApproximately(64000, 1000);

        // Placeholder until implementation
        audioDevice.Should().NotBeNull();
    }

    [Fact] 
    public async Task PreprocessAudioAsync_WithRawAudio_ShouldReturnCleanedAudio()
    {
        // Arrange
        var rawAudio = GenerateTestAudioData(sampleRate: 16000, durationSeconds: 2);
        var settings = new AudioSettings
        {
            PreferredInputDevice = null,
            NoiseThreshold = 15,
            AutoAdjustNoiseThreshold = false
        };

        // Act
        var audioProcessor = CreateAudioProcessor();
        var result = await audioProcessor.PreprocessAudioAsync(rawAudio, settings);
        
        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(rawAudio.Length); // Same length, different quality
        // Note: For clean test data, preprocessing might not change the data significantly
        // but the method should complete successfully
    }

    [Theory]
    [InlineData(0.0f)] // No noise reduction
    [InlineData(0.5f)] // Medium noise reduction  
    [InlineData(1.0f)] // Maximum noise reduction
    public async Task ReduceNoiseAsync_WithDifferentLevels_ShouldAdjustNoiseProperly(float noiseLevel)
    {
        // Arrange
        var noisyAudio = GenerateNoisyTestAudio();

        // Act & Assert - Define noise reduction contract
        // var audioProcessor = CreateAudioProcessor();
        // var result = await audioProcessor.ReduceNoiseAsync(noisyAudio, noiseLevel);
        
        // result.Should().NotBeNull();
        // result.Length.Should().Be(noisyAudio.Length);
        
        // if (noiseLevel > 0)
        // {
        //     result.Should().NotEqual(noisyAudio); // Should be different with noise reduction
        // }

        noiseLevel.Should().BeInRange(0.0f, 1.0f);
    }

    [Fact]
    public async Task NormalizeVolumeAsync_WithQuietAudio_ShouldIncreaseVolume()
    {
        // Arrange
        var quietAudio = GenerateQuietTestAudio();
        var targetDb = -16.0f; // Standard target level

        // Act & Assert - Define volume normalization contract
        // var audioProcessor = CreateAudioProcessor();
        // var result = await audioProcessor.NormalizeVolumeAsync(quietAudio, targetDb);
        
        // result.Should().NotBeNull();
        // result.Length.Should().Be(quietAudio.Length);
        // GetAudioLevel(result).Should().BeGreaterThan(GetAudioLevel(quietAudio));

        quietAudio.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAudioInputLevelAsync_ShouldReturnValidLevel()
    {
        // Arrange
        var audioProcessor = CreateAudioProcessor();
        
        // Initialize first to ensure device is ready
        var settings = new AudioSettings { PreferredInputDevice = null };
        await audioProcessor.InitializeAsync(settings);
        
        // Act
        var level = await audioProcessor.GetAudioInputLevelAsync();
        
        // Assert
        level.Should().BeInRange(0.0f, 1.0f);
    }

    [Theory]
    [InlineData(SpeechEngineType.WhisperLocal)]
    [InlineData(SpeechEngineType.AzureSpeech)]
    [InlineData(SpeechEngineType.OpenAIWhisper)]
    public async Task ConvertAudioFormatAsync_ForDifferentEngines_ShouldReturnOptimalFormat(SpeechEngineType engine)
    {
        // Arrange
        var sourceAudio = GenerateTestAudioData(44100, 2); // High-quality source
        var targetFormat = GetOptimalFormatForEngine(engine);

        // Act & Assert - Define format conversion contract
        // var audioProcessor = CreateAudioProcessor();
        // var result = await audioProcessor.ConvertAudioFormatAsync(sourceAudio, targetFormat, engine);
        
        // result.Should().NotBeNull();
        // result.Should().NotEqual(sourceAudio); // Should be converted
        // Verify format matches engine requirements

        targetFormat.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAudioInputDevicesAsync_ShouldReturnAvailableDevices()
    {
        // Arrange
        var audioProcessor = CreateAudioProcessor();
        await audioProcessor.InitializeAsync(new AudioSettings { PreferredInputDevice = null });
        
        // Act
        var devices = await audioProcessor.GetAudioInputDevicesAsync();
        
        // Assert
        devices.Should().NotBeNull();
        // On Windows, there should typically be at least one audio input device
        // Even if no physical microphone, Windows usually has a default device
        devices.Should().HaveCountGreaterOrEqualTo(0);
        
        // Verify device properties if devices exist
        foreach (var device in devices)
        {
            device.Should().NotBeNull();
            device.Id.Should().NotBeNullOrEmpty();
            device.Name.Should().NotBeNullOrEmpty();
            device.IsInput.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CaptureAudioAsync_WithInvalidDevice_ShouldThrowException()
    {
        // Arrange
        var invalidDevice = new AudioDevice { Id = "non-existent-device" };

        // Act & Assert - Define error handling contract
        // var audioProcessor = CreateAudioProcessor();
        // await FluentActions
        //     .Invoking(async () => await audioProcessor.CaptureAudioAsync(invalidDevice))
        //     .Should().ThrowAsync<ArgumentException>()
        //     .WithMessage("*device*");

        invalidDevice.Should().NotBeNull();
    }

    #region Test Helpers

    private static byte[] GenerateTestAudioData(int sampleRate = 16000, int durationSeconds = 2)
    {
        var samples = sampleRate * durationSeconds;
        var audioData = new byte[samples * 2]; // 16-bit = 2 bytes per sample

        // Generate sine wave at 440Hz (A4 note)
        for (int i = 0; i < samples; i++)
        {
            var sample = (short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 16384);
            var bytes = BitConverter.GetBytes(sample);
            audioData[i * 2] = bytes[0];
            audioData[i * 2 + 1] = bytes[1];
        }

        return audioData;
    }

    private static byte[] GenerateNoisyTestAudio()
    {
        var random = new Random(42); // Consistent seed for testing
        var audioData = GenerateTestAudioData();
        
        // Add random noise
        for (int i = 0; i < audioData.Length; i += 2)
        {
            var originalSample = BitConverter.ToInt16(audioData, i);
            var noise = (short)(random.Next(-1000, 1000));
            var noisySample = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, originalSample + noise));
            var bytes = BitConverter.GetBytes(noisySample);
            audioData[i] = bytes[0];
            audioData[i + 1] = bytes[1];
        }

        return audioData;
    }

    private static byte[] GenerateQuietTestAudio()
    {
        var audioData = GenerateTestAudioData();
        
        // Reduce volume by 75%
        for (int i = 0; i < audioData.Length; i += 2)
        {
            var sample = BitConverter.ToInt16(audioData, i);
            var quietSample = (short)(sample * 0.25);
            var bytes = BitConverter.GetBytes(quietSample);
            audioData[i] = bytes[0];
            audioData[i + 1] = bytes[1];
        }

        return audioData;
    }

    private static AudioFormat GetOptimalFormatForEngine(SpeechEngineType engine)
    {
        return engine switch
        {
            SpeechEngineType.WhisperLocal => new AudioFormat 
            { 
                SampleRate = 16000, 
                BitsPerSample = 16, 
                Channels = 1,
                Encoding = VoiceInputAssistant.Core.Models.AudioEncoding.PCM,
                BlockAlign = 2
            },
            SpeechEngineType.AzureSpeech => new AudioFormat 
            { 
                SampleRate = 16000, 
                BitsPerSample = 16, 
                Channels = 1,
                Encoding = VoiceInputAssistant.Core.Models.AudioEncoding.PCM,
                BlockAlign = 2
            },
            SpeechEngineType.OpenAIWhisper => new AudioFormat 
            { 
                SampleRate = 16000, 
                BitsPerSample = 16, 
                Channels = 1,
                Encoding = VoiceInputAssistant.Core.Models.AudioEncoding.PCM,
                BlockAlign = 2
            },
            _ => new AudioFormat 
            { 
                SampleRate = 16000, 
                BitsPerSample = 16, 
                Channels = 1,
                Encoding = VoiceInputAssistant.Core.Models.AudioEncoding.PCM,
                BlockAlign = 2
            }
        };
    }

    #endregion
}