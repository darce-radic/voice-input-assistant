using System;

namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Event arguments for speech recognition events
/// </summary>
public class SpeechRecognitionEventArgs : EventArgs
{
    public SpeechRecognitionResult Result { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for speech recognition errors
/// </summary>
public class SpeechRecognitionErrorEventArgs : EventArgs
{
    public Exception Exception { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for partial recognition results
/// </summary>
public class PartialRecognitionEventArgs : EventArgs
{
    public string PartialText { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

