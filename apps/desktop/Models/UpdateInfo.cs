using System;

namespace VoiceInputAssistant.Models;

/// <summary>
/// Information about an available update
/// </summary>
public class UpdateInfo
{
    public Version CurrentlyInstalledVersion { get; set; } = new Version("0.0.0.0");
    public ReleaseEntry? FutureReleaseEntry { get; set; }
    public ReleaseEntry[]? ReleasesToApply { get; set; }
    public string PackageDirectory { get; set; } = string.Empty;
    public string FetchReleaseNotes { get; set; } = string.Empty;
}
