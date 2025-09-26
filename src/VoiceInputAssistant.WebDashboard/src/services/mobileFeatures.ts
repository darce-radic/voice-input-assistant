import { EventEmitter } from 'events';
import { notificationService } from './notificationService';

// Device capabilities
export interface DeviceCapabilities {
  hasCamera: boolean;
  hasMicrophone: boolean;
  hasVibration: boolean;
  hasGeolocation: boolean;
  hasAccelerometer: boolean;
  hasGyroscope: boolean;
  hasNotifications: boolean;
  hasBackgroundSync: boolean;
  hasWakeLock: boolean;
  isTouchDevice: boolean;
  supportsInstallPrompt: boolean;
}

// Camera configuration
export interface CameraConfig {
  facingMode: 'user' | 'environment';
  width?: number;
  height?: number;
  aspectRatio?: number;
  frameRate?: number;
}

// Vibration patterns
export enum VibrationPattern {
  SHORT = 'short',
  DOUBLE = 'double',
  LONG = 'long',
  SUCCESS = 'success',
  ERROR = 'error',
  WARNING = 'warning',
  NOTIFICATION = 'notification',
  RECORDING_START = 'recording-start',
  RECORDING_STOP = 'recording-stop'
}

// Device orientation
export interface OrientationData {
  alpha: number; // Z-axis rotation
  beta: number;  // X-axis rotation
  gamma: number; // Y-axis rotation
  absolute: boolean;
}

// Background sync task
export interface BackgroundSyncTask {
  id: string;
  tag: string;
  data: any;
  timestamp: number;
  retries: number;
  maxRetries: number;
}

// Wake lock types
export enum WakeLockType {
  SCREEN = 'screen'
}

class MobileFeaturesService extends EventEmitter {
  private capabilities: DeviceCapabilities | null = null;
  private currentStream: MediaStream | null = null;
  private wakeLock: any = null;
  private backgroundSyncTasks: Map<string, BackgroundSyncTask> = new Map();
  private orientationListener: ((event: DeviceOrientationEvent) => void) | null = null;
  private installPromptEvent: any = null;

  // Vibration patterns in milliseconds
  private vibrationPatterns = {
    [VibrationPattern.SHORT]: [100],
    [VibrationPattern.DOUBLE]: [100, 100, 100],
    [VibrationPattern.LONG]: [500],
    [VibrationPattern.SUCCESS]: [100, 50, 100],
    [VibrationPattern.ERROR]: [200, 100, 200, 100, 200],
    [VibrationPattern.WARNING]: [150, 100, 150],
    [VibrationPattern.NOTIFICATION]: [100, 200, 100],
    [VibrationPattern.RECORDING_START]: [50, 50, 150],
    [VibrationPattern.RECORDING_STOP]: [150, 50, 50]
  };

  constructor() {
    super();
    this.detectCapabilities();
    this.setupEventListeners();
  }

  /**
   * Detect device capabilities
   */
  private async detectCapabilities(): Promise<void> {
    this.capabilities = {
      hasCamera: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia),
      hasMicrophone: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia),
      hasVibration: !!(navigator.vibrate || (navigator as any).webkitVibrate),
      hasGeolocation: !!navigator.geolocation,
      hasAccelerometer: !!window.DeviceMotionEvent,
      hasGyroscope: !!window.DeviceOrientationEvent,
      hasNotifications: 'Notification' in window,
      hasBackgroundSync: 'serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype,
      hasWakeLock: 'wakeLock' in navigator,
      isTouchDevice: 'ontouchstart' in window || navigator.maxTouchPoints > 0,
      supportsInstallPrompt: false // Will be updated when beforeinstallprompt is fired
    };

    // Test for actual camera/microphone access
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      this.capabilities.hasCamera = devices.some(device => device.kind === 'videoinput');
      this.capabilities.hasMicrophone = devices.some(device => device.kind === 'audioinput');
    } catch (error) {
      console.warn('Could not enumerate devices:', error);
    }

    this.emit('capabilities-detected', this.capabilities);
  }

  /**
   * Setup event listeners
   */
  private setupEventListeners(): void {
    // Install prompt detection
    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault();
      this.installPromptEvent = e;
      if (this.capabilities) {
        this.capabilities.supportsInstallPrompt = true;
      }
      this.emit('install-prompt-available');
    });

    // App installed
    window.addEventListener('appinstalled', () => {
      this.installPromptEvent = null;
      this.emit('app-installed');
    });

    // Visibility change for background sync
    document.addEventListener('visibilitychange', () => {
      if (!document.hidden) {
        this.processPendingBackgroundTasks();
      }
    });

    // Online/offline for background sync
    window.addEventListener('online', () => {
      this.processPendingBackgroundTasks();
    });
  }

  /**
   * Get device capabilities
   */
  public getCapabilities(): DeviceCapabilities | null {
    return this.capabilities;
  }

  /**
   * Request camera access
   */
  public async requestCamera(config: CameraConfig = { facingMode: 'user' }): Promise<MediaStream> {
    if (!this.capabilities?.hasCamera) {
      throw new Error('Camera not available on this device');
    }

    try {
      const constraints: MediaStreamConstraints = {
        video: {
          facingMode: config.facingMode,
          width: config.width,
          height: config.height,
          aspectRatio: config.aspectRatio,
          frameRate: config.frameRate
        }
      };

      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      this.currentStream = stream;
      this.emit('camera-started', { stream, config });
      return stream;
    } catch (error) {
      this.emit('camera-error', error);
      throw error;
    }
  }

  /**
   * Stop camera
   */
  public stopCamera(): void {
    if (this.currentStream) {
      this.currentStream.getTracks().forEach(track => track.stop());
      this.currentStream = null;
      this.emit('camera-stopped');
    }
  }

  /**
   * Take photo from current stream
   */
  public takePhoto(width: number = 640, height: number = 480): Promise<Blob> {
    return new Promise((resolve, reject) => {
      if (!this.currentStream) {
        reject(new Error('No active camera stream'));
        return;
      }

      const video = document.createElement('video');
      const canvas = document.createElement('canvas');
      const context = canvas.getContext('2d');

      if (!context) {
        reject(new Error('Could not get canvas context'));
        return;
      }

      video.srcObject = this.currentStream;
      video.play();

      video.addEventListener('loadedmetadata', () => {
        canvas.width = width;
        canvas.height = height;
        
        context.drawImage(video, 0, 0, width, height);
        
        canvas.toBlob((blob) => {
          if (blob) {
            this.emit('photo-taken', { blob, width, height });
            resolve(blob);
          } else {
            reject(new Error('Failed to create photo blob'));
          }
        }, 'image/jpeg', 0.9);
      });

      video.addEventListener('error', (error) => {
        reject(error);
      });
    });
  }

  /**
   * Vibrate device
   */
  public vibrate(pattern: VibrationPattern | number | number[]): boolean {
    if (!this.capabilities?.hasVibration) {
      console.warn('Vibration not supported on this device');
      return false;
    }

    let vibrationPattern: number | number[];

    if (typeof pattern === 'string') {
      vibrationPattern = this.vibrationPatterns[pattern] || [100];
    } else {
      vibrationPattern = pattern;
    }

    try {
      const vibrated = navigator.vibrate(vibrationPattern);
      if (vibrated) {
        this.emit('vibration-started', { pattern: vibrationPattern });
      }
      return vibrated;
    } catch (error) {
      this.emit('vibration-error', error);
      return false;
    }
  }

  /**
   * Get current location
   */
  public getCurrentLocation(options: PositionOptions = {}): Promise<GeolocationPosition> {
    return new Promise((resolve, reject) => {
      if (!this.capabilities?.hasGeolocation) {
        reject(new Error('Geolocation not available on this device'));
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position) => {
          this.emit('location-obtained', position);
          resolve(position);
        },
        (error) => {
          this.emit('location-error', error);
          reject(error);
        },
        {
          enableHighAccuracy: true,
          timeout: 10000,
          maximumAge: 5 * 60 * 1000, // 5 minutes
          ...options
        }
      );
    });
  }

  /**
   * Watch location changes
   */
  public watchLocation(options: PositionOptions = {}): number {
    if (!this.capabilities?.hasGeolocation) {
      throw new Error('Geolocation not available on this device');
    }

    return navigator.geolocation.watchPosition(
      (position) => {
        this.emit('location-changed', position);
      },
      (error) => {
        this.emit('location-error', error);
      },
      {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 30000,
        ...options
      }
    );
  }

  /**
   * Clear location watch
   */
  public clearLocationWatch(watchId: number): void {
    navigator.geolocation.clearWatch(watchId);
    this.emit('location-watch-cleared', { watchId });
  }

  /**
   * Start orientation monitoring
   */
  public startOrientationMonitoring(): void {
    if (!this.capabilities?.hasGyroscope) {
      throw new Error('Device orientation not supported');
    }

    this.orientationListener = (event: DeviceOrientationEvent) => {
      const orientationData: OrientationData = {
        alpha: event.alpha || 0,
        beta: event.beta || 0,
        gamma: event.gamma || 0,
        absolute: event.absolute || false
      };

      this.emit('orientation-changed', orientationData);
    };

    window.addEventListener('deviceorientation', this.orientationListener);
    this.emit('orientation-monitoring-started');
  }

  /**
   * Stop orientation monitoring
   */
  public stopOrientationMonitoring(): void {
    if (this.orientationListener) {
      window.removeEventListener('deviceorientation', this.orientationListener);
      this.orientationListener = null;
      this.emit('orientation-monitoring-stopped');
    }
  }

  /**
   * Request wake lock
   */
  public async requestWakeLock(type: WakeLockType = WakeLockType.SCREEN): Promise<void> {
    if (!this.capabilities?.hasWakeLock) {
      throw new Error('Wake Lock API not supported');
    }

    try {
      this.wakeLock = await (navigator as any).wakeLock.request(type);
      
      this.wakeLock.addEventListener('release', () => {
        this.emit('wake-lock-released');
        this.wakeLock = null;
      });

      this.emit('wake-lock-acquired', { type });
    } catch (error) {
      this.emit('wake-lock-error', error);
      throw error;
    }
  }

  /**
   * Release wake lock
   */
  public async releaseWakeLock(): Promise<void> {
    if (this.wakeLock) {
      await this.wakeLock.release();
      this.wakeLock = null;
      this.emit('wake-lock-released');
    }
  }

  /**
   * Show install prompt
   */
  public async showInstallPrompt(): Promise<{ outcome: 'accepted' | 'dismissed' }> {
    if (!this.installPromptEvent) {
      throw new Error('Install prompt not available');
    }

    const result = await this.installPromptEvent.prompt();
    this.installPromptEvent = null;

    this.emit('install-prompt-shown', result);
    return result;
  }

  /**
   * Register background sync task
   */
  public async registerBackgroundSync(tag: string, data?: any): Promise<void> {
    if (!this.capabilities?.hasBackgroundSync) {
      // Fallback to storing tasks locally
      this.addBackgroundTask(tag, data);
      return;
    }

    try {
      const registration = await navigator.serviceWorker.ready;
      await (registration as any).sync.register(tag);
      
      this.addBackgroundTask(tag, data);
      this.emit('background-sync-registered', { tag, data });
    } catch (error) {
      this.emit('background-sync-error', { tag, error });
      throw error;
    }
  }

  /**
   * Add background task
   */
  private addBackgroundTask(tag: string, data?: any): void {
    const task: BackgroundSyncTask = {
      id: `${tag}_${Date.now()}`,
      tag,
      data,
      timestamp: Date.now(),
      retries: 0,
      maxRetries: 3
    };

    this.backgroundSyncTasks.set(task.id, task);
    this.saveBackgroundTasks();
    this.emit('background-task-added', task);
  }

  /**
   * Process pending background tasks
   */
  private async processPendingBackgroundTasks(): Promise<void> {
    if (this.backgroundSyncTasks.size === 0) {
      return;
    }

    this.emit('background-tasks-processing-started', { 
      count: this.backgroundSyncTasks.size 
    });

    for (const [id, task] of this.backgroundSyncTasks) {
      try {
        await this.executeBackgroundTask(task);
        this.backgroundSyncTasks.delete(id);
        this.emit('background-task-completed', task);
      } catch (error) {
        task.retries++;
        if (task.retries >= task.maxRetries) {
          this.backgroundSyncTasks.delete(id);
          this.emit('background-task-failed', { task, error });
        } else {
          this.emit('background-task-retry', { task, error });
        }
      }
    }

    this.saveBackgroundTasks();
    this.emit('background-tasks-processing-completed');
  }

  /**
   * Execute background task
   */
  private async executeBackgroundTask(task: BackgroundSyncTask): Promise<void> {
    // This is a placeholder - implement specific task execution based on tag
    switch (task.tag) {
      case 'voice-upload':
        await this.executeVoiceUploadTask(task.data);
        break;
      case 'settings-sync':
        await this.executeSettingsSyncTask(task.data);
        break;
      case 'analytics-sync':
        await this.executeAnalyticsSyncTask(task.data);
        break;
      default:
        console.warn(`Unknown background task tag: ${task.tag}`);
    }
  }

  /**
   * Execute voice upload task
   */
  private async executeVoiceUploadTask(data: any): Promise<void> {
    // Implement voice file upload logic
    this.emit('voice-upload-completed', data);
  }

  /**
   * Execute settings sync task
   */
  private async executeSettingsSyncTask(data: any): Promise<void> {
    // Implement settings synchronization logic
    this.emit('settings-sync-completed', data);
  }

  /**
   * Execute analytics sync task
   */
  private async executeAnalyticsSyncTask(data: any): Promise<void> {
    // Implement analytics data synchronization logic
    this.emit('analytics-sync-completed', data);
  }

  /**
   * Save background tasks to localStorage
   */
  private saveBackgroundTasks(): void {
    try {
      const tasks = Array.from(this.backgroundSyncTasks.values());
      localStorage.setItem('mobile_background_tasks', JSON.stringify(tasks));
    } catch (error) {
      console.warn('Failed to save background tasks:', error);
    }
  }

  /**
   * Load background tasks from localStorage
   */
  private loadBackgroundTasks(): void {
    try {
      const saved = localStorage.getItem('mobile_background_tasks');
      if (saved) {
        const tasks: BackgroundSyncTask[] = JSON.parse(saved);
        this.backgroundSyncTasks.clear();
        tasks.forEach(task => {
          this.backgroundSyncTasks.set(task.id, task);
        });
      }
    } catch (error) {
      console.warn('Failed to load background tasks:', error);
    }
  }

  /**
   * Request permissions for mobile features
   */
  public async requestPermissions(): Promise<{ [key: string]: PermissionState }> {
    const permissions: { [key: string]: PermissionState } = {};

    if (this.capabilities?.hasNotifications) {
      try {
        const permission = await Notification.requestPermission();
        permissions.notifications = permission as PermissionState;
      } catch (error) {
        permissions.notifications = 'denied';
      }
    }

    if (this.capabilities?.hasCamera) {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        stream.getTracks().forEach(track => track.stop());
        permissions.camera = 'granted';
      } catch (error) {
        permissions.camera = 'denied';
      }
    }

    if (this.capabilities?.hasMicrophone) {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        stream.getTracks().forEach(track => track.stop());
        permissions.microphone = 'granted';
      } catch (error) {
        permissions.microphone = 'denied';
      }
    }

    if (this.capabilities?.hasGeolocation) {
      try {
        await this.getCurrentLocation();
        permissions.geolocation = 'granted';
      } catch (error) {
        permissions.geolocation = 'denied';
      }
    }

    this.emit('permissions-requested', permissions);
    return permissions;
  }

  /**
   * Get battery information
   */
  public async getBatteryInfo(): Promise<any> {
    if ('getBattery' in navigator) {
      try {
        const battery = await (navigator as any).getBattery();
        const batteryInfo = {
          charging: battery.charging,
          level: battery.level,
          chargingTime: battery.chargingTime,
          dischargingTime: battery.dischargingTime
        };
        
        this.emit('battery-info-obtained', batteryInfo);
        return batteryInfo;
      } catch (error) {
        this.emit('battery-info-error', error);
        throw error;
      }
    } else {
      throw new Error('Battery API not supported');
    }
  }

  /**
   * Share content using native sharing
   */
  public async shareContent(data: ShareData): Promise<void> {
    if ('share' in navigator) {
      try {
        await (navigator as any).share(data);
        this.emit('content-shared', data);
      } catch (error) {
        this.emit('share-error', error);
        throw error;
      }
    } else {
      // Fallback to clipboard
      if (data.text && 'clipboard' in navigator) {
        await (navigator as any).clipboard.writeText(data.text);
        this.emit('content-copied-to-clipboard', data);
      } else {
        throw new Error('Web Share API not supported');
      }
    }
  }

  /**
   * Add to home screen prompt
   */
  public showAddToHomeScreen(): void {
    // iOS Safari
    if (this.isIOSSafari()) {
      this.emit('ios-add-to-homescreen-prompt');
      return;
    }

    // Android Chrome
    if (this.installPromptEvent) {
      this.showInstallPrompt();
    } else {
      this.emit('add-to-homescreen-not-available');
    }
  }

  /**
   * Check if device is iOS Safari
   */
  private isIOSSafari(): boolean {
    const userAgent = navigator.userAgent;
    const isIOS = /iPad|iPhone|iPod/.test(userAgent);
    const isSafari = /Safari/.test(userAgent) && !/Chrome|CriOS|OPiOS|FxiOS/.test(userAgent);
    return isIOS && isSafari;
  }

  /**
   * Initialize mobile features
   */
  public async initialize(): Promise<void> {
    this.loadBackgroundTasks();
    
    // Process any pending background tasks
    if (navigator.onLine && !document.hidden) {
      await this.processPendingBackgroundTasks();
    }

    this.emit('mobile-features-initialized');
  }

  /**
   * Cleanup mobile features
   */
  public destroy(): void {
    this.stopCamera();
    this.stopOrientationMonitoring();
    this.releaseWakeLock();
    this.removeAllListeners();
  }
}

// Export singleton instance
export const mobileFeaturesService = new MobileFeaturesService();
export default mobileFeaturesService;