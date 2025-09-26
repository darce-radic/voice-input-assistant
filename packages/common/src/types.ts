// Speech Recognition Types
export type SpeechEngineType = 
  | 'whisper-local'
  | 'azure-speech'
  | 'openai-whisper'
  | 'google-speech'
  | 'windows-speech';

export interface TranscriptionResult {
  text: string;
  confidence: number;
  language: string;
  engine: SpeechEngineType;
  success: boolean;
  errorMessage?: string;
}

export interface SpeechEngineStatus {
  isAvailable: boolean;
  engine: SpeechEngineType;
  engineVersion?: string;
  requiresNetwork: boolean;
  supportedLanguages: string[];
  supportsInterimResults: boolean;
  supportsSpeakerDiarization: boolean;
}

// Application Profile Types
export interface ApplicationProfile {
  applicationName: string;
  displayName?: string;
  processName?: string;
  preferredEngine: SpeechEngineType;
  languageSettings: LanguageSettings;
  voiceSettings: VoiceSettings;
  isEnabled: boolean;
}

export interface LanguageSettings {
  primaryLanguage: string;
  secondaryLanguages: string[];
  autoDetectLanguage: boolean;
  languageWeights: Record<string, number>;
}

export interface VoiceSettings {
  minimumConfidence: number;
  requireHighConfidence: boolean;
  enableProfanityFilter: boolean;
  enableAutoPunctuation: boolean;
  enableSpeakerDiarization: boolean;
  enableWordTimings: boolean;
  voiceTrigger?: VoiceTriggerSettings;
}

export interface VoiceTriggerSettings {
  wakeWord: string;
  wakeWordSensitivity: number;
  timeoutSeconds: number;
  requireConfirmation: boolean;
}

// Analytics Types
export interface TranscriptionEvent {
  id: string;
  timestamp: Date;
  applicationName: string;
  engine: SpeechEngineType;
  language: string;
  confidence: number;
  duration: number;
  wordCount: number;
  wasSuccessful: boolean;
  errorType?: string;
}

export interface ErrorEvent {
  id: string;
  timestamp: Date;
  applicationName: string;
  engine: SpeechEngineType;
  errorType: string;
  errorMessage: string;
  stackTrace?: string;
}

export interface EngineMetrics {
  timestamp: Date;
  engine: SpeechEngineType;
  averageConfidence: number;
  accuracyRate: number;
  errorRate: number;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  averageLatency: number;
  totalProcessingTimeMs: number;
  peakMemoryUsageBytes: number;
}

export interface UsageStatistics {
  startTime: Date;
  endTime: Date;
  totalTranscriptions: number;
  successfulTranscriptions: number;
  failedTranscriptions: number;
  averageConfidence: number;
  totalDuration: number;
  totalWordCount: number;
  transcriptionsByEngine: Record<SpeechEngineType, number>;
  transcriptionsByLanguage: Record<string, number>;
  transcriptionsByApplication: Record<string, number>;
}

export interface ErrorStatistics {
  startTime: Date;
  endTime: Date;
  totalErrors: number;
  errorsByType: Record<string, number>;
  errorsByEngine: Record<SpeechEngineType, number>;
  errorsByApplication: Record<string, number>;
  mostFrequentErrors: ErrorEvent[];
}

export type ExportFormat = 'json' | 'csv' | 'excel';