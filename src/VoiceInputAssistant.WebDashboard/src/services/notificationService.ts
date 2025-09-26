import { DeviceFeatures } from '../utils/pwaUtils';
import config from './config';
import api from './api';

export interface NotificationData {
  id: string;
  title: string;
  body: string;
  type: 'recognition' | 'analytics' | 'system' | 'reminder';
  priority: 'low' | 'normal' | 'high';
  actions?: NotificationAction[];
  data?: any;
  timestamp: number;
  persistent?: boolean;
}

export interface NotificationAction {
  action: string;
  title: string;
  icon?: string;
}

export interface SubscriptionOptions {
  userVisibleOnly: boolean;
  applicationServerKey: Uint8Array;
}

class NotificationService {
  private subscription: PushSubscription | null = null;
  private isPermissionGranted = false;
  private isServiceWorkerReady = false;
  private pendingNotifications: NotificationData[] = [];
  private notificationHistory: NotificationData[] = [];
  private maxHistorySize = 100;

  // VAPID public key for push notifications (replace with your actual key)
  private readonly vapidPublicKey = config.vapidPublicKey;

  constructor() {
    this.init();
  }

  private async init(): Promise<void> {
    try {
      // Check if service worker is supported
      if (!('serviceWorker' in navigator)) {
        console.warn('Service Worker not supported');
        return;
      }

      // Check notification permission
      this.isPermissionGranted = await DeviceFeatures.requestNotificationPermission();

      // Wait for service worker to be ready
      if ('serviceWorker' in navigator) {
        const registration = await navigator.serviceWorker.ready;
        this.isServiceWorkerReady = true;

        // Check if push is supported
        if (!('PushManager' in window)) {
          console.warn('Push notifications not supported');
          return;
        }

        // Try to get existing subscription
        this.subscription = await registration.pushManager.getSubscription();
        
        if (!this.subscription) {
          // Request permission and subscribe if not already subscribed
          await this.subscribeToPush();
        }

        // Set up message listener for service worker
        navigator.serviceWorker.addEventListener('message', this.handleServiceWorkerMessage.bind(this));
      }

      // Load notification history from localStorage
      this.loadNotificationHistory();

      console.log('Notification service initialized');
    } catch (error) {
      console.error('Failed to initialize notification service:', error);
    }
  }

  private async subscribeToPush(): Promise<void> {
    try {
      if (!this.isPermissionGranted || !this.isServiceWorkerReady) {
        return;
      }

      const registration = await navigator.serviceWorker.ready;
      
      const applicationServerKey = this.urlBase64ToUint8Array(this.vapidPublicKey);
      
      this.subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: applicationServerKey as any // Cast to any to resolve type incompatibility
      });

      // Send subscription to server
      await this.sendSubscriptionToServer(this.subscription);
      
      console.log('Successfully subscribed to push notifications');
    } catch (error) {
      console.error('Failed to subscribe to push notifications:', error);
    }
  }

  private async sendSubscriptionToServer(subscription: PushSubscription): Promise<void> {
    try {
      await api.post('/api/notifications/subscribe', {
        subscription: subscription.toJSON(),
        userAgent: navigator.userAgent
      });
    } catch (error) {
      console.error('Error sending subscription to server:', error);
    }
  }

  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
      outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
  }

  private handleServiceWorkerMessage(event: MessageEvent): void {
    const { type, data } = event.data;

    switch (type) {
      case 'NOTIFICATION_CLICKED':
        this.handleNotificationClick(data);
        break;
      case 'NOTIFICATION_CLOSED':
        this.handleNotificationClose(data);
        break;
    }
  }

  private handleNotificationClick(data: any): void {
    console.log('Notification clicked:', data);
    
    // Handle different notification types
    switch (data.type) {
      case 'recognition':
        // Navigate to recognition results
        window.location.href = `/recognition/results/${data.recognitionId}`;
        break;
      case 'analytics':
        // Navigate to analytics page
        window.location.href = '/analytics';
        break;
      case 'system':
        // Handle system notifications
        this.handleSystemNotificationClick(data);
        break;
    }
  }

  private handleNotificationClose(data: any): void {
    console.log('Notification closed:', data);
    // Update notification status if needed
  }

  private handleSystemNotificationClick(data: any): void {
    switch (data.action) {
      case 'update':
        // Trigger app update
        if ('serviceWorker' in navigator) {
          navigator.serviceWorker.getRegistration().then(registration => {
            if (registration?.waiting) {
              registration.waiting.postMessage({ type: 'SKIP_WAITING' });
            }
          });
        }
        break;
      case 'settings':
        window.location.href = '/settings/notifications';
        break;
    }
  }

  public async showNotification(notification: Omit<NotificationData, 'id' | 'timestamp'>): Promise<void> {
    const notificationData: NotificationData = {
      id: this.generateId(),
      timestamp: Date.now(),
      ...notification
    };

    try {
      if (this.isPermissionGranted && this.isServiceWorkerReady) {
        await this.displayNotification(notificationData);
      } else {
        // Queue notification for later
        this.pendingNotifications.push(notificationData);
        console.log('Notification queued:', notificationData);
      }

      // Add to history
      this.addToHistory(notificationData);
    } catch (error) {
      console.error('Failed to show notification:', error);
    }
  }

  private async displayNotification(notification: NotificationData): Promise<void> {
    const registration = await navigator.serviceWorker.ready;
    
    const options: NotificationOptions = {
      body: notification.body,
      icon: '/icons/icon-192x192.png',
      badge: '/icons/icon-72x72.png',
      tag: notification.type,
      // renotify: notification.priority === 'high', // Deprecated
      requireInteraction: notification.persistent || notification.priority === 'high',
      // actions: notification.actions, // This is not a standard property
      data: {
        ...notification.data,
        id: notification.id,
        type: notification.type,
        timestamp: notification.timestamp
      },
      // timestamp: notification.timestamp // This is not a standard property
    };

    // Customize based on notification type
    switch (notification.type) {
      case 'recognition':
        options.icon = '/icons/mic-icon.png';
        // options.vibrate = [200, 100, 200]; // Deprecated
        break;
      case 'analytics':
        options.icon = '/icons/chart-icon.png';
        // options.vibrate = [100]; // Deprecated
        break;
      case 'system':
        options.icon = '/icons/system-icon.png';
        // options.vibrate = [300]; // Deprecated
        break;
      case 'reminder':
        options.icon = '/icons/reminder-icon.png';
        // options.vibrate = [100, 50, 100, 50, 100]; // Deprecated
        break;
    }

    await registration.showNotification(notification.title, options);
  }

  public async showRecognitionComplete(results: any): Promise<void> {
    await this.showNotification({
      title: 'Voice Recognition Complete',
      body: `Recognized: "${results.text?.substring(0, 50)}${results.text?.length > 50 ? '...' : ''}"`,
      type: 'recognition',
      priority: 'normal',
      actions: [
        { action: 'view', title: 'View Results' },
        { action: 'dismiss', title: 'Dismiss' }
      ],
      data: { recognitionId: results.id, text: results.text }
    });
  }

  public async showAnalyticsUpdate(summary: any): Promise<void> {
    await this.showNotification({
      title: 'Analytics Updated',
      body: `New data available: ${summary.newRecognitions} recognitions processed`,
      type: 'analytics',
      priority: 'low',
      actions: [
        { action: 'view', title: 'View Analytics' },
        { action: 'dismiss', title: 'Dismiss' }
      ],
      data: { summary }
    });
  }

  public async showSystemUpdate(): Promise<void> {
    await this.showNotification({
      title: 'App Update Available',
      body: 'A new version of Voice Assistant is ready to install',
      type: 'system',
      priority: 'high',
      persistent: true,
      actions: [
        { action: 'update', title: 'Update Now' },
        { action: 'later', title: 'Later' }
      ],
      data: { updateType: 'app' }
    });
  }

  public async showOfflineReminder(): Promise<void> {
    await this.showNotification({
      title: 'Working Offline',
      body: 'Some features are limited. Connect to internet to sync your data.',
      type: 'system',
      priority: 'normal',
      actions: [
        { action: 'sync', title: 'Try to Sync' },
        { action: 'dismiss', title: 'OK' }
      ],
      data: { offline: true }
    });
  }

  public async scheduleReminder(title: string, body: string, when: Date): Promise<string> {
    const notificationId = this.generateId();
    
    // For demo purposes, we'll use setTimeout for scheduling
    // In production, you'd want to use a more robust scheduling system
    const delay = when.getTime() - Date.now();
    
    if (delay > 0) {
      setTimeout(async () => {
        await this.showNotification({
          title,
          body,
          type: 'reminder',
          priority: 'normal',
          actions: [
            { action: 'acknowledge', title: 'Got it' },
            { action: 'snooze', title: 'Remind later' }
          ],
          data: { scheduled: true, originalTime: when.toISOString() }
        });
      }, delay);
    }

    return notificationId;
  }

  public async processPendingNotifications(): Promise<void> {
    if (!this.isPermissionGranted || !this.isServiceWorkerReady) {
      return;
    }

    const pending = [...this.pendingNotifications];
    this.pendingNotifications = [];

    for (const notification of pending) {
      try {
        await this.displayNotification(notification);
      } catch (error) {
        console.error('Failed to process pending notification:', error);
        // Re-queue failed notifications
        this.pendingNotifications.push(notification);
      }
    }
  }

  public getNotificationHistory(): NotificationData[] {
    return [...this.notificationHistory];
  }

  public clearNotificationHistory(): void {
    this.notificationHistory = [];
    this.saveNotificationHistory();
  }

  private addToHistory(notification: NotificationData): void {
    this.notificationHistory.unshift(notification);
    
    // Limit history size
    if (this.notificationHistory.length > this.maxHistorySize) {
      this.notificationHistory = this.notificationHistory.slice(0, this.maxHistorySize);
    }
    
    this.saveNotificationHistory();
  }

  private saveNotificationHistory(): void {
    try {
      localStorage.setItem('voiceAssistantNotifications', JSON.stringify(this.notificationHistory));
    } catch (error) {
      console.error('Failed to save notification history:', error);
    }
  }

  private loadNotificationHistory(): void {
    try {
      const stored = localStorage.getItem('voiceAssistantNotifications');
      if (stored) {
        this.notificationHistory = JSON.parse(stored);
      }
    } catch (error) {
      console.error('Failed to load notification history:', error);
      this.notificationHistory = [];
    }
  }

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  public async requestPermission(): Promise<boolean> {
    this.isPermissionGranted = await DeviceFeatures.requestNotificationPermission();
    
    if (this.isPermissionGranted) {
      await this.subscribeToPush();
      await this.processPendingNotifications();
    }
    
    return this.isPermissionGranted;
  }

  public isPermissionEnabled(): boolean {
    return this.isPermissionGranted;
  }

  public isPushSupported(): boolean {
    return 'serviceWorker' in navigator && 'PushManager' in window;
  }

  public getSubscription(): PushSubscription | null {
    return this.subscription;
  }

  public async unsubscribe(): Promise<void> {
    if (this.subscription) {
      await this.subscription.unsubscribe();
      const endpoint = this.subscription.endpoint;
      this.subscription = null;
      
      // Notify server about unsubscription
      try {
        await api.post('/api/notifications/unsubscribe', { endpoint });
      } catch (error) {
        console.error('Failed to notify server about unsubscription:', error);
      }
    }
  }

  // Batch notification methods
  public async showBatchNotifications(notifications: Omit<NotificationData, 'id' | 'timestamp'>[]): Promise<void> {
    if (notifications.length > 5) {
      // If too many notifications, show a summary instead
      await this.showNotification({
        title: 'Multiple Updates',
        body: `${notifications.length} new updates available`,
        type: 'system',
        priority: 'normal',
        actions: [
          { action: 'viewAll', title: 'View All' },
          { action: 'dismiss', title: 'Dismiss' }
        ]
      });
    } else {
      // Show individual notifications with a slight delay
      for (let i = 0; i < notifications.length; i++) {
        setTimeout(() => {
          this.showNotification(notifications[i]);
        }, i * 500); // 500ms delay between notifications
      }
    }
  }

  // Test notification
  public async showTestNotification(): Promise<void> {
    await this.showNotification({
      title: 'Test Notification',
      body: 'This is a test notification from Voice Assistant',
      type: 'system',
      priority: 'normal',
      actions: [
        { action: 'test', title: 'Test Action' },
        { action: 'dismiss', title: 'Dismiss' }
      ]
    });
  }
}

// Create singleton instance
export const notificationService = new NotificationService();
export default notificationService;