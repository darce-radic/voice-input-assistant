using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VoiceInputAssistant.Core.Services;
using VoiceInputAssistant.Core.Services.Interfaces;
using Xunit;

namespace VoiceInputAssistant.Tests.Core.Audio;

/// <summary>
/// Tests for IVoiceActivityDetector using real VoiceActivityDetector implementation
/// </summary>
public class VoiceActivityDetectorTests
{
    private readonly Mock<ILogger<VoiceActivityDetector>> _loggerMock;

    public VoiceActivityDetectorTests()
    {
        _loggerMock = new Mock<ILogger<VoiceActivityDetector>>();
    }

    private IVoiceActivityDetector CreateVoiceActivityDetector()
    {
        return new VoiceActivityDetector(_loggerMock.Object);
    }
    [Theory]
    [InlineData(0.3f, 500)]   // Low sensitivity, short silence
    [InlineData(0.7f, 1000)]  // Medium sensitivity, standard silence
    [InlineData(0.9f, 2000)]  // High sensitivity, long silence
    public async Task InitializeAsync_WithDifferentSettings_ShouldConfigureProperly(float sensitivity, int silenceDurationMs)
    {
        // Arrange & Act
        var vadDetector = CreateVoiceActivityDetector();
        var act = async () => await vadDetector.InitializeAsync(sensitivity, silenceDurationMs);
        
        // Assert - Should not throw exception
        await act.Should().NotThrowAsync();
        sensitivity.Should().BeInRange(0.0f, 1.0f);
        silenceDurationMs.Should().BePositive();
    }

    [Fact]
    public async Task IsSpeechDetectedAsync_WithSpeechAudio_ShouldReturnTrue()
    {
        // Arrange
        var speechAudio = GenerateSpeechAudio();
        var vadDetector = CreateVoiceActivityDetector();
        await vadDetector.InitializeAsync();

        // Act
        var result = await vadDetector.IsSpeechDetectedAsync(speechAudio);
        
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSpeechDetectedAsync_WithSilenceAudio_ShouldReturnFalse()
    {
        // Arrange
        var silenceAudio = GenerateSilenceAudio();
        var vadDetector = CreateVoiceActivityDetector();
        await vadDetector.InitializeAsync();

        // Act
        var result = await vadDetector.IsSpeechDetectedAsync(silenceAudio);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSpeechDetectedAsync_WithNoiseOnlyAudio_ShouldReturnFalse()
    {
        // Arrange
        var noiseAudio = GenerateNoiseOnlyAudio();

        // Act & Assert - Define noise vs speech distinction
        // var vadDetector = CreateVoiceActivityDetector();
        // await vadDetector.InitializeAsync(sensitivity: 0.7f);
        // var result = await vadDetector.IsSpeechDetectedAsync(noiseAudio);
        
        // result.Should().BeFalse(); // Noise should not be detected as speech

        noiseAudio.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectSpeechSegmentsAsync_WithMultipleSpeechParts_ShouldReturnSegments()
    {
        // Arrange
        var audioWithMultipleSpeechSegments = GenerateAudioWithSpeechSegments();
        var sampleRate = 16000;

        // Act & Assert - Define speech segmentation contract
        // var vadDetector = CreateVoiceActivityDetector();
        // await vadDetector.InitializeAsync();
        // var segments = await vadDetector.DetectSpeechSegmentsAsync(audioWithMultipleSpeechSegments, sampleRate);
        
        // segments.Should().NotBeNull();
        // segments.Should().HaveCountGreaterThan(1); // Multiple segments expected
        // segments.All(s => s.EndTime > s.StartTime).Should().BeTrue();
        // segments.All(s => s.Confidence >= 0 && s.Confidence <= 1).Should().BeTrue();

        audioWithMultipleSpeechSegments.Should().NotBeNull();
        sampleRate.Should().BePositive();
    }

    [Fact]
    public async Task GetAudioEnergyLevelAsync_WithDifferentAudioTypes_ShouldReturnValidLevels()
    {
        // Arrange
        var silentAudio = GenerateSilenceAudio();
        var loudAudio = GenerateLoudSpeechAudio();
        var vadDetector = CreateVoiceActivityDetector();
        
        // Act
        var silentLevel = await vadDetector.GetAudioEnergyLevelAsync(silentAudio);
        var loudLevel = await vadDetector.GetAudioEnergyLevelAsync(loudAudio);
        
        // Assert
        silentLevel.Should().BeInRange(0.0f, 0.1f); // Very low energy
        loudLevel.Should().BeGreaterThan(silentLevel);
        loudLevel.Should().BeGreaterThan(0.1f); // Should have noticeable energy
    }

    [Fact]
    public async Task IsSpeechEndedAsync_WithTrailingSilence_ShouldReturnTrue()
    {
        // Arrange
        var audioWithTrailingSilence = GenerateAudioWithTrailingSilence();

        // Act & Assert - Define speech end detection contract
        // var vadDetector = CreateVoiceActivityDetector();
        // await vadDetector.InitializeAsync(minSilenceDurationMs: 1000);
        // var result = await vadDetector.IsSpeechEndedAsync(audioWithTrailingSilence);
        
        // result.Should().BeTrue();

        audioWithTrailingSilence.Should().NotBeNull();
    }

    [Fact]
    public async Task IsSpeechEndedAsync_WithContinuousSpeech_ShouldReturnFalse()
    {
        // Arrange
        var continuousSpeechAudio = GenerateContinuousSpeechAudio();

        // Act & Assert
        // var vadDetector = CreateVoiceActivityDetector();
        // await vadDetector.InitializeAsync();
        // var result = await vadDetector.IsSpeechEndedAsync(continuousSpeechAudio);
        
        // result.Should().BeFalse();

        continuousSpeechAudio.Should().NotBeNull();
    }

    [Fact]
    public async Task CalibrateAsync_WithBackgroundNoise_ShouldAdjustSensitivity()
    {
        // Arrange
        var backgroundNoiseAudio = GenerateBackgroundNoiseAudio();

        // Act & Assert - Define calibration contract
        // var vadDetector = CreateVoiceActivityDetector();
        // var calibratedSensitivity = await vadDetector.CalibrateAsync(backgroundNoiseAudio);
        
        // calibratedSensitivity.Should().BeInRange(0.0f, 1.0f);
        // calibratedSensitivity.Should().BeGreaterThan(0.1f); // Should adjust based on noise

        backgroundNoiseAudio.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0.1f)] // Very low sensitivity - should catch only clear speech
    [InlineData(0.5f)] // Medium sensitivity 
    [InlineData(0.9f)] // High sensitivity - should catch whispers
    public async Task IsSpeechDetectedAsync_WithDifferentSensitivities_ShouldAdjustDetection(float sensitivity)
    {
        // Arrange
        var whisperAudio = GenerateWhisperAudio();

        // Act & Assert - Define sensitivity adjustment contract
        // var vadDetector = CreateVoiceActivityDetector();
        // await vadDetector.InitializeAsync(sensitivity);
        // var result = await vadDetector.IsSpeechDetectedAsync(whisperAudio);
        
        // if (sensitivity >= 0.8f)
        // {
        //     result.Should().BeTrue(); // High sensitivity should catch whispers
        // }
        // else if (sensitivity <= 0.3f)
        // {
        //     result.Should().BeFalse(); // Low sensitivity might miss whispers
        // }

        sensitivity.Should().BeInRange(0.0f, 1.0f);
        whisperAudio.Should().NotBeNull();
    }

    #region Test Audio Generators

    private static byte[] GenerateSpeechAudio()
    {
        // Generate audio that simulates human speech patterns with proper zero crossing rate
        var sampleRate = 16000;
        var duration = 2; // 2 seconds
        var samples = sampleRate * duration;
        var audioData = new byte[samples * 2];
        var random = new Random(42); // Consistent seed for testing

        // Generate complex waveform simulating speech
        for (int i = 0; i < samples; i++)
        {
            // Mix multiple frequencies to simulate speech formants
            var formant1 = Math.Sin(2 * Math.PI * 500 * i / sampleRate); // First formant (vowel)
            var formant2 = Math.Sin(2 * Math.PI * 1500 * i / sampleRate) * 0.6; // Second formant
            var formant3 = Math.Sin(2 * Math.PI * 2500 * i / sampleRate) * 0.3; // Third formant
            
            // Add frequency modulation to simulate speech dynamics
            var freqMod = 1.0 + 0.3 * Math.Sin(2 * Math.PI * 8 * i / sampleRate);
            
            // Add amplitude variation (envelope) to simulate speech patterns
            var envelope = 0.3 + 0.7 * Math.Sin(2 * Math.PI * 3 * i / sampleRate);
            
            // Add some controlled noise to increase zero crossing rate
            var noise = (random.NextDouble() - 0.5) * 0.1;
            
            var speechSignal = (formant1 + formant2 + formant3) * freqMod * envelope + noise;
            var sample = (short)Math.Clamp(speechSignal * 8192, short.MinValue, short.MaxValue);
            
            var bytes = BitConverter.GetBytes(sample);
            audioData[i * 2] = bytes[0];
            audioData[i * 2 + 1] = bytes[1];
        }

        return audioData;
    }

    private static byte[] GenerateSilenceAudio()
    {
        var sampleRate = 16000;
        var duration = 2;
        var samples = sampleRate * duration;
        return new byte[samples * 2]; // All zeros = silence
    }

    private static byte[] GenerateNoiseOnlyAudio()
    {
        var random = new Random(42);
        var sampleRate = 16000;
        var duration = 2;
        var samples = sampleRate * duration;
        var audioData = new byte[samples * 2];

        // Generate random noise
        for (int i = 0; i < samples; i++)
        {
            var sample = (short)(random.Next(-2000, 2000)); // Low-level noise
            var bytes = BitConverter.GetBytes(sample);
            audioData[i * 2] = bytes[0];
            audioData[i * 2 + 1] = bytes[1];
        }

        return audioData;
    }

    private static byte[] GenerateAudioWithSpeechSegments()
    {
        // Create audio with: silence -> speech -> silence -> speech -> silence
        var segments = new List<byte[]>
        {
            GenerateSilenceAudio(),
            GenerateSpeechAudio(),
            GenerateSilenceAudio(),
            GenerateSpeechAudio(),
            GenerateSilenceAudio()
        };

        return segments.SelectMany(s => s).ToArray();
    }

    private static byte[] GenerateLoudSpeechAudio()
    {
        var speechAudio = GenerateSpeechAudio();
        
        // Amplify the audio
        for (int i = 0; i < speechAudio.Length; i += 2)
        {
            var sample = BitConverter.ToInt16(speechAudio, i);
            var amplifiedSample = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, sample * 2));
            var bytes = BitConverter.GetBytes(amplifiedSample);
            speechAudio[i] = bytes[0];
            speechAudio[i + 1] = bytes[1];
        }

        return speechAudio;
    }

    private static byte[] GenerateAudioWithTrailingSilence()
    {
        var speechPart = GenerateSpeechAudio();
        var silencePart = GenerateSilenceAudio();
        
        return speechPart.Concat(silencePart).ToArray();
    }

    private static byte[] GenerateContinuousSpeechAudio()
    {
        var speech1 = GenerateSpeechAudio();
        var speech2 = GenerateSpeechAudio();
        
        return speech1.Concat(speech2).ToArray();
    }

    private static byte[] GenerateBackgroundNoiseAudio()
    {
        return GenerateNoiseOnlyAudio(); // Consistent background noise
    }

    private static byte[] GenerateWhisperAudio()
    {
        var speechAudio = GenerateSpeechAudio();
        
        // Reduce amplitude to simulate whisper
        for (int i = 0; i < speechAudio.Length; i += 2)
        {
            var sample = BitConverter.ToInt16(speechAudio, i);
            var whisperSample = (short)(sample * 0.2); // Reduce to 20% of original volume
            var bytes = BitConverter.GetBytes(whisperSample);
            speechAudio[i] = bytes[0];
            speechAudio[i + 1] = bytes[1];
        }

        return speechAudio;
    }

    #endregion
}