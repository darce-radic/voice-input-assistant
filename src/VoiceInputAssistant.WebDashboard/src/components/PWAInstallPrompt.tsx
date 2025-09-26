import React, { useState, useEffect } from 'react';
import { mobileFeaturesService } from '../services/mobileFeatures';

interface PWAInstallPromptProps {
  onClose?: () => void;
  onInstall?: () => void;
}

const PWAInstallPrompt: React.FC<PWAInstallPromptProps> = ({ onClose, onInstall }) => {
  const [isVisible, setIsVisible] = useState(false);
  const [platform, setPlatform] = useState<'ios' | 'android' | 'desktop' | 'unknown'>('unknown');
  const [isInstallable, setIsInstallable] = useState(false);
  const [hasInstallPrompt, setHasInstallPrompt] = useState(false);
  const [isInstalling, setIsInstalling] = useState(false);

  useEffect(() => {
    detectPlatform();
    checkInstallability();
    
    // Listen for mobile features events
    mobileFeaturesService.on('install-prompt-available', () => {
      setHasInstallPrompt(true);
      setIsInstallable(true);
    });

    mobileFeaturesService.on('app-installed', () => {
      setIsVisible(false);
      onInstall?.();
    });

    mobileFeaturesService.on('ios-add-to-homescreen-prompt', () => {
      if (platform === 'ios') {
        setIsVisible(true);
      }
    });

    // Check if app is already installed
    if ((window.navigator as any).standalone || window.matchMedia('(display-mode: standalone)').matches) {
      setIsVisible(false);
    } else {
      // Show prompt after a delay if installable
      setTimeout(() => {
        if (isInstallable || platform === 'ios') {
          setIsVisible(true);
        }
      }, 5000);
    }

    return () => {
      mobileFeaturesService.removeAllListeners();
    };
  }, [platform, isInstallable, onInstall]);

  const detectPlatform = () => {
    const userAgent = navigator.userAgent;
    
    if (/iPad|iPhone|iPod/.test(userAgent)) {
      setPlatform('ios');
    } else if (/Android/.test(userAgent)) {
      setPlatform('android');
    } else {
      setPlatform('desktop');
    }
  };

  const checkInstallability = () => {
    // Check if beforeinstallprompt was already fired
    const capabilities = mobileFeaturesService.getCapabilities();
    if (capabilities?.supportsInstallPrompt) {
      setIsInstallable(true);
      setHasInstallPrompt(true);
    }
  };

  const handleInstall = async () => {
    setIsInstalling(true);
    
    try {
      if (platform === 'ios') {
        // iOS installation requires manual steps
        setIsVisible(true);
      } else if (hasInstallPrompt) {
        // Use native install prompt
        const result = await mobileFeaturesService.showInstallPrompt();
        if (result.outcome === 'accepted') {
          setIsVisible(false);
          onInstall?.();
        }
      } else {
        // Fallback for other platforms
        mobileFeaturesService.showAddToHomeScreen();
      }
    } catch (error) {
      console.error('Installation failed:', error);
    } finally {
      setIsInstalling(false);
    }
  };

  const handleClose = () => {
    setIsVisible(false);
    onClose?.();
    
    // Don't show again for 24 hours
    localStorage.setItem('pwa-install-dismissed', Date.now().toString());
  };

  const shouldShowPrompt = () => {
    // Check if user dismissed recently
    const dismissed = localStorage.getItem('pwa-install-dismissed');
    if (dismissed) {
      const dismissedTime = parseInt(dismissed);
      const hoursPassed = (Date.now() - dismissedTime) / (1000 * 60 * 60);
      if (hoursPassed < 24) {
        return false;
      }
    }
    
    return isVisible && (isInstallable || platform === 'ios');
  };

  if (!shouldShowPrompt()) {
    return null;
  }

  const getInstallInstructions = () => {
    switch (platform) {
      case 'ios':
        return {
          title: 'Install Voice Assistant',
          steps: [
            'Tap the Share button',
            'Scroll down and tap "Add to Home Screen"',
            'Tap "Add" to install the app'
          ],
          icon: '📱'
        };
      case 'android':
        return {
          title: 'Install Voice Assistant',
          steps: [
            'Tap "Add to Home Screen"',
            'Confirm installation'
          ],
          icon: '🤖'
        };
      case 'desktop':
        return {
          title: 'Install Voice Assistant',
          steps: [
            'Click "Install" to add to your applications',
            'Access from your desktop or app menu'
          ],
          icon: '💻'
        };
      default:
        return {
          title: 'Install Voice Assistant',
          steps: ['Follow your browser\'s installation prompt'],
          icon: '🌐'
        };
    }
  };

  const instructions = getInstallInstructions();

  return (
    <div className="pwa-install-prompt">
      <div className="prompt-overlay" onClick={handleClose} />
      <div className="prompt-container">
        <div className="prompt-header">
          <div className="app-icon">
            <img src="/icons/icon-96x96.png" alt="Voice Assistant" />
          </div>
          <div className="app-info">
            <h3>{instructions.title}</h3>
            <p>Get quick access with offline capabilities</p>
          </div>
          <button className="close-button" onClick={handleClose}>
            ✕
          </button>
        </div>

        <div className="prompt-content">
          <div className="benefits">
            <h4>Why install?</h4>
            <ul>
              <li>🚀 Faster loading times</li>
              <li>📱 Works offline</li>
              <li>🔔 Push notifications</li>
              <li>🏠 Easy home screen access</li>
              <li>💾 Reduced data usage</li>
            </ul>
          </div>

          {platform === 'ios' && (
            <div className="ios-instructions">
              <h4>Installation Steps:</h4>
              <div className="step-list">
                {instructions.steps.map((step, index) => (
                  <div key={index} className="step-item">
                    <span className="step-number">{index + 1}</span>
                    <span className="step-text">{step}</span>
                  </div>
                ))}
              </div>
              
              <div className="ios-visual-guide">
                <div className="share-icon">
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M18 16.08c-.76 0-1.44.3-1.96.77L8.91 12.7c.05-.23.09-.46.09-.7s-.04-.47-.09-.7l7.05-4.11c.54.5 1.25.81 2.04.81 1.66 0 3-1.34 3-3s-1.34-3-3-3-3 1.34-3 3c0 .24.04.47.09.7L8.04 9.81C7.5 9.31 6.79 9 6 9c-1.66 0-3 1.34-3 3s1.34 3 3 3c.79 0 1.50-.31 2.04-.81l7.05 4.11c-.05.23-.09.46-.09.7 0 1.66 1.34 3 3 3s3-1.34 3-3-1.34-3-3-3z"/>
                  </svg>
                </div>
                <span>Look for this Share icon in Safari</span>
              </div>
            </div>
          )}

          {platform !== 'ios' && (
            <div className="install-actions">
              <button
                className="install-button"
                onClick={handleInstall}
                disabled={isInstalling}
              >
                {isInstalling ? (
                  <>
                    <div className="loading-spinner" />
                    Installing...
                  </>
                ) : (
                  <>
                    {instructions.icon} Install App
                  </>
                )}
              </button>
            </div>
          )}
        </div>

        <div className="prompt-footer">
          <small>
            Install now for the best experience. You can uninstall anytime from your device settings.
          </small>
        </div>
      </div>
    </div>
  );
};

export default PWAInstallPrompt;