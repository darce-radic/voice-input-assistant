using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceInputAssistant.Models;
using ModelEngine = VoiceInputAssistant.Models.SpeechEngine;
using ModelResult = VoiceInputAssistant.Models.SpeechRecognitionResult;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Adapter interface for speech engine implementations
/// </summary>
public interface ISpeechEngineAdapter
{
    event EventHandler<SpeechRecognitionEventArgs> SpeechRecognized;
    event EventHandler<SpeechRecognitionErrorEventArgs> RecognitionError;
    event EventHandler<VoiceActivityEventArgs> VoiceActivityDetected;
    event EventHandler<PartialRecognitionEventArgs> PartialRecognitionReceived;
    event EventHandler<AudioLevelEventArgs> AudioLevelChanged;

    bool IsInitialized { get; }
    bool IsListening { get; }
    ModelEngine Engine { get; }

    Task InitializeAsync(SpeechEngineConfig config, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task<ModelResult> ProcessAudioFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task ProcessAudioDataAsync(byte[] audioData, int offset, int count, CancellationToken cancellationToken = default);
    Task ConfigureAsync(SpeechEngineConfig config, CancellationToken cancellationToken = default);
    Task UpdateVocabularyAsync(IEnumerable<string> phrases, CancellationToken cancellationToken = default);
    void UpdateConfiguration(SpeechEngineConfig config);
    void Dispose();
}

/// <summary>
/// Event arguments for partial recognition results
/// </summary>
public class PartialRecognitionEventArgs : EventArgs
{
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for audio level changes
/// </summary>
public class AudioLevelEventArgs : EventArgs
{
    public double Level { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
