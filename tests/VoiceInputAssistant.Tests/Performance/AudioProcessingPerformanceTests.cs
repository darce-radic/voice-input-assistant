using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services;
using VoiceInputAssistant.Core.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace VoiceInputAssistant.Tests.Performance;

/// <summary>
/// Performance and stress tests for audio processing components
/// </summary>
public class AudioProcessingPerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<NAudioProcessor>> _audioLoggerMock;
    private readonly Mock<ILogger<VoiceActivityDetector>> _vadLoggerMock;

    public AudioProcessingPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _audioLoggerMock = new Mock<ILogger<NAudioProcessor>>();
        _vadLoggerMock = new Mock<ILogger<VoiceActivityDetector>>();
    }

    private IAudioProcessor CreateAudioProcessor() => new NAudioProcessor(_audioLoggerMock.Object);
    private IVoiceActivityDetector CreateVoiceActivityDetector() => new VoiceActivityDetector(_vadLoggerMock.Object);

    [Fact]
    public async Task AudioProcessor_InitializationPerformance_ShouldBeFast()
    {
        // Arrange
        var processor = CreateAudioProcessor();
        var settings = new AudioSettings { PreferredInputDevice = null };
        var stopwatch = Stopwatch.StartNew();

        // Act
        await processor.InitializeAsync(settings);
        stopwatch.Stop();

        // Assert
        var initTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Audio processor initialization took: {initTime}ms");
        initTime.Should().BeLessThan(2000); // Should initialize within 2 seconds
    }

    [Fact]
    public async Task VoiceActivityDetector_InitializationPerformance_ShouldBeFast()
    {
        // Arrange
        var vad = CreateVoiceActivityDetector();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await vad.InitializeAsync();
        stopwatch.Stop();

        // Assert
        var initTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"VAD initialization took: {initTime}ms");
        initTime.Should().BeLessThan(100); // Should initialize within 100ms
    }

    [Fact]
    public async Task AudioProcessing_ThroughputPerformance_ShouldHandleHighVolume()
    {
        // Arrange
        var processor = CreateAudioProcessor();
        await processor.InitializeAsync(new AudioSettings { PreferredInputDevice = null });

        var testAudioChunks = GenerateTestAudioChunks(100, 1000); // 100 chunks of 1000ms each
        var processedCount = 0;
        var stopwatch = Stopwatch.StartNew();

        // Act
        foreach (var chunk in testAudioChunks)
        {
            await processor.PreprocessAudioAsync(chunk, new AudioSettings());
            processedCount++;
        }
        stopwatch.Stop();

        // Assert
        var throughputMbps = (processedCount * 16000 * 2) / (stopwatch.ElapsedMilliseconds / 1000.0) / (1024 * 1024); // MB/s
        _output.WriteLine($"Processed {processedCount} chunks in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Throughput: {throughputMbps:F2} MB/s");
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should process 100 seconds of audio in under 30 seconds
        throughputMbps.Should().BeGreaterThan(0.1); // At least 0.1 MB/s throughput
    }

    [Fact]
    public async Task VoiceActivityDetection_ThroughputPerformance_ShouldHandleRealTime()
    {
        // Arrange
        var vad = CreateVoiceActivityDetector();
        await vad.InitializeAsync();

        var testAudioChunks = GenerateRealtimeAudioChunks(200, 50); // 200 chunks of 50ms each (10 seconds)
        var detectionResults = new List<bool>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        foreach (var chunk in testAudioChunks)
        {
            var result = await vad.IsSpeechDetectedAsync(chunk);
            detectionResults.Add(result);
        }
        stopwatch.Stop();

        // Assert
        var avgProcessingTime = (double)stopwatch.ElapsedMilliseconds / testAudioChunks.Count();
        _output.WriteLine($"Processed {testAudioChunks.Count()} chunks in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average processing time per 50ms chunk: {avgProcessingTime:F2}ms");
        
        avgProcessingTime.Should().BeLessThan(25); // Should process 50ms chunks in under 25ms (real-time capable)
        detectionResults.Should().HaveCount(200);
    }

    [Fact]
    public async Task AudioPipeline_ConcurrentProcessing_ShouldHandleMultipleStreams()
    {
        // Arrange
        const int concurrentStreams = 5;
        var tasks = new List<Task>();
        var results = new List<TimeSpan>();
        var lockObject = new object();

        // Act
        for (int i = 0; i < concurrentStreams; i++)
        {
            var streamId = i;
            tasks.Add(Task.Run(async () =>
            {
                var processor = CreateAudioProcessor();
                var vad = CreateVoiceActivityDetector();
                var stopwatch = Stopwatch.StartNew();

                await processor.InitializeAsync(new AudioSettings { PreferredInputDevice = null });
                await vad.InitializeAsync();

                var audioChunks = GenerateTestAudioChunks(20, 500); // 20 chunks of 500ms
                foreach (var chunk in audioChunks)
                {
                    var preprocessed = await processor.PreprocessAudioAsync(chunk, new AudioSettings());
                    await vad.IsSpeechDetectedAsync(preprocessed);
                }

                stopwatch.Stop();
                lock (lockObject)
                {
                    results.Add(stopwatch.Elapsed);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentStreams);
        var maxTime = results.Max().TotalSeconds;
        var avgTime = results.Average(r => r.TotalSeconds);
        
        _output.WriteLine($"Concurrent processing - Max time: {maxTime:F2}s, Avg time: {avgTime:F2}s");
        maxTime.Should().BeLessThan(30); // All streams should complete within 30 seconds
        avgTime.Should().BeLessThan(20); // Average should be under 20 seconds
    }

    [Fact]
    public async Task AudioProcessor_MemoryUsage_ShouldBeStable()
    {
        // Arrange
        var processor = CreateAudioProcessor();
        await processor.InitializeAsync(new AudioSettings { PreferredInputDevice = null });

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(false);

        // Act - Process many audio chunks
        for (int i = 0; i < 1000; i++)
        {
            var audioChunk = GenerateTestAudio(100); // 100ms chunks
            await processor.PreprocessAudioAsync(audioChunk, new AudioSettings());
            
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);
        
        _output.WriteLine($"Initial memory: {initialMemory / (1024.0 * 1024.0):F2} MB");
        _output.WriteLine($"Final memory: {finalMemory / (1024.0 * 1024.0):F2} MB");
        _output.WriteLine($"Memory increase: {memoryIncreaseMB:F2} MB");
        
        memoryIncreaseMB.Should().BeLessThan(50); // Memory increase should be less than 50MB
    }

    [Fact]
    public async Task VoiceActivityDetector_MemoryUsage_ShouldBeStable()
    {
        // Arrange
        var vad = CreateVoiceActivityDetector();
        await vad.InitializeAsync();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetTotalMemory(false);

        // Act - Process many audio chunks
        for (int i = 0; i < 2000; i++)
        {
            var audioChunk = GenerateTestAudio(50); // 50ms chunks
            await vad.IsSpeechDetectedAsync(audioChunk);
            await vad.GetAudioEnergyLevelAsync(audioChunk);
            
            if (i % 200 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);
        
        _output.WriteLine($"VAD Memory increase: {memoryIncreaseMB:F2} MB");
        memoryIncreaseMB.Should().BeLessThan(20); // Memory increase should be less than 20MB
    }

    [Fact]
    public async Task AudioPipeline_LongRunningStressTest_ShouldMaintainPerformance()
    {
        // Arrange
        var processor = CreateAudioProcessor();
        var vad = CreateVoiceActivityDetector();
        await processor.InitializeAsync(new AudioSettings { PreferredInputDevice = null });
        await vad.InitializeAsync();

        var processingTimes = new List<double>();
        const int totalChunks = 500; // Simulate 25 seconds of audio (50ms chunks)

        // Act - Long running processing
        for (int i = 0; i < totalChunks; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var audioChunk = GenerateTestAudio(50); // 50ms chunk
            var preprocessed = await processor.PreprocessAudioAsync(audioChunk, new AudioSettings());
            await vad.IsSpeechDetectedAsync(preprocessed);
            
            stopwatch.Stop();
            processingTimes.Add(stopwatch.Elapsed.TotalMilliseconds);

            // Simulate real-time constraints
            if (i % 100 == 0)
            {
                _output.WriteLine($"Processed {i + 1}/{totalChunks} chunks");
            }
        }

        // Assert
        var avgProcessingTime = processingTimes.Average();
        var maxProcessingTime = processingTimes.Max();
        var performance95thPercentile = processingTimes.OrderBy(x => x).Skip((int)(totalChunks * 0.95)).First();

        _output.WriteLine($"Average processing time: {avgProcessingTime:F2}ms");
        _output.WriteLine($"Max processing time: {maxProcessingTime:F2}ms");
        _output.WriteLine($"95th percentile: {performance95thPercentile:F2}ms");

        avgProcessingTime.Should().BeLessThan(25); // Average should be well under real-time
        performance95thPercentile.Should().BeLessThan(40); // 95% of operations should be fast
        maxProcessingTime.Should().BeLessThan(100); // Even worst case should be reasonable
    }

    [Fact]
    public async Task AudioProcessor_DeviceEnumeration_PerformanceTest()
    {
        // Arrange
        var processor = CreateAudioProcessor();
        await processor.InitializeAsync(new AudioSettings { PreferredInputDevice = null });

        var enumerationTimes = new List<double>();

        // Act - Multiple device enumerations
        for (int i = 0; i < 50; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var devices = await processor.GetAudioInputDevicesAsync();
            stopwatch.Stop();
            
            enumerationTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        // Assert
        var avgTime = enumerationTimes.Average();
        var maxTime = enumerationTimes.Max();
        
        _output.WriteLine($"Device enumeration - Avg: {avgTime:F2}ms, Max: {maxTime:F2}ms");
        avgTime.Should().BeLessThan(100); // Should be fast on average
        maxTime.Should().BeLessThan(500); // Even slowest should be reasonable
    }

    [Fact]
    public async Task VoiceActivityDetector_SpeechSegmentation_PerformanceTest()
    {
        // Arrange
        var vad = CreateVoiceActivityDetector();
        await vad.InitializeAsync();
        
        var longAudioSample = GenerateComplexAudioWithMultipleSegments(60000); // 60 seconds
        var stopwatch = Stopwatch.StartNew();

        // Act
        var segments = await vad.DetectSpeechSegmentsAsync(longAudioSample, 16000);
        stopwatch.Stop();

        // Assert
        var processingTime = stopwatch.Elapsed.TotalSeconds;
        var audioLength = 60.0; // seconds
        var realTimeRatio = processingTime / audioLength;

        _output.WriteLine($"Segmentation of 60s audio took: {processingTime:F2}s");
        _output.WriteLine($"Real-time ratio: {realTimeRatio:F2}x");
        
        segments.Should().NotBeNull();
        realTimeRatio.Should().BeLessThan(0.5); // Should process faster than 0.5x real-time
        processingTime.Should().BeLessThan(30); // Should complete within 30 seconds
    }

    #region Test Data Generators

    private static IEnumerable<byte[]> GenerateTestAudioChunks(int chunkCount, int durationMs)
    {
        var chunks = new List<byte[]>();
        for (int i = 0; i < chunkCount; i++)
        {
            chunks.Add(GenerateTestAudio(durationMs));
        }
        return chunks;
    }

    private static IEnumerable<byte[]> GenerateRealtimeAudioChunks(int chunkCount, int durationMs)
    {
        var chunks = new List<byte[]>();
        var random = new Random(42);
        
        for (int i = 0; i < chunkCount; i++)
        {
            var hasSpeech = i % 3 != 0; // Mix of speech and silence
            chunks.Add(GenerateTestAudio(durationMs, hasSpeech, random));
        }
        return chunks;
    }

    private static byte[] GenerateTestAudio(int durationMs, bool hasSpeech = true, Random? random = null)
    {
        random ??= new Random(42);
        var sampleRate = 16000;
        var samples = (sampleRate * durationMs) / 1000;
        var audioData = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            short sample = 0;
            
            if (hasSpeech)
            {
                // Generate speech-like formants
                var formant1 = Math.Sin(2 * Math.PI * 500 * i / sampleRate);
                var formant2 = Math.Sin(2 * Math.PI * 1500 * i / sampleRate) * 0.6;
                var envelope = 0.3 + 0.7 * Math.Sin(2 * Math.PI * 3 * i / sampleRate);
                var noise = (random.NextDouble() - 0.5) * 0.1;
                
                var speechSignal = (formant1 + formant2) * envelope + noise;
                sample = (short)Math.Clamp(speechSignal * 4096, short.MinValue, short.MaxValue);
            }
            else
            {
                // Generate low-level noise
                sample = (short)(random.Next(-1000, 1000));
            }
            
            var bytes = BitConverter.GetBytes(sample);
            audioData[i * 2] = bytes[0];
            audioData[i * 2 + 1] = bytes[1];
        }

        return audioData;
    }

    private static byte[] GenerateComplexAudioWithMultipleSegments(int durationMs)
    {
        var segments = new List<byte[]>();
        var totalDuration = 0;
        var random = new Random(42);

        while (totalDuration < durationMs)
        {
            var segmentDuration = random.Next(500, 3000); // 0.5 to 3 seconds
            var isSpeech = random.NextDouble() > 0.3; // 70% speech, 30% silence
            
            if (totalDuration + segmentDuration > durationMs)
            {
                segmentDuration = durationMs - totalDuration;
            }

            segments.Add(GenerateTestAudio(segmentDuration, isSpeech, random));
            totalDuration += segmentDuration;
        }

        return segments.SelectMany(s => s).ToArray();
    }

    #endregion
}