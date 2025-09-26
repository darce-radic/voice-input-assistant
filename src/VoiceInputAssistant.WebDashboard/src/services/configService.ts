import { EventEmitter } from 'events';
import { CacheStrategy } from './offlineCacheService';
import { VibrationPattern } from './mobileFeatures';
import { LogLevel } from './errorHandlingService';

// Configuration categories
export enum ConfigCategory {
  SPEECH = 'speech',
  ML = 'ml',
  WEBRTC = 'webrtc',
  MOBILE = 'mobile',
  CACHE = 'cache',
  PERFORMANCE = 'performance',
  UI = 'ui',
  ANALYTICS = 'analytics',
  LOGGING = 'logging',
  HOTKEYS = 'hotkeys'
}

// Configuration validation rules
export interface ConfigValidation {
  required?: boolean;
  type?: 'string' | 'number' | 'boolean' | 'object' | 'array';
  min?: number;
  max?: number;
  pattern?: RegExp;
  enum?: any[];
  custom?: (value: any) => boolean | string;
}

// Configuration schema definition
export interface ConfigSchema {
  [key: string]: {
    defaultValue: any;
    validation?: ConfigValidation;
    description?: string;
    category: ConfigCategory;
    sensitive?: boolean; // For API keys, passwords, etc.
    requiresRestart?: boolean;
  };
}

// Configuration export/import format
export interface ConfigExport {
  version: string;
  timestamp: Date;
  deviceInfo: any;
  configurations: {
    [category: string]: any;
  };
  userPreferences: {
    [key: string]: any;
  };
}

// Configuration change event
export interface ConfigChangeEvent {
  category: ConfigCategory;
  key: string;
  oldValue: any;
  newValue: any;
  timestamp: Date;
}

class ConfigService extends EventEmitter {
  private configurations: Map<string, any> = new Map();
  private userPreferences: Map<string, any> = new Map();
  private schema: ConfigSchema = {};
  private changeHistory: ConfigChangeEvent[] = [];
  
  constructor() {
    super();
    this.initializeSchema();
    this.loadConfigurations();
  }

  /**
   * Initialize configuration schema with defaults
   */
  private initializeSchema(): void {
    this.schema = {
      // Speech Service Configuration
      'speech.defaultEngine': {
        defaultValue: 'Google Cloud',
        validation: { 
          type: 'string', 
          enum: ['Google Cloud', 'Azure', 'AWS', 'OpenAI Whisper'] 
        },
        description: 'Default speech recognition engine',
        category: ConfigCategory.SPEECH
      },
      'speech.language': {
        defaultValue: 'en-US',
        validation: { type: 'string', pattern: /^[a-z]{2}-[A-Z]{2}$/ },
        description: 'Default language for speech recognition',
        category: ConfigCategory.SPEECH
      },
      'speech.autoStart': {
        defaultValue: false,
        validation: { type: 'boolean' },
        description: 'Automatically start recording on voice activity detection',
        category: ConfigCategory.SPEECH
      },
      'speech.confidenceThreshold': {
        defaultValue: 0.7,
        validation: { type: 'number', min: 0, max: 1 },
        description: 'Minimum confidence level for accepting transcriptions',
        category: ConfigCategory.SPEECH
      },
      'speech.maxRecordingDuration': {
        defaultValue: 300, // 5 minutes
        validation: { type: 'number', min: 10, max: 3600 },
        description: 'Maximum recording duration in seconds',
        category: ConfigCategory.SPEECH
      },

      // API Keys (sensitive)
      'speech.googleCloudApiKey': {
        defaultValue: '',
        validation: { type: 'string' },
        description: 'Google Cloud Speech-to-Text API key',
        category: ConfigCategory.SPEECH,
        sensitive: true
      },
      'speech.azureApiKey': {
        defaultValue: '',
        validation: { type: 'string' },
        description: 'Azure Cognitive Services API key',
        category: ConfigCategory.SPEECH,
        sensitive: true
      },
      'speech.awsApiKey': {
        defaultValue: '',
        validation: { type: 'string' },
        description: 'AWS Transcribe API key',
        category: ConfigCategory.SPEECH,
        sensitive: true
      },
      'speech.openaiApiKey': {
        defaultValue: '',
        validation: { type: 'string' },
        description: 'OpenAI Whisper API key',
        category: ConfigCategory.SPEECH,
        sensitive: true
      },

      // ML Service Configuration
      'ml.enableVAD': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable Voice Activity Detection',
        category: ConfigCategory.ML
      },
      'ml.enableNoiseReduction': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable noise reduction',
        category: ConfigCategory.ML
      },
      'ml.enableSentimentAnalysis': {
        defaultValue: false,
        validation: { type: 'boolean' },
        description: 'Enable sentiment analysis of transcriptions',
        category: ConfigCategory.ML
      },
      'ml.enableSpeakerIdentification': {
        defaultValue: false,
        validation: { type: 'boolean' },
        description: 'Enable speaker identification',
        category: ConfigCategory.ML
      },
      'ml.vadSensitivity': {
        defaultValue: 0.5,
        validation: { type: 'number', min: 0, max: 1 },
        description: 'Voice Activity Detection sensitivity',
        category: ConfigCategory.ML
      },

      // WebRTC Configuration
      'webrtc.enableAutoConnect': {
        defaultValue: false,
        validation: { type: 'boolean' },
        description: 'Automatically connect to available peers',
        category: ConfigCategory.WEBRTC
      },
      'webrtc.maxConnections': {
        defaultValue: 5,
        validation: { type: 'number', min: 1, max: 20 },
        description: 'Maximum number of simultaneous connections',
        category: ConfigCategory.WEBRTC
      },
      'webrtc.enableScreenShare': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable screen sharing capability',
        category: ConfigCategory.WEBRTC
      },
      'webrtc.audioQuality': {
        defaultValue: 'high',
        validation: { 
          type: 'string', 
          enum: ['low', 'medium', 'high', 'highest'] 
        },
        description: 'Audio quality for WebRTC connections',
        category: ConfigCategory.WEBRTC
      },

      // Mobile Features Configuration
      'mobile.enableVibration': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable haptic feedback',
        category: ConfigCategory.MOBILE
      },
      'mobile.enableWakeLock': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Prevent screen sleep during recording',
        category: ConfigCategory.MOBILE
      },
      'mobile.enableLocationTracking': {
        defaultValue: false,
        validation: { type: 'boolean' },
        description: 'Enable location tracking for context',
        category: ConfigCategory.MOBILE
      },
      'mobile.defaultVibrationPattern': {
        defaultValue: VibrationPattern.SHORT,
        validation: { 
          type: 'string', 
          enum: Object.values(VibrationPattern) 
        },
        description: 'Default vibration pattern for notifications',
        category: ConfigCategory.MOBILE
      },

      // Cache Configuration
      'cache.strategy': {
        defaultValue: CacheStrategy.STALE_WHILE_REVALIDATE,
        validation: { 
          type: 'string', 
          enum: Object.values(CacheStrategy) 
        },
        description: 'Default caching strategy',
        category: ConfigCategory.CACHE
      },
      'cache.maxSize': {
        defaultValue: 100, // MB
        validation: { type: 'number', min: 10, max: 1000 },
        description: 'Maximum cache size in MB',
        category: ConfigCategory.CACHE
      },
      'cache.enableOfflineMode': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable offline functionality',
        category: ConfigCategory.CACHE
      },

      // Performance Configuration
      'performance.enableLazyLoading': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable lazy loading of images and components',
        category: ConfigCategory.PERFORMANCE
      },
      'performance.enableCodeSplitting': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable code splitting for better performance',
        category: ConfigCategory.PERFORMANCE
      },
      'performance.performanceThreshold': {
        defaultValue: 75,
        validation: { type: 'number', min: 0, max: 100 },
        description: 'Performance score threshold for warnings',
        category: ConfigCategory.PERFORMANCE
      },

      // UI Configuration
      'ui.theme': {
        defaultValue: 'auto',
        validation: { 
          type: 'string', 
          enum: ['light', 'dark', 'auto'] 
        },
        description: 'Application theme preference',
        category: ConfigCategory.UI
      },
      'ui.fontSize': {
        defaultValue: 'medium',
        validation: { 
          type: 'string', 
          enum: ['small', 'medium', 'large', 'xlarge'] 
        },
        description: 'Font size preference',
        category: ConfigCategory.UI
      },
      'ui.showConfidenceLevel': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Show confidence level in transcriptions',
        category: ConfigCategory.UI
      },
      'ui.showTimestamps': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Show timestamps in transcription history',
        category: ConfigCategory.UI
      },
      'ui.animationsEnabled': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable UI animations',
        category: ConfigCategory.UI
      },

      // Analytics Configuration
      'analytics.enableUsageTracking': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable usage analytics tracking',
        category: ConfigCategory.ANALYTICS
      },
      'analytics.enableErrorReporting': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable automatic error reporting',
        category: ConfigCategory.ANALYTICS
      },
      'analytics.dataRetentionDays': {
        defaultValue: 30,
        validation: { type: 'number', min: 1, max: 365 },
        description: 'Number of days to retain analytics data',
        category: ConfigCategory.ANALYTICS
      },

      // Logging Configuration
      'logging.level': {
        defaultValue: LogLevel.INFO,
        validation: { 
          type: 'number', 
          enum: Object.values(LogLevel).filter(v => typeof v === 'number') 
        },
        description: 'Logging level threshold',
        category: ConfigCategory.LOGGING
      },
      'logging.enableConsole': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable console logging',
        category: ConfigCategory.LOGGING
      },
      'logging.enableRemote': {
        defaultValue: false,
        validation: { type: 'boolean' },
        description: 'Enable remote logging',
        category: ConfigCategory.LOGGING
      },
      'logging.maxLocalLogs': {
        defaultValue: 1000,
        validation: { type: 'number', min: 100, max: 10000 },
        description: 'Maximum number of logs to store locally',
        category: ConfigCategory.LOGGING
      },

      // Hotkey Configuration
      'hotkeys.enabled': {
        defaultValue: true,
        validation: { type: 'boolean' },
        description: 'Enable keyboard shortcuts',
        category: ConfigCategory.HOTKEYS
      },
      'hotkeys.globalEnabled': {
        defaultValue: false,
        validation: { type: 'boolean' },
        description: 'Enable global keyboard shortcuts',
        category: ConfigCategory.HOTKEYS
      }
    };
  }

  /**
   * Load configurations from localStorage
   */
  private loadConfigurations(): void {
    try {
      // Load main configurations
      const stored = localStorage.getItem('voice-assistant-config');
      if (stored) {
        const parsed = JSON.parse(stored);
        Object.entries(parsed).forEach(([key, value]) => {
          this.configurations.set(key, value);
        });
      }

      // Load user preferences
      const preferences = localStorage.getItem('voice-assistant-preferences');
      if (preferences) {
        const parsed = JSON.parse(preferences);
        Object.entries(parsed).forEach(([key, value]) => {
          this.userPreferences.set(key, value);
        });
      }

      // Apply defaults for missing configurations
      Object.entries(this.schema).forEach(([key, config]) => {
        if (!this.configurations.has(key)) {
          this.configurations.set(key, config.defaultValue);
        }
      });

      this.emit('configurations-loaded');
    } catch (error) {
      console.error('Failed to load configurations:', error);
      this.loadDefaults();
    }
  }

  /**
   * Load default configurations
   */
  private loadDefaults(): void {
    Object.entries(this.schema).forEach(([key, config]) => {
      this.configurations.set(key, config.defaultValue);
    });
  }

  /**
   * Save configurations to localStorage
   */
  private saveConfigurations(): void {
    try {
      const config = Object.fromEntries(this.configurations.entries());
      const preferences = Object.fromEntries(this.userPreferences.entries());
      
      localStorage.setItem('voice-assistant-config', JSON.stringify(config));
      localStorage.setItem('voice-assistant-preferences', JSON.stringify(preferences));
      
      this.emit('configurations-saved');
    } catch (error) {
      console.error('Failed to save configurations:', error);
    }
  }

  /**
   * Get a configuration value
   */
  public get<T = any>(key: string, defaultValue?: T): T {
    if (this.configurations.has(key)) {
      return this.configurations.get(key) as T;
    }
    
    if (this.schema[key]) {
      return this.schema[key].defaultValue as T;
    }
    
    return defaultValue as T;
  }

  /**
   * Set a configuration value
   */
  public set(key: string, value: any): boolean {
    const oldValue = this.get(key);
    
    // Validate the value
    if (!this.validateValue(key, value)) {
      console.warn(`Invalid value for configuration '${key}':`, value);
      return false;
    }

    // Check if value actually changed
    if (JSON.stringify(oldValue) === JSON.stringify(value)) {
      return true; // No change needed
    }

    // Update configuration
    this.configurations.set(key, value);
    
    // Record change
    const changeEvent: ConfigChangeEvent = {
      category: this.schema[key]?.category || ConfigCategory.UI,
      key,
      oldValue,
      newValue: value,
      timestamp: new Date()
    };
    
    this.changeHistory.push(changeEvent);
    
    // Keep only recent changes
    if (this.changeHistory.length > 1000) {
      this.changeHistory = this.changeHistory.slice(-1000);
    }

    // Save to storage
    this.saveConfigurations();

    // Emit events
    this.emit('config-changed', changeEvent);
    this.emit(`config-changed:${key}`, changeEvent);
    this.emit(`config-changed:${changeEvent.category}`, changeEvent);

    return true;
  }

  /**
   * Get multiple configuration values by category
   */
  public getByCategory(category: ConfigCategory): Record<string, any> {
    const result: Record<string, any> = {};
    
    Object.entries(this.schema).forEach(([key, config]) => {
      if (config.category === category) {
        result[key] = this.get(key);
      }
    });
    
    return result;
  }

  /**
   * Set multiple configuration values
   */
  public setMany(configs: Record<string, any>): boolean {
    let allValid = true;
    
    // Validate all values first
    Object.entries(configs).forEach(([key, value]) => {
      if (!this.validateValue(key, value)) {
        allValid = false;
      }
    });

    if (!allValid) {
      return false;
    }

    // Apply all changes
    Object.entries(configs).forEach(([key, value]) => {
      this.set(key, value);
    });

    return true;
  }

  /**
   * Validate a configuration value
   */
  private validateValue(key: string, value: any): boolean {
    const schema = this.schema[key];
    if (!schema?.validation) {
      return true; // No validation rules
    }

    const validation = schema.validation;

    // Required check
    if (validation.required && (value === null || value === undefined || value === '')) {
      return false;
    }

    // Type check
    if (validation.type) {
      const actualType = Array.isArray(value) ? 'array' : typeof value;
      if (actualType !== validation.type) {
        return false;
      }
    }

    // Numeric constraints
    if (typeof value === 'number') {
      if (validation.min !== undefined && value < validation.min) {
        return false;
      }
      if (validation.max !== undefined && value > validation.max) {
        return false;
      }
    }

    // String pattern
    if (typeof value === 'string' && validation.pattern) {
      if (!validation.pattern.test(value)) {
        return false;
      }
    }

    // Enum check
    if (validation.enum && !validation.enum.includes(value)) {
      return false;
    }

    // Custom validation
    if (validation.custom) {
      const result = validation.custom(value);
      if (result !== true) {
        return false;
      }
    }

    return true;
  }

  /**
   * Reset a configuration to its default value
   */
  public reset(key: string): boolean {
    const schema = this.schema[key];
    if (!schema) {
      return false;
    }

    return this.set(key, schema.defaultValue);
  }

  /**
   * Reset all configurations to defaults
   */
  public resetAll(): void {
    Object.entries(this.schema).forEach(([key, config]) => {
      this.configurations.set(key, config.defaultValue);
    });

    this.saveConfigurations();
    this.emit('all-configurations-reset');
  }

  /**
   * Reset configurations by category
   */
  public resetByCategory(category: ConfigCategory): void {
    Object.entries(this.schema).forEach(([key, config]) => {
      if (config.category === category) {
        this.set(key, config.defaultValue);
      }
    });
  }

  /**
   * Get user preference
   */
  public getUserPreference<T = any>(key: string, defaultValue?: T): T {
    return this.userPreferences.get(key) ?? defaultValue;
  }

  /**
   * Set user preference
   */
  public setUserPreference(key: string, value: any): void {
    this.userPreferences.set(key, value);
    this.saveConfigurations();
    this.emit('user-preference-changed', { key, value });
  }

  /**
   * Export all configurations
   */
  public exportConfigurations(includeSensitive: boolean = false): ConfigExport {
    const configurations: Record<string, any> = {};
    
    // Group by category
    Object.values(ConfigCategory).forEach(category => {
      configurations[category] = {};
    });

    // Add configurations by category
    Object.entries(this.schema).forEach(([key, config]) => {
      if (!includeSensitive && config.sensitive) {
        return; // Skip sensitive data
      }
      
      const shortKey = key.replace(`${config.category}.`, '');
      configurations[config.category][shortKey] = this.get(key);
    });

    return {
      version: '1.0.0',
      timestamp: new Date(),
      deviceInfo: {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language
      },
      configurations,
      userPreferences: Object.fromEntries(this.userPreferences.entries())
    };
  }

  /**
   * Import configurations from export
   */
  public importConfigurations(data: ConfigExport): boolean {
    try {
      // Validate import data
      if (!data.configurations || !data.version) {
        throw new Error('Invalid configuration format');
      }

      // Import configurations by category
      Object.entries(data.configurations).forEach(([category, configs]) => {
        if (typeof configs === 'object') {
          Object.entries(configs).forEach(([shortKey, value]) => {
            const fullKey = `${category}.${shortKey}`;
            if (this.schema[fullKey]) {
              this.set(fullKey, value);
            }
          });
        }
      });

      // Import user preferences
      if (data.userPreferences) {
        Object.entries(data.userPreferences).forEach(([key, value]) => {
          this.setUserPreference(key, value);
        });
      }

      this.emit('configurations-imported', data);
      return true;
    } catch (error) {
      console.error('Failed to import configurations:', error);
      return false;
    }
  }

  /**
   * Get configuration schema
   */
  public getSchema(): ConfigSchema {
    return { ...this.schema };
  }

  /**
   * Get configuration change history
   */
  public getChangeHistory(): ConfigChangeEvent[] {
    return [...this.changeHistory];
  }

  /**
   * Get all configurations (non-sensitive)
   */
  public getAllConfigurations(includeSensitive: boolean = false): Record<string, any> {
    const result: Record<string, any> = {};
    
    Object.entries(this.schema).forEach(([key, config]) => {
      if (!includeSensitive && config.sensitive) {
        return;
      }
      result[key] = this.get(key);
    });
    
    return result;
  }

  /**
   * Validate all current configurations
   */
  public validateAllConfigurations(): { valid: boolean; errors: string[] } {
    const errors: string[] = [];
    
    this.configurations.forEach((value, key) => {
      if (!this.validateValue(key, value)) {
        errors.push(`Invalid value for '${key}': ${value}`);
      }
    });
    
    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Check if configuration requires application restart
   */
  public requiresRestart(key: string): boolean {
    return this.schema[key]?.requiresRestart || false;
  }

  /**
   * Get configurations that require restart
   */
  public getRestartRequiredConfigurations(): string[] {
    const changed = this.changeHistory
      .filter(change => this.requiresRestart(change.key))
      .map(change => change.key);
      
    return [...new Set(changed)]; // Remove duplicates
  }

  /**
   * Clear restart required flag
   */
  public clearRestartRequired(): void {
    this.changeHistory = this.changeHistory.filter(
      change => !this.requiresRestart(change.key)
    );
  }
}

// Export singleton instance
export const configService = new ConfigService();
export default configService;