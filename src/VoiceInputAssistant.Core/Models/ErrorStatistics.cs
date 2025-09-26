using System;

namespace VoiceInputAssistant.Core.Models
{
    /// <summary>
    /// Represents error statistics
    /// </summary>
    public class ErrorStatistics
    {
        public string ErrorType { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastOccurred { get; set; }
        public string MostCommonMessage { get; set; } = string.Empty;
    }
}