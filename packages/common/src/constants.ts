export const SUPPORTED_LANGUAGES = {
  'en': 'English',
  'zh': 'Chinese (Simplified)',
  'es': 'Spanish',
  'hi': 'Hindi',
  'ar': 'Arabic',
  'bn': 'Bengali',
  'pt': 'Portuguese',
  'ru': 'Russian',
  'ja': 'Japanese',
  'de': 'German',
  'fr': 'French',
  'it': 'Italian',
  'ko': 'Korean',
  'tr': 'Turkish',
  'pl': 'Polish',
  'uk': 'Ukrainian',
  'vi': 'Vietnamese',
  'th': 'Thai',
  'nl': 'Dutch',
  'id': 'Indonesian',
} as const;

export const DEFAULT_SETTINGS = {
  voice: {
    minimumConfidence: 0.7,
    requireHighConfidence: false,
    enableProfanityFilter: true,
    enableAutoPunctuation: true,
    enableSpeakerDiarization: false,
    enableWordTimings: false,
  },
  language: {
    primaryLanguage: 'en',
    secondaryLanguages: [],
    autoDetectLanguage: true,
    languageWeights: {},
  },
  recording: {
    autoStopAfterSilence: true,
    silenceThresholdSeconds: 2,
    maxRecordingDurationSeconds: 300,
    audioSampleRate: 16000,
    audioChannels: 1,
  },
} as const;

export const SPEECH_ENGINE_CONFIG = {
  whisperLocal: {
    name: 'Whisper Local',
    requiresNetwork: false,
    supportedFeatures: {
      interimResults: false,
      speakerDiarization: false,
      wordTimings: false,
      multiLanguage: true,
    },
  },
  azureSpeech: {
    name: 'Azure Speech',
    requiresNetwork: true,
    supportedFeatures: {
      interimResults: true,
      speakerDiarization: true,
      wordTimings: true,
      multiLanguage: true,
    },
  },
  openAiWhisper: {
    name: 'OpenAI Whisper',
    requiresNetwork: true,
    supportedFeatures: {
      interimResults: false,
      speakerDiarization: false,
      wordTimings: false,
      multiLanguage: true,
    },
  },
  googleSpeech: {
    name: 'Google Speech',
    requiresNetwork: true,
    supportedFeatures: {
      interimResults: true,
      speakerDiarization: true,
      wordTimings: true,
      multiLanguage: true,
    },
  },
  windowsSpeech: {
    name: 'Windows Speech',
    requiresNetwork: false,
    supportedFeatures: {
      interimResults: true,
      speakerDiarization: false,
      wordTimings: false,
      multiLanguage: false,
    },
  },
} as const;

export const API_ENDPOINTS = {
  speech: {
    transcribe: '/api/speech/transcribe',
    status: '/api/speech/status',
    engines: '/api/speech/engines',
  },
  profile: {
    get: '/api/profile',
    update: '/api/profile',
    delete: '/api/profile',
  },
  analytics: {
    usage: '/api/analytics/usage',
    errors: '/api/analytics/errors',
    metrics: '/api/analytics/metrics',
    export: '/api/analytics/export',
  },
  auth: {
    login: '/api/auth/login',
    logout: '/api/auth/logout',
    refresh: '/api/auth/refresh',
  },
} as const;

export const ERROR_TYPES = {
  SPEECH_RECOGNITION: 'SPEECH_RECOGNITION_ERROR',
  NETWORK: 'NETWORK_ERROR',
  AUTHENTICATION: 'AUTHENTICATION_ERROR',
  AUTHORIZATION: 'AUTHORIZATION_ERROR',
  VALIDATION: 'VALIDATION_ERROR',
  SERVER: 'SERVER_ERROR',
  UNKNOWN: 'UNKNOWN_ERROR',
} as const;

export const HTTP_STATUS = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  INTERNAL_SERVER_ERROR: 500,
} as const;

export const CACHE_KEYS = {
  USER_PROFILE: 'user_profile',
  APP_SETTINGS: 'app_settings',
  LANGUAGE_SETTINGS: 'language_settings',
  VOICE_SETTINGS: 'voice_settings',
  ENGINE_STATUS: 'engine_status',
  RECENT_TRANSCRIPTIONS: 'recent_transcriptions',
} as const;

export const EVENTS = {
  TRANSCRIPTION_START: 'transcription_start',
  TRANSCRIPTION_END: 'transcription_end',
  TRANSCRIPTION_ERROR: 'transcription_error',
  ENGINE_STATUS_CHANGE: 'engine_status_change',
  SETTINGS_CHANGE: 'settings_change',
  PROFILE_UPDATE: 'profile_update',
  CONNECTION_STATUS: 'connection_status',
} as const;

export const TIMEOUTS = {
  API_REQUEST: 30000,
  TRANSCRIPTION: 300000,
  TOKEN_REFRESH: 300000,
  CACHE_DURATION: 3600000,
  RETRY_DELAY: 1000,
} as const;