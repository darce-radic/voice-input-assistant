using System;

namespace VoiceInputAssistant.Core.Models
{
    /// <summary>
    /// Represents accuracy statistics for a speech recognition engine
    /// </summary>
    public class EngineAccuracyStatistics
    {
        public string EngineName { get; set; } = string.Empty;
        public int TotalRecognitions { get; set; }
        public double AverageAccuracy { get; set; }
        public double MedianAccuracy { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public int ErrorCount { get; set; }
    }
}