/**
 * PWA utility functions for offline functionality, device features, and app-like behavior
 */

// PWA Installation and Updates
export class PWAManager {
  private deferredPrompt: any = null;
  private isInstalled = false;
  private updateAvailable = false;

  constructor() {
    this.init();
  }

  private init() {
    // Listen for beforeinstallprompt event
    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault();
      this.deferredPrompt = e;
      this.notifyInstallAvailable();
    });

    // Listen for app installed event
    window.addEventListener('appinstalled', () => {
      this.isInstalled = true;
      this.notifyAppInstalled();
    });

    // Check if already installed
    if (window.matchMedia && window.matchMedia('(display-mode: standalone)').matches) {
      this.isInstalled = true;
    }

    // Register service worker update listener
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.addEventListener('message', (event) => {
        if (event.data && event.data.type === 'UPDATE_AVAILABLE') {
          this.updateAvailable = true;
          this.notifyUpdateAvailable();
        }
      });
    }
  }

  async installApp(): Promise<boolean> {
    if (!this.deferredPrompt) {
      return false;
    }

    try {
      this.deferredPrompt.prompt();
      const { outcome } = await this.deferredPrompt.userChoice;
      
      if (outcome === 'accepted') {
        this.deferredPrompt = null;
        return true;
      }
      
      return false;
    } catch (error) {
      console.error('Error installing PWA:', error);
      return false;
    }
  }

  async updateApp(): Promise<void> {
    if ('serviceWorker' in navigator) {
      const registration = await navigator.serviceWorker.getRegistration();
      if (registration && registration.waiting) {
        registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        window.location.reload();
      }
    }
  }

  canInstall(): boolean {
    return !!this.deferredPrompt && !this.isInstalled;
  }

  isAppInstalled(): boolean {
    return this.isInstalled;
  }

  hasUpdateAvailable(): boolean {
    return this.updateAvailable;
  }

  private notifyInstallAvailable() {
    // Dispatch custom event for UI components to listen
    window.dispatchEvent(new CustomEvent('pwa-install-available'));
  }

  private notifyAppInstalled() {
    window.dispatchEvent(new CustomEvent('pwa-installed'));
  }

  private notifyUpdateAvailable() {
    window.dispatchEvent(new CustomEvent('pwa-update-available'));
  }
}

// Offline Data Management
export class OfflineManager {
  private dbName = 'VoiceAssistantDB';
  private dbVersion = 1;
  private db: IDBDatabase | null = null;

  async init(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, this.dbVersion);

      request.onerror = () => reject(request.error);
      request.onsuccess = () => {
        this.db = request.result;
        resolve();
      };

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;

        // Create object stores for offline data
        if (!db.objectStoreNames.contains('analytics')) {
          const analyticsStore = db.createObjectStore('analytics', { keyPath: 'id', autoIncrement: true });
          analyticsStore.createIndex('date', 'date');
        }

        if (!db.objectStoreNames.contains('recognition_history')) {
          const historyStore = db.createObjectStore('recognition_history', { keyPath: 'id' });
          historyStore.createIndex('completedTime', 'completedTime');
        }

        if (!db.objectStoreNames.contains('profiles')) {
          db.createObjectStore('profiles', { keyPath: 'id' });
        }

        if (!db.objectStoreNames.contains('offline_actions')) {
          const actionsStore = db.createObjectStore('offline_actions', { keyPath: 'id', autoIncrement: true });
          actionsStore.createIndex('timestamp', 'timestamp');
        }
      };
    });
  }

  async storeData(storeName: string, data: any): Promise<void> {
    if (!this.db) await this.init();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([storeName], 'readwrite');
      const store = transaction.objectStore(storeName);
      const request = store.put(data);

      request.onerror = () => reject(request.error);
      request.onsuccess = () => resolve();
    });
  }

  async getData(storeName: string, key?: any): Promise<any> {
    if (!this.db) await this.init();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([storeName], 'readonly');
      const store = transaction.objectStore(storeName);
      const request = key ? store.get(key) : store.getAll();

      request.onerror = () => reject(request.error);
      request.onsuccess = () => resolve(request.result);
    });
  }

  async queueOfflineAction(action: OfflineAction): Promise<void> {
    action.timestamp = Date.now();
    await this.storeData('offline_actions', action);

    // Request background sync if available
    if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
      const registration = await navigator.serviceWorker.ready;
      await (registration as any).sync.register('sync-offline-actions');
    }
  }

  async getOfflineActions(): Promise<OfflineAction[]> {
    return await this.getData('offline_actions');
  }

  async removeOfflineAction(id: number): Promise<void> {
    if (!this.db) await this.init();

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction(['offline_actions'], 'readwrite');
      const store = transaction.objectStore('offline_actions');
      const request = store.delete(id);

      request.onerror = () => reject(request.error);
      request.onsuccess = () => resolve();
    });
  }
}

// Device Features Integration
export class DeviceFeatures {
  // Push Notifications
  static async requestNotificationPermission(): Promise<boolean> {
    if (!('Notification' in window)) {
      console.warn('This browser does not support notifications');
      return false;
    }

    if (Notification.permission === 'granted') {
      return true;
    }

    if (Notification.permission !== 'denied') {
      const permission = await Notification.requestPermission();
      return permission === 'granted';
    }

    return false;
  }

  static async showNotification(title: string, options: NotificationOptions = {}): Promise<void> {
    const hasPermission = await this.requestNotificationPermission();
    if (!hasPermission) return;

    const defaultOptions: NotificationOptions = {
      icon: '/icons/icon-192x192.png',
      badge: '/icons/icon-72x72.png',
      tag: 'voice-assistant',
      // renotify: true, // Deprecated
      ...options
    };

    // Use service worker notification if available
    if ('serviceWorker' in navigator) {
      const registration = await navigator.serviceWorker.ready;
      await registration.showNotification(title, defaultOptions);
    } else {
      new Notification(title, defaultOptions);
    }
  }

  // Device Vibration
  static vibrate(pattern: number | number[] = 200): boolean {
    if ('vibrate' in navigator) {
      return navigator.vibrate(pattern);
    }
    return false;
  }

  // Screen Wake Lock
  static async requestWakeLock(): Promise<WakeLockSentinel | null> {
    try {
      if ('wakeLock' in navigator) {
        return await (navigator as any).wakeLock.request('screen');
      }
    } catch (error) {
      console.warn('Wake lock failed:', error);
    }
    return null;
  }

  // Device Orientation
  static async requestOrientationPermission(): Promise<boolean> {
    try {
      if ('DeviceOrientationEvent' in window && 'requestPermission' in DeviceOrientationEvent) {
        const permission = await (DeviceOrientationEvent as any).requestPermission();
        return permission === 'granted';
      }
      return true; // Android doesn't require permission
    } catch (error) {
      console.warn('Orientation permission failed:', error);
      return false;
    }
  }

  // Battery Status
  static async getBatteryStatus(): Promise<BatteryManager | null> {
    try {
      if ('getBattery' in navigator) {
        return await (navigator as any).getBattery();
      }
    } catch (error) {
      console.warn('Battery API not available:', error);
    }
    return null;
  }

  // Network Information
  static getNetworkInfo(): NetworkInformation | null {
    try {
      return (navigator as any).connection || null;
    } catch (error) {
      console.warn('Network Information API not available:', error);
      return null;
    }
  }

  // Geolocation
  static async getCurrentLocation(): Promise<GeolocationPosition | null> {
    return new Promise((resolve) => {
      if ('geolocation' in navigator) {
        navigator.geolocation.getCurrentPosition(
          (position) => resolve(position),
          (error) => {
            console.warn('Geolocation failed:', error);
            resolve(null);
          }
        );
      } else {
        resolve(null);
      }
    });
  }

  // Media Devices (Camera/Microphone)
  static async getMediaDevices(): Promise<MediaDeviceInfo[]> {
    try {
      if ('mediaDevices' in navigator && 'enumerateDevices' in navigator.mediaDevices) {
        return await navigator.mediaDevices.enumerateDevices();
      }
    } catch (error) {
      console.warn('Media devices enumeration failed:', error);
    }
    return [];
  }

  static async requestCameraAccess(): Promise<MediaStream | null> {
    try {
      if ('mediaDevices' in navigator && 'getUserMedia' in navigator.mediaDevices) {
        return await navigator.mediaDevices.getUserMedia({ video: true });
      }
    } catch (error) {
      console.warn('Camera access failed:', error);
    }
    return null;
  }

  static async requestMicrophoneAccess(): Promise<MediaStream | null> {
    try {
      if ('mediaDevices' in navigator && 'getUserMedia' in navigator.mediaDevices) {
        return await navigator.mediaDevices.getUserMedia({ audio: true });
      }
    } catch (error) {
      console.warn('Microphone access failed:', error);
    }
    return null;
  }

  // Share API
  static async shareContent(data: ShareData): Promise<boolean> {
    try {
      if ('share' in navigator) {
        await (navigator as any).share(data);
        return true;
      }
    } catch (error) {
      console.warn('Web Share API failed:', error);
    }
    return false;
  }

  // Clipboard API
  static async writeToClipboard(text: string): Promise<boolean> {
    try {
      if ('clipboard' in navigator) {
        await navigator.clipboard.writeText(text);
        return true;
      }
    } catch (error) {
      console.warn('Clipboard write failed:', error);
    }
    return false;
  }

  static async readFromClipboard(): Promise<string | null> {
    try {
      if ('clipboard' in navigator) {
        return await navigator.clipboard.readText();
      }
    } catch (error) {
      console.warn('Clipboard read failed:', error);
    }
    return null;
  }
}

// Performance Monitoring
export class PerformanceMonitor {
  private metrics: PerformanceMetric[] = [];

  startMeasure(name: string): void {
    performance.mark(`${name}-start`);
  }

  endMeasure(name: string): number {
    performance.mark(`${name}-end`);
    performance.measure(name, `${name}-start`, `${name}-end`);
    
    const measure = performance.getEntriesByName(name)[0] as PerformanceMeasure;
    const duration = measure.duration;

    this.metrics.push({
      name,
      duration,
      timestamp: Date.now()
    });

    return duration;
  }

  getMetrics(): PerformanceMetric[] {
    return this.metrics;
  }

  clearMetrics(): void {
    this.metrics = [];
    performance.clearMarks();
    performance.clearMeasures();
  }

  async reportVitals(): Promise<void> {
    // Report Core Web Vitals if available
    if ('PerformanceObserver' in window) {
      try {
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            console.log(`Web Vital: ${entry.name}:`, entry);
          }
        });

        observer.observe({ entryTypes: ['navigation', 'paint', 'largest-contentful-paint'] });
      } catch (error) {
        console.warn('Performance observation failed:', error);
      }
    }
  }
}

// Connection Monitor
export class ConnectionMonitor {
  private listeners: Array<(online: boolean) => void> = [];

  constructor() {
    window.addEventListener('online', () => this.notifyListeners(true));
    window.addEventListener('offline', () => this.notifyListeners(false));
  }

  isOnline(): boolean {
    return navigator.onLine;
  }

  onStatusChange(callback: (online: boolean) => void): () => void {
    this.listeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      const index = this.listeners.indexOf(callback);
      if (index > -1) {
        this.listeners.splice(index, 1);
      }
    };
  }

  private notifyListeners(online: boolean): void {
    this.listeners.forEach(listener => listener(online));
  }

  getNetworkInfo(): NetworkInformation | null {
    return DeviceFeatures.getNetworkInfo();
  }

  getConnectionSpeed(): string {
    const connection = DeviceFeatures.getNetworkInfo();
    if (connection && 'effectiveType' in connection) {
      return connection.effectiveType;
    }
    return 'unknown';
  }
}

// Types
export interface OfflineAction {
  id?: number;
  url: string;
  method: string;
  headers: Record<string, string>;
  body?: string;
  timestamp?: number;
}

export interface PerformanceMetric {
  name: string;
  duration: number;
  timestamp: number;
}

export interface ShareData {
  title?: string;
  text?: string;
  url?: string;
}

export interface NetworkInformation {
  effectiveType: string;
  downlink: number;
  rtt: number;
  saveData: boolean;
}

export interface BatteryManager extends EventTarget {
  charging: boolean;
  chargingTime: number;
  dischargingTime: number;
  level: number;
}

// Global instances
export const pwaManager = new PWAManager();
export const offlineManager = new OfflineManager();
export const performanceMonitor = new PerformanceMonitor();
export const connectionMonitor = new ConnectionMonitor();

// Initialize offline manager
offlineManager.init().catch(console.error);