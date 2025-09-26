using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services;

namespace VoiceInputAssistant.Core.Services;

/// <summary>
/// NAudio-based implementation of audio processing for Windows
/// </summary>
public class NAudioProcessor : IAudioProcessor, IDisposable
{
    private readonly ILogger<NAudioProcessor> _logger;
    private MMDeviceEnumerator? _deviceEnumerator;
    private WaveInEvent? _waveIn;
    private AudioSettings? _currentSettings;
    private bool _disposed;

    public NAudioProcessor(ILogger<NAudioProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync(AudioSettings settings)
    {
        try
        {
            _currentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            _deviceEnumerator = new MMDeviceEnumerator();
            
            _logger.LogInformation("NAudioProcessor initialized with settings: InputVolume={InputVolume}, NoiseThreshold={NoiseThreshold}", 
                settings.InputVolume, settings.NoiseThreshold);
                
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize NAudioProcessor");
            throw;
        }
    }

    public async Task<byte[]> CaptureAudioAsync(AudioDevice device, int durationMs = 0)
    {
        if (_deviceEnumerator == null)
            throw new InvalidOperationException("NAudioProcessor not initialized. Call InitializeAsync first.");

        try
        {
            var mmDevice = GetMMDevice(device);
            if (mmDevice == null)
            {
                throw new ArgumentException($"Audio device with ID '{device.Id}' not found", nameof(device));
            }

            var capturedData = new List<byte>();
            var waveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono

            using var waveIn = new WasapiCapture(mmDevice);
            waveIn.WaveFormat = waveFormat;
            
            waveIn.DataAvailable += (sender, e) =>
            {
                capturedData.AddRange(e.Buffer.Take(e.BytesRecorded));
            };

            waveIn.StartRecording();

            if (durationMs > 0)
            {
                await Task.Delay(durationMs);
            }
            else
            {
                // For continuous capture, capture 1 second by default in this implementation
                await Task.Delay(1000);
            }

            waveIn.StopRecording();
            
            var result = capturedData.ToArray();
            _logger.LogDebug("Captured {ByteCount} bytes of audio from device {DeviceName}", 
                result.Length, device.Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture audio from device {DeviceId}", device.Id);
            throw;
        }
    }

    public async Task<byte[]> PreprocessAudioAsync(byte[] audioData, AudioSettings settings)
    {
        if (audioData == null || audioData.Length == 0)
            return Array.Empty<byte>();

        try
        {
            var processedData = new byte[audioData.Length];
            Array.Copy(audioData, processedData, audioData.Length);

            // Apply noise reduction if enabled
            if (settings.AutoAdjustNoiseThreshold)
            {
                processedData = await ReduceNoiseAsync(processedData, GetNoiseReductionLevel(settings));
            }

            // Apply volume normalization based on input volume setting
            if (settings.InputVolume != 100)
            {
                processedData = await NormalizeVolumeAsync(processedData, ConvertVolumeToDb(settings.InputVolume));
            }

            _logger.LogDebug("Preprocessed {InputBytes} bytes to {OutputBytes} bytes", 
                audioData.Length, processedData.Length);
            
            return processedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preprocess audio data");
            throw;
        }
    }

    public async Task<byte[]> ReduceNoiseAsync(byte[] audioData, float noiseReductionLevel)
    {
        if (audioData == null || audioData.Length == 0)
            return Array.Empty<byte>();

        if (noiseReductionLevel <= 0)
            return audioData;

        try
        {
            var result = new byte[audioData.Length];
            
            // Simple noise gate implementation
            // Convert bytes to 16-bit samples for processing
            for (int i = 0; i < audioData.Length - 1; i += 2)
            {
                var sample = BitConverter.ToInt16(audioData, i);
                var amplitude = Math.Abs(sample);
                
                // Apply noise gate - reduce amplitude of quiet sounds
                var threshold = (int)(short.MaxValue * 0.1f * noiseReductionLevel);
                
                if (amplitude < threshold)
                {
                    // Reduce amplitude of noise
                    sample = (short)(sample * (1.0f - noiseReductionLevel * 0.8f));
                }
                
                var bytes = BitConverter.GetBytes(sample);
                result[i] = bytes[0];
                result[i + 1] = bytes[1];
            }

            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reduce noise in audio data");
            throw;
        }
    }

    public async Task<byte[]> NormalizeVolumeAsync(byte[] audioData, float targetDbLevel = -16.0f)
    {
        if (audioData == null || audioData.Length == 0)
            return Array.Empty<byte>();

        try
        {
            var result = new byte[audioData.Length];
            
            // Calculate current RMS level
            double sumSquares = 0;
            int sampleCount = 0;
            
            for (int i = 0; i < audioData.Length - 1; i += 2)
            {
                var sample = BitConverter.ToInt16(audioData, i);
                sumSquares += sample * sample;
                sampleCount++;
            }
            
            if (sampleCount == 0)
                return audioData;
            
            var rms = Math.Sqrt(sumSquares / sampleCount);
            var currentDbLevel = 20 * Math.Log10(rms / short.MaxValue);
            
            // Calculate gain needed to reach target level
            var gainDb = targetDbLevel - currentDbLevel;
            var gainLinear = Math.Pow(10, gainDb / 20);
            
            // Apply gain with limiting to prevent clipping
            for (int i = 0; i < audioData.Length - 1; i += 2)
            {
                var sample = BitConverter.ToInt16(audioData, i);
                var normalizedSample = (int)(sample * gainLinear);
                
                // Limit to prevent clipping
                normalizedSample = Math.Max(short.MinValue, Math.Min(short.MaxValue, normalizedSample));
                
                var bytes = BitConverter.GetBytes((short)normalizedSample);
                result[i] = bytes[0];
                result[i + 1] = bytes[1];
            }

            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize audio volume");
            throw;
        }
    }

    public async Task<float> GetAudioInputLevelAsync()
    {
        if (_deviceEnumerator == null)
            return 0f;

        try
        {
            var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            var level = defaultDevice.AudioMeterInformation.MasterPeakValue;
            
            await Task.CompletedTask;
            return level;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get audio input level");
            return 0f;
        }
    }

    public async Task<byte[]> ConvertAudioFormatAsync(byte[] audioData, AudioFormat targetFormat, SpeechEngineType engine)
    {
        if (audioData == null || audioData.Length == 0)
            return Array.Empty<byte>();

        try
        {
            // Source format assumptions (from our capture)
            const int sourceSampleRate = 16000;
            const int sourceBitsPerSample = 16;
            const int sourceChannels = 1;

            // Check if conversion is needed
            if (targetFormat.SampleRate == sourceSampleRate && 
                targetFormat.BitsPerSample == sourceBitsPerSample && 
                targetFormat.Channels == sourceChannels &&
                targetFormat.Encoding == AudioEncoding.PCM)
            {
                return audioData; // No conversion needed
            }

            _logger.LogDebug("Converting audio format: {SourceRate}Hz/{SourceBits}bit/{SourceChannels}ch -> {TargetRate}Hz/{TargetBits}bit/{TargetChannels}ch",
                sourceSampleRate, sourceBitsPerSample, sourceChannels,
                targetFormat.SampleRate, targetFormat.BitsPerSample, targetFormat.Channels);

            var result = audioData;

            // Convert sample rate if needed
            if (targetFormat.SampleRate != sourceSampleRate)
            {
                result = await ResampleAudioAsync(result, sourceSampleRate, targetFormat.SampleRate);
            }

            // Convert channel count if needed
            if (targetFormat.Channels != sourceChannels)
            {
                result = ConvertChannels(result, sourceChannels, targetFormat.Channels);
            }

            // Convert bit depth if needed
            if (targetFormat.BitsPerSample != sourceBitsPerSample)
            {
                result = ConvertBitDepth(result, sourceBitsPerSample, targetFormat.BitsPerSample);
            }

            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert audio format");
            throw;
        }
    }

    public async Task<IEnumerable<AudioDevice>> GetAudioInputDevicesAsync()
    {
        if (_deviceEnumerator == null)
            throw new InvalidOperationException("NAudioProcessor not initialized. Call InitializeAsync first.");

        try
        {
            var devices = new List<AudioDevice>();
            var mmDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            foreach (var mmDevice in mmDevices)
            {
                var device = new AudioDevice
                {
                    Id = mmDevice.ID,
                    Name = mmDevice.FriendlyName,
                    IsDefault = mmDevice.ID == _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console).ID,
                    IsEnabled = mmDevice.State == DeviceState.Active,
                    IsInput = true,
                    Volume = (int)(mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100),
                    IsMuted = mmDevice.AudioEndpointVolume.Mute
                };
                devices.Add(device);
            }

            _logger.LogInformation("Found {DeviceCount} audio input devices", devices.Count);
            await Task.CompletedTask;
            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate audio input devices");
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<byte[]> ResampleAudioAsync(byte[] audioData, int sourceRate, int targetRate)
    {
        if (sourceRate == targetRate)
            return audioData;

        try
        {
            // Simple linear interpolation resampling
            var sourceSamples = audioData.Length / 2; // 16-bit samples
            var targetSamples = (int)((long)sourceSamples * targetRate / sourceRate);
            var result = new byte[targetSamples * 2];

            for (int i = 0; i < targetSamples; i++)
            {
                var sourceIndex = (double)i * sourceRate / targetRate;
                var index = (int)sourceIndex;
                var fraction = sourceIndex - index;

                short sample1 = 0, sample2 = 0;

                if (index * 2 < audioData.Length - 1)
                    sample1 = BitConverter.ToInt16(audioData, index * 2);
                if ((index + 1) * 2 < audioData.Length - 1)
                    sample2 = BitConverter.ToInt16(audioData, (index + 1) * 2);

                // Linear interpolation
                var interpolated = (short)(sample1 + (sample2 - sample1) * fraction);
                var bytes = BitConverter.GetBytes(interpolated);
                result[i * 2] = bytes[0];
                result[i * 2 + 1] = bytes[1];
            }

            _logger.LogDebug("Resampled audio from {SourceRate}Hz to {TargetRate}Hz: {SourceSamples} -> {TargetSamples} samples",
                sourceRate, targetRate, sourceSamples, targetSamples);

            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resample audio");
            throw;
        }
    }

    private byte[] ConvertChannels(byte[] audioData, int sourceChannels, int targetChannels)
    {
        if (sourceChannels == targetChannels)
            return audioData;

        try
        {
            if (sourceChannels == 1 && targetChannels == 2)
            {
                // Mono to stereo - duplicate mono channel
                var result = new byte[audioData.Length * 2];
                for (int i = 0; i < audioData.Length; i += 2)
                {
                    // Copy mono sample to both left and right channels
                    result[i * 2] = audioData[i];
                    result[i * 2 + 1] = audioData[i + 1];
                    result[i * 2 + 2] = audioData[i];
                    result[i * 2 + 3] = audioData[i + 1];
                }
                return result;
            }
            else if (sourceChannels == 2 && targetChannels == 1)
            {
                // Stereo to mono - mix channels
                var result = new byte[audioData.Length / 2];
                for (int i = 0; i < audioData.Length; i += 4)
                {
                    var left = BitConverter.ToInt16(audioData, i);
                    var right = BitConverter.ToInt16(audioData, i + 2);
                    var mixed = (short)((left + right) / 2);
                    var bytes = BitConverter.GetBytes(mixed);
                    result[i / 2] = bytes[0];
                    result[i / 2 + 1] = bytes[1];
                }
                return result;
            }

            _logger.LogWarning("Unsupported channel conversion: {SourceChannels} -> {TargetChannels}",
                sourceChannels, targetChannels);
            return audioData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert audio channels");
            return audioData;
        }
    }

    private byte[] ConvertBitDepth(byte[] audioData, int sourceBits, int targetBits)
    {
        if (sourceBits == targetBits)
            return audioData;

        try
        {
            if (sourceBits == 16 && targetBits == 8)
            {
                // 16-bit to 8-bit conversion
                var result = new byte[audioData.Length / 2];
                for (int i = 0; i < audioData.Length; i += 2)
                {
                    var sample16 = BitConverter.ToInt16(audioData, i);
                    var sample8 = (byte)((sample16 + 32768) / 256); // Convert to unsigned 8-bit
                    result[i / 2] = sample8;
                }
                return result;
            }
            else if (sourceBits == 8 && targetBits == 16)
            {
                // 8-bit to 16-bit conversion
                var result = new byte[audioData.Length * 2];
                for (int i = 0; i < audioData.Length; i++)
                {
                    var sample8 = audioData[i];
                    var sample16 = (short)((sample8 - 128) * 256); // Convert from unsigned 8-bit
                    var bytes = BitConverter.GetBytes(sample16);
                    result[i * 2] = bytes[0];
                    result[i * 2 + 1] = bytes[1];
                }
                return result;
            }

            _logger.LogWarning("Unsupported bit depth conversion: {SourceBits} -> {TargetBits}",
                sourceBits, targetBits);
            return audioData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert audio bit depth");
            return audioData;
        }
    }

    private MMDevice? GetMMDevice(AudioDevice device)
    {
        try
        {
            return _deviceEnumerator?.GetDevice(device.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not find MMDevice for AudioDevice {DeviceId}", device.Id);
            return null;
        }
    }

    private static float GetNoiseReductionLevel(AudioSettings settings)
    {
        // Convert noise threshold to reduction level
        // Higher threshold = more aggressive noise reduction
        return Math.Min(1.0f, settings.NoiseThreshold / 100.0f);
    }

    private static float ConvertVolumeToDb(int volumePercent)
    {
        // Convert volume percentage to dB
        // 100% = -6dB, 50% = -12dB, etc.
        if (volumePercent <= 0) return -60f;
        return -6f - (20f * (100f - volumePercent) / 100f);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _waveIn?.Dispose();
            _deviceEnumerator?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}