using System.Collections.Generic;
using VoiceInputAssistant.Core.Models;

namespace VoiceInputAssistant.Models;

/// <summary>
/// Configuration for speech recognition engines
/// </summary>
public class SpeechEngineConfig
{
    public SpeechEngine Engine { get; set; }
    public string? ApiKey { get; set; }
    public string? Region { get; set; }
    public string? ModelPath { get; set; }
    public string Language { get; set; } = "en-US";
    public RecognitionQuality Quality { get; set; } = RecognitionQuality.Balanced;
    public PostProcessingMode PostProcessing { get; set; } = PostProcessingMode.BasicCorrection;
    public VoiceActivationMode ActivationMode { get; set; } = VoiceActivationMode.PushToTalk;
    public bool EnablePartialResults { get; set; } = true;
    public bool EnableVoiceActivityDetection { get; set; } = true;
    public double SilenceTimeout { get; set; } = 2.0;
    public double AudioLevelThreshold { get; set; } = 0.1;
    public int SampleRate { get; set; } = 16000;
    public int BitDepth { get; set; } = 16;
    public int Channels { get; set; } = 1;
    public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
}