using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Services;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// Implementation of voice activity detection using energy and spectral analysis
/// </summary>
public class VoiceActivityDetector : IVoiceActivityDetector
{
    private readonly ILogger<VoiceActivityDetector> _logger;
    private float _sensitivity = 0.7f;
    private int _minSilenceDurationMs = 1000;
    private float _noiseFloor = 0.01f;
    private float _speechThreshold = 0.1f;

    public VoiceActivityDetector(ILogger<VoiceActivityDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync(float sensitivity = 0.7f, int minSilenceDurationMs = 1000)
    {
        try
        {
            _sensitivity = Math.Clamp(sensitivity, 0.0f, 1.0f);
            _minSilenceDurationMs = Math.Max(100, minSilenceDurationMs);
            
            // Calculate thresholds based on sensitivity
            _speechThreshold = 0.05f + (1.0f - _sensitivity) * 0.15f; // Higher sensitivity = lower threshold
            _noiseFloor = _speechThreshold * 0.1f;
            
            _logger.LogInformation("VoiceActivityDetector initialized with sensitivity={Sensitivity}, minSilenceDuration={MinSilenceDuration}ms, speechThreshold={SpeechThreshold}", 
                _sensitivity, _minSilenceDurationMs, _speechThreshold);
                
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize VoiceActivityDetector");
            throw;
        }
    }

    public async Task<bool> IsSpeechDetectedAsync(byte[] audioData)
    {
        if (audioData == null || audioData.Length < 2)
            return false;

        try
        {
            var energyLevel = await GetAudioEnergyLevelAsync(audioData);
            var isSpectralSpeech = await IsSpectralSpeechAsync(audioData);
            
            // Combine energy and spectral analysis
            var isSpeech = energyLevel > _speechThreshold && isSpectralSpeech;
            
            _logger.LogDebug("Speech detection: energy={Energy:F4}, threshold={Threshold:F4}, spectral={SpectralSpeech}, result={IsSpeech}", 
                energyLevel, _speechThreshold, isSpectralSpeech, isSpeech);
            
            return isSpeech;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect speech in audio data");
            return false;
        }
    }

    public async Task<IEnumerable<SpeechSegment>> DetectSpeechSegmentsAsync(byte[] audioData, int sampleRate)
    {
        if (audioData == null || audioData.Length < 2)
            return Enumerable.Empty<SpeechSegment>();

        try
        {
            var segments = new List<SpeechSegment>();
            var windowSizeMs = 50; // 50ms windows
            var windowSizeSamples = (sampleRate * windowSizeMs) / 1000;
            var windowSizeBytes = windowSizeSamples * 2; // 16-bit samples = 2 bytes each
            
            bool inSpeechSegment = false;
            int speechStartIndex = 0;
            var speechStartTime = TimeSpan.Zero;
            
            for (int i = 0; i < audioData.Length - windowSizeBytes; i += windowSizeBytes)
            {
                var windowData = audioData.Skip(i).Take(windowSizeBytes).ToArray();
                var currentTime = TimeSpan.FromSeconds((double)i / (sampleRate * 2));
                
                var isSpeechWindow = await IsSpeechDetectedAsync(windowData);
                var energyLevel = await GetAudioEnergyLevelAsync(windowData);
                
                if (!inSpeechSegment && isSpeechWindow)
                {
                    // Start of speech segment
                    inSpeechSegment = true;
                    speechStartIndex = i;
                    speechStartTime = currentTime;
                }
                else if (inSpeechSegment && !isSpeechWindow)
                {
                    // Potential end of speech segment - verify with silence duration
                    var silenceEnd = await FindEndOfSilenceAsync(audioData, i, sampleRate);
                    var silenceDuration = TimeSpan.FromSeconds((double)(silenceEnd - i) / (sampleRate * 2));
                    
                    if (silenceDuration.TotalMilliseconds >= _minSilenceDurationMs)
                    {
                        // End of speech segment
                        var endTime = currentTime;
                        var avgEnergyLevel = await CalculateAverageEnergyAsync(audioData, speechStartIndex, i);
                        
                        var segment = new SpeechSegment
                        {
                            StartTime = speechStartTime,
                            EndTime = endTime,
                            Confidence = Math.Min(1.0f, avgEnergyLevel * 2), // Convert energy to confidence
                            AverageEnergyLevel = avgEnergyLevel,
                            AudioDataStartIndex = speechStartIndex,
                            AudioDataEndIndex = i
                        };
                        
                        segments.Add(segment);
                        inSpeechSegment = false;
                    }
                }
            }
            
            // Handle case where speech continues to the end
            if (inSpeechSegment)
            {
                var endTime = TimeSpan.FromSeconds((double)audioData.Length / (sampleRate * 2));
                var avgEnergyLevel = await CalculateAverageEnergyAsync(audioData, speechStartIndex, audioData.Length);
                
                var segment = new SpeechSegment
                {
                    StartTime = speechStartTime,
                    EndTime = endTime,
                    Confidence = Math.Min(1.0f, avgEnergyLevel * 2),
                    AverageEnergyLevel = avgEnergyLevel,
                    AudioDataStartIndex = speechStartIndex,
                    AudioDataEndIndex = audioData.Length
                };
                
                segments.Add(segment);
            }
            
            _logger.LogDebug("Detected {SegmentCount} speech segments in audio data", segments.Count);
            return segments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect speech segments");
            return Enumerable.Empty<SpeechSegment>();
        }
    }

    public async Task<float> GetAudioEnergyLevelAsync(byte[] audioData)
    {
        if (audioData == null || audioData.Length < 2)
            return 0f;

        try
        {
            double sumSquares = 0;
            int sampleCount = 0;
            
            // Calculate RMS (Root Mean Square) energy
            for (int i = 0; i < audioData.Length - 1; i += 2)
            {
                var sample = BitConverter.ToInt16(audioData, i);
                var normalizedSample = sample / (double)short.MaxValue;
                sumSquares += normalizedSample * normalizedSample;
                sampleCount++;
            }
            
            if (sampleCount == 0)
                return 0f;
            
            var rms = Math.Sqrt(sumSquares / sampleCount);
            var energyLevel = (float)Math.Min(1.0, rms);
            
            await Task.CompletedTask;
            return energyLevel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate audio energy level");
            return 0f;
        }
    }

    public async Task<bool> IsSpeechEndedAsync(byte[] audioData)
    {
        if (audioData == null || audioData.Length < 2)
            return true;

        try
        {
            var energyLevel = await GetAudioEnergyLevelAsync(audioData);
            
            // Consider speech ended if energy is below noise floor
            var speechEnded = energyLevel <= _noiseFloor;
            
            _logger.LogDebug("Speech end detection: energy={Energy:F4}, noiseFloor={NoiseFloor:F4}, speechEnded={SpeechEnded}", 
                energyLevel, _noiseFloor, speechEnded);
            
            return speechEnded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine if speech has ended");
            return true;
        }
    }

    public async Task<float> CalibrateAsync(byte[] backgroundAudio)
    {
        if (backgroundAudio == null || backgroundAudio.Length < 2)
            return _sensitivity;

        try
        {
            var backgroundEnergy = await GetAudioEnergyLevelAsync(backgroundAudio);
            
            // Adjust thresholds based on background noise level
            _noiseFloor = backgroundEnergy * 1.5f; // Set noise floor above background
            _speechThreshold = _noiseFloor * 3.0f; // Speech threshold above noise floor
            
            // Adjust sensitivity based on noise level
            var calibratedSensitivity = backgroundEnergy > 0.1f 
                ? Math.Max(0.3f, _sensitivity - backgroundEnergy) // Reduce sensitivity in noisy environments
                : Math.Min(0.9f, _sensitivity + 0.1f); // Increase sensitivity in quiet environments
            
            _sensitivity = calibratedSensitivity;
            
            _logger.LogInformation("VAD calibrated: backgroundEnergy={BackgroundEnergy:F4}, noiseFloor={NoiseFloor:F4}, speechThreshold={SpeechThreshold:F4}, sensitivity={Sensitivity:F2}", 
                backgroundEnergy, _noiseFloor, _speechThreshold, _sensitivity);
            
            return _sensitivity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calibrate VAD with background noise");
            return _sensitivity;
        }
    }

    #region Private Helper Methods

    private async Task<bool> IsSpectralSpeechAsync(byte[] audioData)
    {
        if (audioData == null || audioData.Length < 2)
            return false;

        try
        {
            // Simple spectral analysis: check for energy in speech frequency range
            // This is a basic implementation - more sophisticated VAD would use FFT
            
            var samples = new List<double>();
            for (int i = 0; i < audioData.Length - 1; i += 2)
            {
                var sample = BitConverter.ToInt16(audioData, i);
                samples.Add(sample / (double)short.MaxValue);
            }
            
            // Calculate zero crossing rate (indicator of speech vs noise)
            var zeroCrossings = 0;
            for (int i = 1; i < samples.Count; i++)
            {
                if ((samples[i] > 0 && samples[i - 1] < 0) || (samples[i] < 0 && samples[i - 1] > 0))
                {
                    zeroCrossings++;
                }
            }
            
            var zeroCrossingRate = (double)zeroCrossings / samples.Count;
            
            // Speech typically has ZCR between 0.1 and 0.5
            // Pure tones have very low ZCR, noise has very high ZCR
            var isSpectralSpeech = zeroCrossingRate >= 0.05 && zeroCrossingRate <= 0.8;
            
            await Task.CompletedTask;
            return isSpectralSpeech;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed spectral analysis, defaulting to false");
            return false;
        }
    }

    private async Task<int> FindEndOfSilenceAsync(byte[] audioData, int startIndex, int sampleRate)
    {
        var windowSizeBytes = (sampleRate * 10) / 1000 * 2; // 10ms windows
        
        for (int i = startIndex; i < audioData.Length - windowSizeBytes; i += windowSizeBytes)
        {
            var windowData = audioData.Skip(i).Take(windowSizeBytes).ToArray();
            var isSpeechWindow = await IsSpeechDetectedAsync(windowData);
            
            if (isSpeechWindow)
            {
                return i; // Found speech, end of silence
            }
        }
        
        return audioData.Length; // Silence continues to end
    }

    private async Task<float> CalculateAverageEnergyAsync(byte[] audioData, int startIndex, int endIndex)
    {
        if (startIndex >= endIndex || endIndex > audioData.Length)
            return 0f;

        var segmentData = audioData.Skip(startIndex).Take(endIndex - startIndex).ToArray();
        return await GetAudioEnergyLevelAsync(segmentData);
    }

    #endregion
}