import { z } from 'zod';

// Enums
export const SpeechEngineTypeSchema = z.enum([
  'whisper-local',
  'azure-speech',
  'openai-whisper',
  'google-speech',
  'windows-speech'
]);

export const ExportFormatSchema = z.enum(['json', 'csv', 'excel']);

// Voice Settings
export const VoiceTriggerSettingsSchema = z.object({
  wakeWord: z.string().min(1),
  wakeWordSensitivity: z.number().min(0).max(1),
  timeoutSeconds: z.number().int().min(1),
  requireConfirmation: z.boolean()
});

export const VoiceSettingsSchema = z.object({
  minimumConfidence: z.number().min(0).max(1),
  requireHighConfidence: z.boolean(),
  enableProfanityFilter: z.boolean(),
  enableAutoPunctuation: z.boolean(),
  enableSpeakerDiarization: z.boolean(),
  enableWordTimings: z.boolean(),
  voiceTrigger: VoiceTriggerSettingsSchema.optional()
});

// Language Settings
export const LanguageSettingsSchema = z.object({
  primaryLanguage: z.string().min(2),
  secondaryLanguages: z.array(z.string().min(2)),
  autoDetectLanguage: z.boolean(),
  languageWeights: z.record(z.string(), z.number().min(0).max(1))
});

// Application Profile
export const ApplicationProfileSchema = z.object({
  applicationName: z.string().min(1),
  displayName: z.string().optional(),
  processName: z.string().optional(),
  preferredEngine: SpeechEngineTypeSchema,
  languageSettings: LanguageSettingsSchema,
  voiceSettings: VoiceSettingsSchema,
  isEnabled: z.boolean()
});

// Transcription Results
export const TranscriptionResultSchema = z.object({
  text: z.string(),
  confidence: z.number().min(0).max(1),
  language: z.string().min(2),
  engine: SpeechEngineTypeSchema,
  success: z.boolean(),
  errorMessage: z.string().optional()
});

export const SpeechEngineStatusSchema = z.object({
  isAvailable: z.boolean(),
  engine: SpeechEngineTypeSchema,
  engineVersion: z.string().optional(),
  requiresNetwork: z.boolean(),
  supportedLanguages: z.array(z.string().min(2)),
  supportsInterimResults: z.boolean(),
  supportsSpeakerDiarization: z.boolean()
});

// Analytics Events
export const TranscriptionEventSchema = z.object({
  id: z.string().uuid(),
  timestamp: z.date(),
  applicationName: z.string().min(1),
  engine: SpeechEngineTypeSchema,
  language: z.string().min(2),
  confidence: z.number().min(0).max(1),
  duration: z.number().min(0),
  wordCount: z.number().int().min(0),
  wasSuccessful: z.boolean(),
  errorType: z.string().optional()
});

export const ErrorEventSchema = z.object({
  id: z.string().uuid(),
  timestamp: z.date(),
  applicationName: z.string().min(1),
  engine: SpeechEngineTypeSchema,
  errorType: z.string().min(1),
  errorMessage: z.string().min(1),
  stackTrace: z.string().optional()
});

export const EngineMetricsSchema = z.object({
  timestamp: z.date(),
  engine: SpeechEngineTypeSchema,
  averageConfidence: z.number().min(0).max(1),
  accuracyRate: z.number().min(0).max(1),
  errorRate: z.number().min(0).max(1),
  totalRequests: z.number().int().min(0),
  successfulRequests: z.number().int().min(0),
  failedRequests: z.number().int().min(0),
  averageLatency: z.number().min(0),
  totalProcessingTimeMs: z.number().min(0),
  peakMemoryUsageBytes: z.number().min(0)
});

// Statistics
export const UsageStatisticsSchema = z.object({
  startTime: z.date(),
  endTime: z.date(),
  totalTranscriptions: z.number().int().min(0),
  successfulTranscriptions: z.number().int().min(0),
  failedTranscriptions: z.number().int().min(0),
  averageConfidence: z.number().min(0).max(1),
  totalDuration: z.number().min(0),
  totalWordCount: z.number().int().min(0),
  transcriptionsByEngine: z.record(SpeechEngineTypeSchema, z.number().int().min(0)),
  transcriptionsByLanguage: z.record(z.string(), z.number().int().min(0)),
  transcriptionsByApplication: z.record(z.string(), z.number().int().min(0))
});

export const ErrorStatisticsSchema = z.object({
  startTime: z.date(),
  endTime: z.date(),
  totalErrors: z.number().int().min(0),
  errorsByType: z.record(z.string(), z.number().int().min(0)),
  errorsByEngine: z.record(SpeechEngineTypeSchema, z.number().int().min(0)),
  errorsByApplication: z.record(z.string(), z.number().int().min(0)),
  mostFrequentErrors: z.array(ErrorEventSchema)
});

// API Request Schemas
export const TranscribeRequestSchema = z.object({
  audioData: z.instanceof(Blob).or(z.instanceof(ArrayBuffer)),
  engine: SpeechEngineTypeSchema.optional(),
  language: z.string().min(2).optional(),
  applicationName: z.string().min(1)
});

export const ExportDataRequestSchema = z.object({
  startTime: z.date(),
  endTime: z.date(),
  format: ExportFormatSchema,
  applicationName: z.string().min(1).optional()
});