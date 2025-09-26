using System;

namespace VoiceInputAssistant.Models;

/// <summary>
/// Represents a release entry from the update server
/// </summary>
public class ReleaseEntry
{
    public string SHA1 { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public long Filesize { get; set; }
    public bool IsDelta { get; set; }
    public Version Version { get; set; } = new Version("0.0.0.0");
    public string PackageName { get; set; } = string.Empty;
    public string EntryAsString { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
}