using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceInputAssistant.Core.Events;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for managing audio devices and audio capture
    /// </summary>
    public interface IAudioDeviceService
    {
        /// <summary>
        /// Event raised when audio devices are changed (added, removed, or default changed)
        /// </summary>
        event EventHandler<AudioDeviceChangedEventArgs> DeviceChanged;

        /// <summary>
        /// Event raised when audio level changes during recording
        /// </summary>
        event EventHandler<AudioLevelEventArgs> AudioLevelChanged;

        /// <summary>
        /// Event raised when voice activity is detected
        /// </summary>
        event EventHandler<VoiceActivityEventArgs> VoiceActivityDetected;

        /// <summary>
        /// Gets whether the service is initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets whether audio recording is currently active
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// Initializes the audio device service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the audio device service
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Gets all available input devices (microphones)
        /// </summary>
        /// <returns>Collection of available input devices</returns>
        Task<IEnumerable<AudioDevice>> GetInputDevicesAsync();

        /// <summary>
        /// Gets all available output devices (speakers/headphones)
        /// </summary>
        /// <returns>Collection of available output devices</returns>
        Task<IEnumerable<AudioDevice>> GetOutputDevicesAsync();

        /// <summary>
        /// Gets the default input device
        /// </summary>
        /// <returns>Default input device or null if none available</returns>
        Task<AudioDevice> GetDefaultInputDeviceAsync();

        /// <summary>
        /// Gets the default output device
        /// </summary>
        /// <returns>Default output device or null if none available</returns>
        Task<AudioDevice> GetDefaultOutputDeviceAsync();

        /// <summary>
        /// Sets the audio device to use for input
        /// </summary>
        /// <param name="device">Device to use, or null for default</param>
        Task SetInputDeviceAsync(AudioDevice device);

        /// <summary>
        /// Gets the currently selected input device
        /// </summary>
        /// <returns>Current input device</returns>
        Task<AudioDevice> GetCurrentInputDeviceAsync();

        /// <summary>
        /// Tests an input device to verify it's working
        /// </summary>
        /// <param name="device">Device to test</param>
        /// <param name="duration">Duration to test for</param>
        /// <returns>True if the device is working properly</returns>
        Task<bool> TestInputDeviceAsync(AudioDevice device, TimeSpan duration);

        /// <summary>
        /// Starts audio recording
        /// </summary>
        /// <param name="settings">Recording settings</param>
        Task StartRecordingAsync(AudioRecordingSettings settings);

        /// <summary>
        /// Stops audio recording
        /// </summary>
        Task StopRecordingAsync();

        /// <summary>
        /// Gets the current audio level (0.0 to 1.0)
        /// </summary>
        /// <returns>Current audio level</returns>
        float GetCurrentAudioLevel();

        /// <summary>
        /// Configures voice activity detection
        /// </summary>
        /// <param name="settings">VAD settings</param>
        Task ConfigureVoiceActivityDetectionAsync(VadSettings settings);

        /// <summary>
        /// Enables or disables voice activity detection
        /// </summary>
        /// <param name="enabled">Whether to enable VAD</param>
        Task SetVoiceActivityDetectionAsync(bool enabled);

        /// <summary>
        /// Configures audio preprocessing options
        /// </summary>
        /// <param name="settings">Preprocessing settings</param>
        Task ConfigureAudioPreprocessingAsync(AudioPreprocessingSettings settings);

        /// <summary>
        /// Processes raw audio data through the preprocessing pipeline
        /// </summary>
        /// <param name="audioData">Raw audio data</param>
        /// <returns>Processed audio data</returns>
        Task<byte[]> ProcessAudioAsync(byte[] audioData);

        /// <summary>
        /// Gets audio format information for the current input device
        /// </summary>
        /// <returns>Audio format information</returns>
        Task<AudioFormat> GetAudioFormatAsync();

        /// <summary>
        /// Plays a test tone through the output device
        /// </summary>
        /// <param name="frequency">Frequency in Hz</param>
        /// <param name="duration">Duration to play</param>
        Task PlayTestToneAsync(float frequency, TimeSpan duration);
    }

    /// <summary>
    /// Event arguments for audio device changes
    /// </summary>
    public class AudioDeviceChangedEventArgs : EventArgs
    {
        public AudioDeviceChangeType ChangeType { get; set; }
        public AudioDevice Device { get; set; }
    }

    /// <summary>
    /// Types of audio device changes
    /// </summary>
    public enum AudioDeviceChangeType
    {
        DeviceAdded,
        DeviceRemoved,
        DefaultDeviceChanged,
        DeviceStateChanged
    }

    /// <summary>
    /// Audio recording settings
    /// </summary>
    public class AudioRecordingSettings
    {
        /// <summary>
        /// Sample rate in Hz (e.g., 16000, 44100, 48000)
        /// </summary>
        public int SampleRate { get; set; } = 16000;

        /// <summary>
        /// Number of channels (1 = mono, 2 = stereo)
        /// </summary>
        public int Channels { get; set; } = 1;

        /// <summary>
        /// Bits per sample (16, 24, 32)
        /// </summary>
        public int BitsPerSample { get; set; } = 16;

        /// <summary>
        /// Buffer size in milliseconds
        /// </summary>
        public int BufferDurationMs { get; set; } = 100;

        /// <summary>
        /// Whether to enable automatic gain control
        /// </summary>
        public bool EnableAutomaticGainControl { get; set; } = true;

        /// <summary>
        /// Recording gain (0.0 to 2.0, where 1.0 is unity gain)
        /// </summary>
        public float Gain { get; set; } = 1.0f;
    }

    /// <summary>
    /// Voice Activity Detection settings
    /// </summary>
    public class VadSettings
    {
        /// <summary>
        /// Sensitivity threshold (0.0 to 1.0)
        /// </summary>
        public float Sensitivity { get; set; } = 0.5f;

        /// <summary>
        /// Minimum speech duration to trigger detection (ms)
        /// </summary>
        public int MinSpeechDurationMs { get; set; } = 100;

        /// <summary>
        /// Minimum silence duration to trigger end of speech (ms)
        /// </summary>
        public int MinSilenceDurationMs { get; set; } = 500;

        /// <summary>
        /// Pre-speech buffer duration (ms)
        /// </summary>
        public int PreSpeechBufferMs { get; set; } = 200;

        /// <summary>
        /// Post-speech buffer duration (ms)
        /// </summary>
        public int PostSpeechBufferMs { get; set; } = 200;

        /// <summary>
        /// VAD algorithm to use
        /// </summary>
        public VadAlgorithm Algorithm { get; set; } = VadAlgorithm.WebRTC;
    }

    /// <summary>
    /// Audio preprocessing settings
    /// </summary>
    public class AudioPreprocessingSettings
    {
        /// <summary>
        /// Enable noise suppression
        /// </summary>
        public bool EnableNoiseSuppression { get; set; } = true;

        /// <summary>
        /// Noise suppression level (0 = low, 1 = medium, 2 = high)
        /// </summary>
        public int NoiseSuppressionLevel { get; set; } = 1;

        /// <summary>
        /// Enable echo cancellation
        /// </summary>
        public bool EnableEchoCancellation { get; set; } = false;

        /// <summary>
        /// Enable automatic gain control
        /// </summary>
        public bool EnableAutomaticGainControl { get; set; } = true;

        /// <summary>
        /// Enable high-pass filter to remove low-frequency noise
        /// </summary>
        public bool EnableHighPassFilter { get; set; } = true;

        /// <summary>
        /// High-pass filter cutoff frequency (Hz)
        /// </summary>
        public float HighPassCutoffFrequency { get; set; } = 85.0f;

        /// <summary>
        /// Enable dynamic range compression
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Compression threshold (dB)
        /// </summary>
        public float CompressionThreshold { get; set; } = -20.0f;

        /// <summary>
        /// Compression ratio
        /// </summary>
        public float CompressionRatio { get; set; } = 3.0f;
    }

    /// <summary>
    /// Available VAD algorithms
    /// </summary>
    public enum VadAlgorithm
    {
        /// <summary>
        /// Simple energy-based detection
        /// </summary>
        Energy,

        /// <summary>
        /// WebRTC VAD algorithm
        /// </summary>
        WebRTC,

        /// <summary>
        /// Silero VAD (neural network based)
        /// </summary>
        Silero,

        /// <summary>
        /// Custom algorithm implementation
        /// </summary>
        Custom
    }

    /// <summary>
    /// Audio encoding formats
    /// </summary>
    public enum AudioEncoding
    {
        PCM,
        IEEE_FLOAT,
        ALAW,
        MULAW,
        MP3,
        AAC,
        OPUS,
        FLAC
    }
}