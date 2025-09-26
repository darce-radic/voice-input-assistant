import { format, formatDistance, formatRelative } from 'date-fns';
import type { TranscriptionResult, EngineMetrics, UsageStatistics } from './types';

/**
 * Formats a date for display
 */
export function formatDate(date: Date, formatString = 'PPpp'): string {
  return format(date, formatString);
}

/**
 * Formats a relative time (e.g., "2 hours ago")
 */
export function formatRelativeTime(date: Date, baseDate = new Date()): string {
  return formatDistance(date, baseDate, { addSuffix: true });
}

/**
 * Formats a relative date (e.g., "yesterday at 2:30 PM")
 */
export function formatRelativeDate(date: Date, baseDate = new Date()): string {
  return formatRelative(date, baseDate);
}

/**
 * Formats a word count with proper units
 */
export function formatWordCount(count: number): string {
  if (count < 1000) {
    return `${count} words`;
  }
  return `${(count / 1000).toFixed(1)}k words`;
}

/**
 * Formats a duration in milliseconds to a human-readable string
 */
export function formatDuration(ms: number): string {
  const seconds = Math.floor(ms / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);

  if (hours > 0) {
    return `${hours}h ${minutes % 60}m ${seconds % 60}s`;
  }
  if (minutes > 0) {
    return `${minutes}m ${seconds % 60}s`;
  }
  return `${seconds}s`;
}

/**
 * Formats a file size in bytes to a human-readable string
 */
export function formatFileSize(bytes: number): string {
  const units = ['B', 'KB', 'MB', 'GB'];
  let size = bytes;
  let unitIndex = 0;

  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024;
    unitIndex++;
  }

  return `${size.toFixed(1)} ${units[unitIndex]}`;
}

/**
 * Formats a percentage
 */
export function formatPercent(value: number, decimals = 1): string {
  return `${(value * 100).toFixed(decimals)}%`;
}

/**
 * Calculates word count from text
 */
export function calculateWordCount(text: string): number {
  return text.trim().split(/\s+/).length;
}

/**
 * Calculates speaking rate (words per minute)
 */
export function calculateSpeakingRate(wordCount: number, durationMs: number): number {
  const minutes = durationMs / 1000 / 60;
  return Math.round(wordCount / minutes);
}

/**
 * Generates a summary of transcription results
 */
export function generateTranscriptionSummary(result: TranscriptionResult): string {
  const confidenceStr = formatPercent(result.confidence);
  if (!result.success) {
    return `Transcription failed: ${result.errorMessage}`;
  }
  return `Transcription completed with ${confidenceStr} confidence (${result.engine})`;
}

/**
 * Generates a summary of engine metrics
 */
export function generateEngineMetricsSummary(metrics: EngineMetrics): string {
  const accuracy = formatPercent(metrics.accuracyRate);
  const errorRate = formatPercent(metrics.errorRate);
  const latency = formatDuration(metrics.averageLatency);
  
  return `${metrics.engine}: ${accuracy} accuracy, ${errorRate} errors, ${latency} avg latency`;
}

/**
 * Generates a summary of usage statistics
 */
export function generateUsageSummary(stats: UsageStatistics): string {
  const successRate = formatPercent(stats.successfulTranscriptions / stats.totalTranscriptions);
  const duration = formatDuration(stats.totalDuration);
  const words = formatWordCount(stats.totalWordCount);
  
  return `${stats.totalTranscriptions} transcriptions (${successRate} success) | ${duration} total | ${words}`;
}

/**
 * Creates a dummy audio stream for testing
 */
export function createTestAudioStream(durationMs: number = 1000): MediaStream {
  const audioContext = new AudioContext();
  const oscillator = audioContext.createOscillator();
  const destination = audioContext.createMediaStreamDestination();
  
  oscillator.connect(destination);
  oscillator.start();
  setTimeout(() => oscillator.stop(), durationMs);
  
  return destination.stream;
}

/**
 * Checks if the browser supports required Web APIs
 */
export function checkBrowserSupport(): Record<string, boolean> {
  return {
    mediaDevices: 'mediaDevices' in navigator,
    audioWorklet: 'audioWorklet' in AudioContext.prototype,
    mediaRecorder: 'MediaRecorder' in window,
    webAudio: 'AudioContext' in window,
    webWorkers: 'Worker' in window,
    serviceWorker: 'serviceWorker' in navigator,
    indexedDB: 'indexedDB' in window,
    permissions: 'permissions' in navigator,
  };
}

/**
 * Throttles a function
 */
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle = false;
  
  return function(this: any, ...args: Parameters<T>): void {
    if (!inThrottle) {
      func.apply(this, args);
      inThrottle = true;
      setTimeout(() => inThrottle = false, limit);
    }
  };
}

/**
 * Debounces a function
 */
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout;
  
  return function(this: any, ...args: Parameters<T>): void {
    clearTimeout(timeout);
    timeout = setTimeout(() => func.apply(this, args), wait);
  };
}