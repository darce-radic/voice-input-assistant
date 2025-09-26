using System;

namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Status information for a speech recognition engine
/// </summary>
public record SpeechEngineStatus(
    bool IsAvailable,
    SpeechEngineType Engine,
    string? EngineVersion = null,
    bool RequiresNetwork = false,
    string[]? SupportedLanguages = null,
    bool SupportsInterimResults = false,
    bool SupportsSpeakerDiarization = false,
    string? StatusMessage = null,
    DateTime LastChecked = default
);