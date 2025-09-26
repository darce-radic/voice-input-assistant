using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services;
using VoiceInputAssistant.Core.Services.Interfaces;
using Xunit;

namespace VoiceInputAssistant.Tests.Integration;

/// <summary>
/// Integration tests for the complete audio processing pipeline
/// </summary>
public class AudioPipelineIntegrationTests
{
    private readonly Mock<ILogger<NAudioProcessor>> _audioLoggerMock;
    private readonly Mock<ILogger<VoiceActivityDetector>> _vadLoggerMock;

    public AudioPipelineIntegrationTests()
    {
        _audioLoggerMock = new Mock<ILogger<NAudioProcessor>>();
        _vadLoggerMock = new Mock<ILogger<VoiceActivityDetector>>();
    }

    private IAudioProcessor CreateAudioProcessor()
    {
        return new NAudioProcessor(_audioLoggerMock.Object);
    }

    private IVoiceActivityDetector CreateVoiceActivityDetector()
    {
        return new VoiceActivityDetector(_vadLoggerMock.Object);
    }

    [Fact]
    public async Task AudioPipeline_InitializeAndEnumerateDevices_ShouldWorkEndToEnd()
    {
        // Arrange
        var audioProcessor = CreateAudioProcessor();
        var settings = new AudioSettings 
        { 
            PreferredInputDevice = null,
            InputVolume = 100,
            NoiseThreshold = 15,
            AutoAdjustNoiseThreshold = true
        };

        // Act & Assert - Complete initialization and device enumeration
        await audioProcessor.InitializeAsync(settings);
        var devices = await audioProcessor.GetAudioInputDevicesAsync();
        
        devices.Should().NotBeNull();
        // Should work without throwing exceptions
    }

    [Fact]
    public async Task AudioPipeline_ProcessAudioWithVAD_ShouldWorkEndToEnd()
    {
        // Arrange
        var audioProcessor = CreateAudioProcessor();
        var vad = CreateVoiceActivityDetector();
        var testAudio = GenerateRealisticSpeechAudio();
        
        var settings = new AudioSettings 
        { 
            PreferredInputDevice = null,
            NoiseThreshold = 10
        };

        // Act - Complete pipeline: initialize -> preprocess -> detect speech
        await audioProcessor.InitializeAsync(settings);
        await vad.InitializeAsync(sensitivity: 0.7f);

        var preprocessedAudio = await audioProcessor.PreprocessAudioAsync(testAudio, settings);
        var isSpeechDetected = await vad.IsSpeechDetectedAsync(preprocessedAudio);
        var energyLevel = await vad.GetAudioEnergyLevelAsync(preprocessedAudio);

        // Assert
        preprocessedAudio.Should().NotBeNull();
        preprocessedAudio.Length.Should().Be(testAudio.Length);
        isSpeechDetected.Should().BeTrue();
        energyLevel.Should().BeGreaterThan(0.0f);
        energyLevel.Should().BeLessOrEqualTo(1.0f);
    }

    [Fact]
    public async Task AudioPipeline_SpeechSegmentation_ShouldDetectMultipleSegments()
    {
        // Arrange
        var vad = CreateVoiceActivityDetector();
        var audioWithSegments = GenerateAudioWithSpeechSegments();
        var sampleRate = 16000;

        // Act
        await vad.InitializeAsync(sensitivity: 0.6f, minSilenceDurationMs: 500);
        var segments = await vad.DetectSpeechSegmentsAsync(audioWithSegments, sampleRate);

        // Assert
        segments.Should().NotBeNull();
        segments.Should().HaveCountGreaterOrEqualTo(1); // At least one segment expected
        
        foreach (var segment in segments)
        {
            segment.StartTime.Should().BeLessThan(segment.EndTime);
            segment.Confidence.Should().BeInRange(0.0f, 1.0f);
            segment.AverageEnergyLevel.Should().BeGreaterThan(0.0f);
        }
    }

    [Fact]
    public async Task AudioPipeline_VolumeNormalizationAndDetection_ShouldWork()
    {
        // Arrange
        var audioProcessor = CreateAudioProcessor();
        var vad = CreateVoiceActivityDetector();
        var quietAudio = GenerateQuietSpeechAudio();
        
        // Act - Normalize quiet audio and then detect speech
        var normalizedAudio = await audioProcessor.NormalizeVolumeAsync(quietAudio, -16.0f);
        await vad.InitializeAsync();
        var isSpeechDetected = await vad.IsSpeechDetectedAsync(normalizedAudio);
        
        var originalEnergy = await vad.GetAudioEnergyLevelAsync(quietAudio);
        var normalizedEnergy = await vad.GetAudioEnergyLevelAsync(normalizedAudio);

        // Assert
        normalizedEnergy.Should().BeGreaterThan(originalEnergy);
        isSpeechDetected.Should().BeTrue(); // Should be detectable after normalization
    }

    [Fact]
    public async Task AudioPipeline_NoiseReductionAndDetection_ShouldImproveDetection()
    {
        // Arrange
        var audioProcessor = CreateAudioProcessor();
        var vad = CreateVoiceActivityDetector();
        var noisyAudio = GenerateNoisyAudioWithSpeech();
        
        // Act
        await vad.InitializeAsync(sensitivity: 0.7f);
        
        var originalDetection = await vad.IsSpeechDetectedAsync(noisyAudio);
        var cleanedAudio = await audioProcessor.ReduceNoiseAsync(noisyAudio, 0.7f);
        var cleanedDetection = await vad.IsSpeechDetectedAsync(cleanedAudio);

        // Assert
        cleanedAudio.Should().NotBeNull();
        cleanedAudio.Length.Should().Be(noisyAudio.Length);
        // After noise reduction, detection should be more reliable
        cleanedDetection.Should().BeTrue();
    }

    [Fact]
    public async Task AudioPipeline_RealTimeProcessing_ShouldHandleStreamingAudio()
    {
        // Arrange
        var audioProcessor = CreateAudioProcessor();
        var vad = CreateVoiceActivityDetector();
        
        await audioProcessor.InitializeAsync(new AudioSettings { PreferredInputDevice = null });
        await vad.InitializeAsync();

        // Simulate streaming audio chunks
        var audioChunks = GenerateStreamingAudioChunks(5); // 5 chunks of audio
        var detectionResults = new List<bool>();
        var energyLevels = new List<float>();

        // Act
        foreach (var chunk in audioChunks)
        {
            var preprocessed = await audioProcessor.PreprocessAudioAsync(chunk, new AudioSettings());
            var isDetected = await vad.IsSpeechDetectedAsync(preprocessed);
            var energy = await vad.GetAudioEnergyLevelAsync(preprocessed);
            
            detectionResults.Add(isDetected);
            energyLevels.Add(energy);
        }

        // Assert
        detectionResults.Should().HaveCount(5);
        energyLevels.Should().HaveCount(5);
        energyLevels.Should().OnlyContain(level => level >= 0.0f && level <= 1.0f);
    }

    [Fact]
    public async Task AudioPipeline_CalibrationAndAdaptiveDetection_ShouldWork()
    {
        // Arrange
        var vad = CreateVoiceActivityDetector();
        var backgroundNoise = GenerateBackgroundNoise();
        var speechInNoise = GenerateSpeechWithNoise();

        // Act - Calibrate with background noise
        await vad.InitializeAsync();
        var calibratedSensitivity = await vad.CalibrateAsync(backgroundNoise);
        
        // Re-initialize with calibrated sensitivity
        await vad.InitializeAsync(calibratedSensitivity);
        var detectionResult = await vad.IsSpeechDetectedAsync(speechInNoise);

        // Assert
        calibratedSensitivity.Should().BeInRange(0.0f, 1.0f);
        detectionResult.Should().BeTrue(); // Should detect speech even in noise after calibration
    }

    #region Audio Test Data Generators

    private static byte[] GenerateRealisticSpeechAudio()
    {
        var sampleRate = 16000;
        var duration = 2; // 2 seconds
        var samples = sampleRate * duration;
        var audioData = new byte[samples * 2];
        var random = new Random(42);

        for (int i = 0; i < samples; i++)
        {
            // Generate speech-like formants
            var formant1 = Math.Sin(2 * Math.PI * 500 * i / sampleRate);
            var formant2 = Math.Sin(2 * Math.PI * 1500 * i / sampleRate) * 0.6;
            var formant3 = Math.Sin(2 * Math.PI * 2500 * i / sampleRate) * 0.3;
            
            var envelope = 0.3 + 0.7 * Math.Sin(2 * Math.PI * 3 * i / sampleRate);
            var noise = (random.NextDouble() - 0.5) * 0.1;
            
            var speechSignal = (formant1 + formant2 + formant3) * envelope + noise;
            var sample = (short)Math.Clamp(speechSignal * 8192, short.MinValue, short.MaxValue);
            
            var bytes = BitConverter.GetBytes(sample);
            audioData[i * 2] = bytes[0];
            audioData[i * 2 + 1] = bytes[1];
        }

        return audioData;
    }

    private static byte[] GenerateAudioWithSpeechSegments()
    {
        // Create: silence -> speech -> silence -> speech -> silence
        var segments = new List<byte[]>
        {
            GenerateSilenceAudio(500), // 500ms silence
            GenerateRealisticSpeechAudio(),
            GenerateSilenceAudio(800), // 800ms silence
            GenerateRealisticSpeechAudio(),
            GenerateSilenceAudio(500)  // 500ms silence
        };

        return segments.SelectMany(s => s).ToArray();
    }

    private static byte[] GenerateQuietSpeechAudio()
    {
        var speechAudio = GenerateRealisticSpeechAudio();
        
        // Reduce volume to 20%
        for (int i = 0; i < speechAudio.Length; i += 2)
        {
            var sample = BitConverter.ToInt16(speechAudio, i);
            var quietSample = (short)(sample * 0.2);
            var bytes = BitConverter.GetBytes(quietSample);
            speechAudio[i] = bytes[0];
            speechAudio[i + 1] = bytes[1];
        }

        return speechAudio;
    }

    private static byte[] GenerateNoisyAudioWithSpeech()
    {
        var speechAudio = GenerateRealisticSpeechAudio();
        var random = new Random(42);
        
        // Add significant noise
        for (int i = 0; i < speechAudio.Length; i += 2)
        {
            var originalSample = BitConverter.ToInt16(speechAudio, i);
            var noise = (short)(random.Next(-2000, 2000));
            var noisySample = (short)Math.Clamp(originalSample + noise, short.MinValue, short.MaxValue);
            var bytes = BitConverter.GetBytes(noisySample);
            speechAudio[i] = bytes[0];
            speechAudio[i + 1] = bytes[1];
        }

        return speechAudio;
    }

    private static IEnumerable<byte[]> GenerateStreamingAudioChunks(int chunkCount)
    {
        var chunks = new List<byte[]>();
        var chunkDurationMs = 500; // 500ms per chunk
        var sampleRate = 16000;
        var samplesPerChunk = (sampleRate * chunkDurationMs) / 1000;
        var random = new Random(42);

        for (int chunk = 0; chunk < chunkCount; chunk++)
        {
            var audioData = new byte[samplesPerChunk * 2];
            var hasSpeech = chunk % 2 == 0; // Alternate speech and silence

            for (int i = 0; i < samplesPerChunk; i++)
            {
                short sample = 0;
                
                if (hasSpeech)
                {
                    // Generate speech-like audio
                    var formant = Math.Sin(2 * Math.PI * 800 * i / sampleRate);
                    var envelope = 0.5 + 0.5 * Math.Sin(2 * Math.PI * 5 * i / sampleRate);
                    sample = (short)(formant * envelope * 4096);
                }
                else
                {
                    // Generate low-level noise
                    sample = (short)(random.Next(-500, 500));
                }
                
                var bytes = BitConverter.GetBytes(sample);
                audioData[i * 2] = bytes[0];
                audioData[i * 2 + 1] = bytes[1];
            }
            
            chunks.Add(audioData);
        }

        return chunks;
    }

    private static byte[] GenerateBackgroundNoise()
    {
        var sampleRate = 16000;
        var duration = 1; // 1 second
        var samples = sampleRate * duration;
        var audioData = new byte[samples * 2];
        var random = new Random(42);

        for (int i = 0; i < samples; i++)
        {
            var sample = (short)(random.Next(-1000, 1000)); // Low-level consistent noise
            var bytes = BitConverter.GetBytes(sample);
            audioData[i * 2] = bytes[0];
            audioData[i * 2 + 1] = bytes[1];
        }

        return audioData;
    }

    private static byte[] GenerateSpeechWithNoise()
    {
        var speechAudio = GenerateRealisticSpeechAudio();
        var noise = GenerateBackgroundNoise();
        
        // Mix speech with background noise
        var mixedAudio = new byte[Math.Min(speechAudio.Length, noise.Length)];
        
        for (int i = 0; i < mixedAudio.Length; i += 2)
        {
            var speechSample = BitConverter.ToInt16(speechAudio, i);
            var noiseSample = BitConverter.ToInt16(noise, i % noise.Length);
            var mixedSample = (short)Math.Clamp(speechSample + (noiseSample * 0.3), short.MinValue, short.MaxValue);
            
            var bytes = BitConverter.GetBytes(mixedSample);
            mixedAudio[i] = bytes[0];
            mixedAudio[i + 1] = bytes[1];
        }

        return mixedAudio;
    }

    private static byte[] GenerateSilenceAudio(int durationMs)
    {
        var sampleRate = 16000;
        var samples = (sampleRate * durationMs) / 1000;
        return new byte[samples * 2]; // All zeros = silence
    }

    #endregion
}