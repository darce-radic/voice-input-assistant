import React, { useState, useEffect } from 'react';
import {
  pwaManager,
  offlineManager,
  connectionMonitor,
  performanceMonitor,
  DeviceFeatures
} from '../utils/pwaUtils';

interface PWAFeaturesProps {
  onInstallPrompt?: (canInstall: boolean) => void;
  onUpdateAvailable?: (hasUpdate: boolean) => void;
  onOfflineStatus?: (isOffline: boolean) => void;
}

const PWAFeatures: React.FC<PWAFeaturesProps> = ({
  onInstallPrompt,
  onUpdateAvailable,
  onOfflineStatus
}) => {
  const [canInstall, setCanInstall] = useState(false);
  const [isInstalled, setIsInstalled] = useState(false);
  const [hasUpdate, setHasUpdate] = useState(false);
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [showInstallBanner, setShowInstallBanner] = useState(false);
  const [showUpdateBanner, setShowUpdateBanner] = useState(false);
  const [cacheSize, setCacheSize] = useState<any[]>([]);
  const [deviceInfo, setDeviceInfo] = useState<any>({});

  useEffect(() => {
    // Initialize PWA features
    initializePWAFeatures();

    // Set up event listeners for PWA events
    const handleInstallAvailable = () => {
      setCanInstall(true);
      setShowInstallBanner(true);
      onInstallPrompt?.(true);
    };

    const handleAppInstalled = () => {
      setIsInstalled(true);
      setCanInstall(false);
      setShowInstallBanner(false);
    };

    const handleUpdateAvailable = () => {
      setHasUpdate(true);
      setShowUpdateBanner(true);
      onUpdateAvailable?.(true);
    };

    window.addEventListener('pwa-install-available', handleInstallAvailable);
    window.addEventListener('pwa-installed', handleAppInstalled);
    window.addEventListener('pwa-update-available', handleUpdateAvailable);

    // Set up connection monitoring
    const unsubscribe = connectionMonitor.onStatusChange((online) => {
      setIsOnline(online);
      onOfflineStatus?.(!online);
    });

    return () => {
      window.removeEventListener('pwa-install-available', handleInstallAvailable);
      window.removeEventListener('pwa-installed', handleAppInstalled);
      window.removeEventListener('pwa-update-available', handleUpdateAvailable);
      unsubscribe();
    };
  }, [onInstallPrompt, onUpdateAvailable, onOfflineStatus]);

  const initializePWAFeatures = async () => {
    // Check current PWA state
    setIsInstalled(pwaManager.isAppInstalled());
    setCanInstall(pwaManager.canInstall());
    setHasUpdate(pwaManager.hasUpdateAvailable());

    // Get device info
    const networkInfo = DeviceFeatures.getNetworkInfo();
    const battery = await DeviceFeatures.getBatteryStatus();
    const location = await DeviceFeatures.getCurrentLocation();
    
    setDeviceInfo({
      network: networkInfo,
      battery,
      location: location ? {
        latitude: location.coords.latitude,
        longitude: location.coords.longitude,
        accuracy: location.coords.accuracy
      } : null
    });

    // Get cache information
    if ('serviceWorker' in navigator) {
      try {
        const registration = await navigator.serviceWorker.ready;
        const messageChannel = new MessageChannel();
        
        messageChannel.port1.onmessage = (event) => {
          if (event.data.type === 'CACHE_SIZE_RESPONSE') {
            setCacheSize(event.data.sizes);
          }
        };

        registration.active?.postMessage(
          { type: 'GET_CACHE_SIZE' },
          [messageChannel.port2]
        );
      } catch (error) {
        console.error('Failed to get cache size:', error);
      }
    }
  };

  const handleInstallApp = async () => {
    const installed = await pwaManager.installApp();
    if (installed) {
      setShowInstallBanner(false);
      setCanInstall(false);
      // Show success notification
      await DeviceFeatures.showNotification('App Installed', {
        body: 'Voice Assistant has been installed on your device!',
        // actions: [ // This is not a standard property
        //   { action: 'open', title: 'Open App' }
        // ]
      });
    }
  };

  const handleUpdateApp = async () => {
    await pwaManager.updateApp();
    setShowUpdateBanner(false);
    setHasUpdate(false);
  };

  const handleShareApp = async () => {
    const shared = await DeviceFeatures.shareContent({
      title: 'Voice Input Assistant',
      text: 'Check out this amazing voice recognition app!',
      url: window.location.origin
    });

    if (!shared) {
      // Fallback to clipboard
      const success = await DeviceFeatures.writeToClipboard(
        `Check out Voice Input Assistant: ${window.location.origin}`
      );
      
      if (success) {
        // Show notification
        await DeviceFeatures.showNotification('Link Copied', {
          body: 'App link copied to clipboard!'
        });
      }
    }
  };

  const handleNotificationTest = async () => {
    await DeviceFeatures.showNotification('Test Notification', {
      body: 'This is a test notification from Voice Assistant',
      // actions: [ // This is not a standard property
      //   { action: 'dismiss', title: 'Dismiss' },
      //   { action: 'settings', title: 'Settings' }
      // ]
    });
  };

  const handleVibrationTest = () => {
    // Vibration pattern: short, pause, long, pause, short
    const pattern = [100, 50, 200, 50, 100];
    const success = DeviceFeatures.vibrate(pattern);
    
    if (!success) {
      console.log('Vibration not supported on this device');
    }
  };

  const clearCaches = async () => {
    if ('serviceWorker' in navigator) {
      const registration = await navigator.serviceWorker.ready;
      registration.active?.postMessage({
        type: 'CLEAR_CACHE',
        payload: { cacheNames: cacheSize.map(c => c.cacheName) }
      });
      
      // Refresh cache size
      setTimeout(initializePWAFeatures, 1000);
    }
  };

  const getTotalCacheSize = () => {
    return cacheSize.reduce((total, cache) => total + cache.size, 0);
  };

  if (!('serviceWorker' in navigator)) {
    return null; // PWA not supported
  }

  return (
    <div className="pwa-features">
      {/* Install Banner */}
      {showInstallBanner && canInstall && (
        <div className="pwa-banner install-banner">
          <div className="banner-content">
            <div className="banner-icon">📱</div>
            <div className="banner-text">
              <h4>Install Voice Assistant</h4>
              <p>Get the full app experience with offline support</p>
            </div>
          </div>
          <div className="banner-actions">
            <button onClick={handleInstallApp} className="btn-primary">
              Install
            </button>
            <button 
              onClick={() => setShowInstallBanner(false)}
              className="btn-secondary"
            >
              Later
            </button>
          </div>
        </div>
      )}

      {/* Update Banner */}
      {showUpdateBanner && hasUpdate && (
        <div className="pwa-banner update-banner">
          <div className="banner-content">
            <div className="banner-icon">🔄</div>
            <div className="banner-text">
              <h4>Update Available</h4>
              <p>A new version of the app is ready to install</p>
            </div>
          </div>
          <div className="banner-actions">
            <button onClick={handleUpdateApp} className="btn-primary">
              Update
            </button>
            <button 
              onClick={() => setShowUpdateBanner(false)}
              className="btn-secondary"
            >
              Later
            </button>
          </div>
        </div>
      )}

      {/* Connection Status */}
      <div className={`connection-status ${isOnline ? 'online' : 'offline'}`}>
        <div className="status-indicator">
          <div className={`status-dot ${isOnline ? 'online' : 'offline'}`}></div>
          <span>{isOnline ? 'Online' : 'Offline'}</span>
        </div>
        {!isOnline && (
          <span className="offline-message">
            Limited functionality - some features may not be available
          </span>
        )}
      </div>

      {/* PWA Controls (for development/testing) */}
      {process.env.NODE_ENV === 'development' && (
        <div className="pwa-controls">
          <h4>PWA Development Tools</h4>
          
          <div className="control-group">
            <h5>Installation</h5>
            <div className="control-buttons">
              {!isInstalled && canInstall && (
                <button onClick={handleInstallApp}>Install App</button>
              )}
              {hasUpdate && (
                <button onClick={handleUpdateApp}>Update App</button>
              )}
              <button onClick={handleShareApp}>Share App</button>
            </div>
          </div>

          <div className="control-group">
            <h5>Device Features</h5>
            <div className="control-buttons">
              <button onClick={handleNotificationTest}>Test Notification</button>
              <button onClick={handleVibrationTest}>Test Vibration</button>
            </div>
          </div>

          <div className="control-group">
            <h5>Cache Information</h5>
            <div className="cache-info">
              <p>Total cached items: {getTotalCacheSize()}</p>
              {cacheSize.map((cache, index) => (
                <div key={index} className="cache-item">
                  <span>{cache.cacheName}:</span>
                  <span>{cache.size} items</span>
                </div>
              ))}
              <button onClick={clearCaches}>Clear All Caches</button>
            </div>
          </div>

          <div className="control-group">
            <h5>Device Information</h5>
            <div className="device-info">
              {deviceInfo.network && (
                <div>
                  <strong>Network:</strong> {deviceInfo.network.effectiveType}
                  <br />
                  <strong>Downlink:</strong> {deviceInfo.network.downlink} Mbps
                </div>
              )}
              {deviceInfo.battery && (
                <div>
                  <strong>Battery:</strong> {Math.round(deviceInfo.battery.level * 100)}%
                  {deviceInfo.battery.charging ? ' (Charging)' : ''}
                </div>
              )}
              {deviceInfo.location && (
                <div>
                  <strong>Location:</strong> Available (accuracy: {Math.round(deviceInfo.location.accuracy)}m)
                </div>
              )}
            </div>
          </div>
        </div>
      )}

    </div>
  );
};

export default PWAFeatures;