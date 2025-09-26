import { EventEmitter } from 'events';
import { notificationService } from './notificationService';
import { analyticsService } from './analyticsService';

// Error severity levels
export enum ErrorSeverity {
  LOW = 'low',
  MEDIUM = 'medium',
  HIGH = 'high',
  CRITICAL = 'critical'
}

// Error categories
export enum ErrorCategory {
  SPEECH_SERVICE = 'speech-service',
  ML_SERVICE = 'ml-service',
  WEBRTC_SERVICE = 'webrtc-service',
  MOBILE_FEATURES = 'mobile-features',
  OFFLINE_CACHE = 'offline-cache',
  PERFORMANCE = 'performance',
  NETWORK = 'network',
  AUTHENTICATION = 'authentication',
  USER_INPUT = 'user-input',
  SYSTEM = 'system',
  UNKNOWN = 'unknown'
}

// Log levels
export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3,
  CRITICAL = 4
}

// Error context interface
export interface ErrorContext {
  userId?: string;
  sessionId: string;
  userAgent: string;
  url: string;
  timestamp: Date;
  stackTrace?: string;
  breadcrumbs: BreadcrumbItem[];
  deviceInfo: DeviceInfo;
  additionalData?: Record<string, any>;
}

// Breadcrumb for tracking user actions
export interface BreadcrumbItem {
  message: string;
  category: string;
  level: LogLevel;
  timestamp: Date;
  data?: Record<string, any>;
}

// Device information
export interface DeviceInfo {
  platform: string;
  browser: string;
  version: string;
  isMobile: boolean;
  screenResolution: string;
  language: string;
  timezone: string;
  connectionType?: string;
}

// Error report
export interface ErrorReport {
  id: string;
  message: string;
  severity: ErrorSeverity;
  category: ErrorCategory;
  context: ErrorContext;
  resolved: boolean;
  createdAt: Date;
  updatedAt: Date;
}

// Logger configuration
interface LoggerConfig {
  level: LogLevel;
  enableConsole: boolean;
  enableRemoteLogging: boolean;
  enableLocalStorage: boolean;
  maxBreadcrumbs: number;
  maxLocalLogs: number;
  remoteEndpoint?: string;
  apiKey?: string;
}

class ErrorHandlingService extends EventEmitter {
  private config: LoggerConfig = {
    level: LogLevel.INFO,
    enableConsole: true,
    enableRemoteLogging: false,
    enableLocalStorage: true,
    maxBreadcrumbs: 100,
    maxLocalLogs: 1000,
    remoteEndpoint: undefined,
    apiKey: undefined
  };

  private breadcrumbs: BreadcrumbItem[] = [];
  private sessionId: string;
  private deviceInfo: DeviceInfo;
  private errorReports: Map<string, ErrorReport> = new Map();

  constructor() {
    super();
    this.sessionId = this.generateSessionId();
    this.deviceInfo = this.collectDeviceInfo();
    this.setupGlobalErrorHandlers();
    this.loadStoredLogs();
  }

  /**
   * Initialize the error handling service
   */
  public initialize(config?: Partial<LoggerConfig>): void {
    this.config = { ...this.config, ...config };
    
    // Set up periodic log cleanup
    setInterval(() => {
      this.cleanupOldLogs();
    }, 5 * 60 * 1000); // Every 5 minutes

    this.addBreadcrumb('ErrorHandlingService initialized', ErrorCategory.SYSTEM, LogLevel.INFO);
  }

  /**
   * Setup global error handlers
   */
  private setupGlobalErrorHandlers(): void {
    // Catch unhandled JavaScript errors
    window.addEventListener('error', (event) => {
      this.handleError(new Error(event.message), {
        severity: ErrorSeverity.HIGH,
        category: ErrorCategory.SYSTEM,
        context: {
          filename: event.filename,
          lineno: event.lineno,
          colno: event.colno,
          stack: event.error?.stack
        }
      });
    });

    // Catch unhandled promise rejections
    window.addEventListener('unhandledrejection', (event) => {
      this.handleError(new Error('Unhandled Promise Rejection: ' + event.reason), {
        severity: ErrorSeverity.HIGH,
        category: ErrorCategory.SYSTEM,
        context: {
          reason: event.reason
        }
      });
    });

    // Catch network errors
    window.addEventListener('offline', () => {
      this.addBreadcrumb('Network went offline', ErrorCategory.NETWORK, LogLevel.WARN);
    });

    window.addEventListener('online', () => {
      this.addBreadcrumb('Network came back online', ErrorCategory.NETWORK, LogLevel.INFO);
    });
  }

  /**
   * Handle an error with context
   */
  public handleError(error: Error | string, options?: {
    severity?: ErrorSeverity;
    category?: ErrorCategory;
    context?: Record<string, any>;
    showNotification?: boolean;
    reportToAnalytics?: boolean;
  }): string {
    const errorMessage = error instanceof Error ? error.message : error;
    const errorStack = error instanceof Error ? error.stack : undefined;
    
    const severity = options?.severity || ErrorSeverity.MEDIUM;
    const category = options?.category || ErrorCategory.UNKNOWN;
    const showNotification = options?.showNotification !== false;
    const reportToAnalytics = options?.reportToAnalytics !== false;

    // Create error context
    const context: ErrorContext = {
      sessionId: this.sessionId,
      userAgent: navigator.userAgent,
      url: window.location.href,
      timestamp: new Date(),
      stackTrace: errorStack,
      breadcrumbs: [...this.breadcrumbs],
      deviceInfo: this.deviceInfo,
      additionalData: options?.context
    };

    // Create error report
    const report: ErrorReport = {
      id: this.generateErrorId(),
      message: errorMessage,
      severity,
      category,
      context,
      resolved: false,
      createdAt: new Date(),
      updatedAt: new Date()
    };

    // Store error report
    this.errorReports.set(report.id, report);

    // Log the error
    this.log(LogLevel.ERROR, errorMessage, {
      category,
      severity,
      errorId: report.id,
      stack: errorStack,
      ...options?.context
    });

    // Show notification based on severity
    if (showNotification) {
      this.showErrorNotification(report);
    }

    // Report to analytics
    if (reportToAnalytics) {
      this.reportToAnalytics(report);
    }

    // Emit error event
    this.emit('error-handled', report);

    // Store error locally
    this.storeErrorLocally(report);

    // Send to remote logging if enabled
    if (this.config.enableRemoteLogging) {
      this.sendToRemoteLogging(report);
    }

    return report.id;
  }

  /**
   * Log a message with level and context
   */
  public log(level: LogLevel, message: string, data?: Record<string, any>): void {
    if (level < this.config.level) {
      return; // Skip logs below configured level
    }

    const logEntry = {
      level,
      message,
      timestamp: new Date(),
      sessionId: this.sessionId,
      data: data || {}
    };

    // Console logging
    if (this.config.enableConsole) {
      this.logToConsole(logEntry);
    }

    // Add to breadcrumbs
    this.addBreadcrumb(message, data?.category || ErrorCategory.SYSTEM, level, data);

    // Store locally
    if (this.config.enableLocalStorage) {
      this.storeLogLocally(logEntry);
    }

    // Emit log event
    this.emit('log-entry', logEntry);
  }

  /**
   * Convenience logging methods
   */
  public debug(message: string, data?: Record<string, any>): void {
    this.log(LogLevel.DEBUG, message, data);
  }

  public info(message: string, data?: Record<string, any>): void {
    this.log(LogLevel.INFO, message, data);
  }

  public warn(message: string, data?: Record<string, any>): void {
    this.log(LogLevel.WARN, message, data);
  }

  public error(message: string, data?: Record<string, any>): void {
    this.log(LogLevel.ERROR, message, data);
  }

  public critical(message: string, data?: Record<string, any>): void {
    this.log(LogLevel.CRITICAL, message, data);
  }

  /**
   * Add breadcrumb for tracking user actions
   */
  public addBreadcrumb(message: string, category: ErrorCategory | string, level: LogLevel = LogLevel.INFO, data?: Record<string, any>): void {
    const breadcrumb: BreadcrumbItem = {
      message,
      category: typeof category === 'string' ? category : category,
      level,
      timestamp: new Date(),
      data
    };

    this.breadcrumbs.push(breadcrumb);

    // Keep only recent breadcrumbs
    if (this.breadcrumbs.length > this.config.maxBreadcrumbs) {
      this.breadcrumbs = this.breadcrumbs.slice(-this.config.maxBreadcrumbs);
    }
  }

  /**
   * Log to console with appropriate styling
   */
  private logToConsole(entry: any): void {
    const timestamp = entry.timestamp.toISOString();
    const prefix = `[${timestamp}] [${LogLevel[entry.level]}]`;
    
    switch (entry.level) {
      case LogLevel.DEBUG:
        console.debug(`%c${prefix}`, 'color: #6b7280', entry.message, entry.data);
        break;
      case LogLevel.INFO:
        console.info(`%c${prefix}`, 'color: #3b82f6', entry.message, entry.data);
        break;
      case LogLevel.WARN:
        console.warn(`%c${prefix}`, 'color: #f59e0b', entry.message, entry.data);
        break;
      case LogLevel.ERROR:
        console.error(`%c${prefix}`, 'color: #ef4444', entry.message, entry.data);
        break;
      case LogLevel.CRITICAL:
        console.error(`%c${prefix}`, 'color: #dc2626; font-weight: bold', entry.message, entry.data);
        break;
    }
  }

  /**
   * Show error notification based on severity
   */
  private showErrorNotification(report: ErrorReport): void {
    let notificationPriority: 'high' | 'normal' | 'low' = 'high';
    let title = 'An error occurred';

    switch (report.severity) {
      case ErrorSeverity.LOW:
        notificationPriority = 'low';
        title = 'Minor issue detected';
        break;
      case ErrorSeverity.MEDIUM:
        notificationPriority = 'normal';
        title = 'Warning';
        break;
      case ErrorSeverity.HIGH:
      case ErrorSeverity.CRITICAL:
        notificationPriority = 'high';
        title = 'Error';
        break;
    }

    // Only show critical errors to avoid overwhelming users
    if (report.severity === ErrorSeverity.CRITICAL) {
      notificationService.showNotification({
        title,
        body: report.message,
        type: 'system',
        priority: notificationPriority
      });
    }
  }

  /**
   * Report error to analytics
   */
  private reportToAnalytics(report: ErrorReport): void {
    try {
      analyticsService.trackError(new Error(report.message), JSON.stringify({
        errorId: report.id,
        category: report.category,
        severity: report.severity,
        url: report.context.url,
        userAgent: report.context.userAgent,
        timestamp: report.createdAt
      }));
    } catch (error) {
      console.warn('Failed to report error to analytics:', error);
    }
  }

  /**
   * Store error locally for offline access
   */
  private storeErrorLocally(report: ErrorReport): void {
    try {
      const stored = this.getStoredErrors();
      stored.push(report);
      
      // Keep only recent errors
      const recent = stored.slice(-this.config.maxLocalLogs);
      localStorage.setItem('voice-assistant-errors', JSON.stringify(recent));
    } catch (error) {
      console.warn('Failed to store error locally:', error);
    }
  }

  /**
   * Store log entry locally
   */
  private storeLogLocally(entry: any): void {
    try {
      const stored = this.getStoredLogs();
      stored.push(entry);
      
      // Keep only recent logs
      const recent = stored.slice(-this.config.maxLocalLogs);
      localStorage.setItem('voice-assistant-logs', JSON.stringify(recent));
    } catch (error) {
      console.warn('Failed to store log locally:', error);
    }
  }

  /**
   * Send error to remote logging service
   */
  private async sendToRemoteLogging(report: ErrorReport): Promise<void> {
    if (!this.config.remoteEndpoint || !this.config.apiKey) {
      return;
    }

    try {
      await fetch(this.config.remoteEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.config.apiKey}`
        },
        body: JSON.stringify(report)
      });
    } catch (error) {
      console.warn('Failed to send error to remote logging:', error);
    }
  }

  /**
   * Get stored errors from localStorage
   */
  private getStoredErrors(): ErrorReport[] {
    try {
      const stored = localStorage.getItem('voice-assistant-errors');
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  }

  /**
   * Get stored logs from localStorage
   */
  private getStoredLogs(): any[] {
    try {
      const stored = localStorage.getItem('voice-assistant-logs');
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  }

  /**
   * Load stored logs on initialization
   */
  private loadStoredLogs(): void {
    const storedErrors = this.getStoredErrors();
    storedErrors.forEach(report => {
      this.errorReports.set(report.id, report);
    });
  }

  /**
   * Clean up old logs and errors
   */
  private cleanupOldLogs(): void {
    const maxAge = 7 * 24 * 60 * 60 * 1000; // 7 days
    const cutoff = Date.now() - maxAge;

    // Clean up error reports
    for (const [id, report] of this.errorReports) {
      if (report.createdAt.getTime() < cutoff) {
        this.errorReports.delete(id);
      }
    }

    // Clean up stored logs
    try {
      const logs = this.getStoredLogs().filter(log => 
        new Date(log.timestamp).getTime() > cutoff
      );
      localStorage.setItem('voice-assistant-logs', JSON.stringify(logs));

      const errors = this.getStoredErrors().filter(error => 
        new Date(error.createdAt).getTime() > cutoff
      );
      localStorage.setItem('voice-assistant-errors', JSON.stringify(errors));
    } catch (error) {
      console.warn('Failed to cleanup old logs:', error);
    }
  }

  /**
   * Generate unique session ID
   */
  private generateSessionId(): string {
    return `session_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Generate unique error ID
   */
  private generateErrorId(): string {
    return `error_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Collect device information
   */
  private collectDeviceInfo(): DeviceInfo {
    const connection = (navigator as any).connection || (navigator as any).mozConnection || (navigator as any).webkitConnection;
    
    return {
      platform: navigator.platform,
      browser: this.getBrowserName(),
      version: this.getBrowserVersion(),
      isMobile: /Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent),
      screenResolution: `${screen.width}x${screen.height}`,
      language: navigator.language,
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      connectionType: connection?.effectiveType || 'unknown'
    };
  }

  /**
   * Get browser name
   */
  private getBrowserName(): string {
    const userAgent = navigator.userAgent;
    if (userAgent.indexOf('Chrome') > -1) return 'Chrome';
    if (userAgent.indexOf('Firefox') > -1) return 'Firefox';
    if (userAgent.indexOf('Safari') > -1) return 'Safari';
    if (userAgent.indexOf('Edge') > -1) return 'Edge';
    if (userAgent.indexOf('Opera') > -1) return 'Opera';
    return 'Unknown';
  }

  /**
   * Get browser version
   */
  private getBrowserVersion(): string {
    const userAgent = navigator.userAgent;
    const match = userAgent.match(/(chrome|firefox|safari|edge|opera)\/?([\d.]+)/i);
    return match ? match[2] : 'Unknown';
  }

  /**
   * Get all error reports
   */
  public getErrorReports(): ErrorReport[] {
    return Array.from(this.errorReports.values());
  }

  /**
   * Get error report by ID
   */
  public getErrorReport(id: string): ErrorReport | undefined {
    return this.errorReports.get(id);
  }

  /**
   * Mark error as resolved
   */
  public resolveError(id: string): void {
    const report = this.errorReports.get(id);
    if (report) {
      report.resolved = true;
      report.updatedAt = new Date();
      this.emit('error-resolved', report);
    }
  }

  /**
   * Get current breadcrumbs
   */
  public getBreadcrumbs(): BreadcrumbItem[] {
    return [...this.breadcrumbs];
  }

  /**
   * Export logs and errors for debugging
   */
  public exportDebugData(): string {
    const debugData = {
      sessionId: this.sessionId,
      deviceInfo: this.deviceInfo,
      breadcrumbs: this.breadcrumbs,
      errorReports: Array.from(this.errorReports.values()),
      logs: this.getStoredLogs(),
      timestamp: new Date().toISOString()
    };

    return JSON.stringify(debugData, null, 2);
  }

  /**
   * Set configuration
   */
  public setConfig(config: Partial<LoggerConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * Get current configuration
   */
  public getConfig(): LoggerConfig {
    return { ...this.config };
  }

  /**
   * Clear all logs and errors
   */
  public clearAll(): void {
    this.breadcrumbs = [];
    this.errorReports.clear();
    localStorage.removeItem('voice-assistant-logs');
    localStorage.removeItem('voice-assistant-errors');
    this.emit('logs-cleared');
  }
}

// Export singleton instance
export const errorHandlingService = new ErrorHandlingService();
export default errorHandlingService;