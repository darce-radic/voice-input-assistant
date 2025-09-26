using System;

namespace VoiceInputAssistant.Core.Models
{
    /// <summary>
    /// Represents application usage statistics
    /// </summary>
    public class ApplicationUsage
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public int RecognitionCount { get; set; }
        public TimeSpan TotalUsageTime { get; set; }
        public double AverageAccuracy { get; set; }
        public DateTime LastUsed { get; set; }
    }
}