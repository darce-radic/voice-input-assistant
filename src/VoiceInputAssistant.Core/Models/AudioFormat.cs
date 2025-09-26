namespace VoiceInputAssistant.Core.Models;

/// <summary>
/// Audio format information
/// </summary>
public class AudioFormat
{
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
    public int BytesPerSecond { get; set; }
    public int BlockAlign { get; set; }
    public AudioEncoding Encoding { get; set; }
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