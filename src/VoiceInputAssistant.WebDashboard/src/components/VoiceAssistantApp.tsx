import React, { useState, useEffect, useRef } from 'react';
// import { speechService } from '../services/speechService';
// import { mlService, Sentiment } from '../services/mlService';
// import { webrtcService } from '../services/webrtcService';
import { mobileFeaturesService, VibrationPattern } from '../services/mobileFeatures';
import { offlineCacheService } from '../services/offlineCacheService';
import { performanceService } from '../services/performanceService';
import { notificationService } from '../services/notificationService';
import hotkeyService from '../services/hotkeyService';
import { analyticsService } from '../services/analyticsService';
import PWAInstallPrompt from './PWAInstallPrompt';
import HotkeySettings from './HotkeySettings';

// Types
interface AppState {
  isRecording: boolean;
  isTranscribing: boolean;
  transcript: string;
  confidence: number;
  currentEngine: string;
  isOnline: boolean;
  batteryLevel?: number;
  performanceScore: number;
  activeConnections: number;
  mlModelsLoaded: boolean;
}

interface TranscriptionResult {
  text: string;
  confidence: number;
  timestamp: Date;
  engine: string;
  language: string;
}

interface AnalyticsData {
  sessionsToday: number;
  totalTranscriptionTime: number;
  averageAccuracy: number;
  mostUsedEngine: string;
}

const VoiceAssistantApp: React.FC = () => {
  // Core app state
  const [appState, setAppState] = useState<AppState>({
    isRecording: false,
    isTranscribing: false,
    transcript: '',
    confidence: 0,
    currentEngine: 'Google Cloud',
    isOnline: navigator.onLine,
    performanceScore: 100,
    activeConnections: 0,
    mlModelsLoaded: false
  });

  // UI state
  const [showHotkeySettings, setShowHotkeySettings] = useState(false);
  const [showPWAInstall, setShowPWAInstall] = useState(false);
  const [showAnalytics, setShowAnalytics] = useState(false);
  const [showWebRTC, setShowWebRTC] = useState(false);

  // Data state
  const [transcriptionHistory, setTranscriptionHistory] = useState<TranscriptionResult[]>([]);
  const [analyticsData, setAnalyticsData] = useState<AnalyticsData>({
    sessionsToday: 0,
    totalTranscriptionTime: 0,
    averageAccuracy: 0,
    mostUsedEngine: 'Google Cloud'
  });

  // Refs
  const audioVisualizerRef = useRef<HTMLCanvasElement>(null);
  const transcriptionRef = useRef<HTMLDivElement>(null);

  // Initialize services
  useEffect(() => {
    initializeServices();
    setupEventListeners();
    loadUserPreferences();
    
    return () => {
      cleanupServices();
    };
  }, []);

  const initializeServices = async () => {
    try {
      // Initialize performance monitoring first
      performanceService.startWebVitalsMonitoring();
      performanceService.setupLazyLoading();
      
      // Initialize mobile features
      await mobileFeaturesService.initialize();
      
      // Initialize ML services
      // await mlService.initialize();
      // setAppState(prev => ({ ...prev, mlModelsLoaded: true }));
      
      // Initialize speech service with default engine
      // await speechService.initialize();
      
      // Initialize WebRTC service
      // await webrtcService.initialize();
      
      // Setup hotkey service
      [
        { id: 'toggle-recording', keys: 'Ctrl+Shift+S', description: 'Toggle recording' },
        { id: 'clear-transcript', keys: 'Ctrl+Shift+X', description: 'Clear transcript' },
        { id: 'copy-transcript', keys: 'Ctrl+Shift+C', description: 'Copy transcript' }
      ].forEach(hotkeyService.registerHotkey);
      
      // Load analytics data
      // const analytics = await analyticsService.getAnalyticsSummary();
      // setAnalyticsData(analytics);
      
      console.log('[App] All services initialized successfully');
    } catch (error) {
      console.error('[App] Service initialization failed:', error);
      notificationService.showNotification({ title: 'Error', body: 'Service initialization failed', type: 'system', priority: 'normal' });
    }
  };

  const setupEventListeners = () => {
    // Speech service events
    // speechService.on('transcription-result', (result: any) => {
    //   setAppState(prev => ({ ...prev, transcript: result.text, confidence: result.confidence }));
    //   addTranscriptionResult(result);
    // });

    // speechService.on('recording-start', () => {
    //   setAppState(prev => ({ ...prev, isRecording: true }));
    //   mobileFeaturesService.vibrate(VibrationPattern.RECORDING_START);
    // });

    // speechService.on('recording-stop', () => {
    //   setAppState(prev => ({ ...prev, isRecording: false }));
    //   mobileFeaturesService.vibrate(VibrationPattern.RECORDING_STOP);
    // });

    // speechService.on('transcription-start', () => {
    //   setAppState(prev => ({ ...prev, isTranscribing: true }));
    // });

    // speechService.on('transcription-complete', () => {
    //   setAppState(prev => ({ ...prev, isTranscribing: false }));
    // });

    // Network status
    offlineCacheService.on('online', () => {
      setAppState(prev => ({ ...prev, isOnline: true }));
      notificationService.showNotification({ title: 'Back Online', body: 'Back online - syncing data...', type: 'system', priority: 'normal' });
    });

    offlineCacheService.on('offline', () => {
      setAppState(prev => ({ ...prev, isOnline: false }));
      notificationService.showNotification({ title: 'Offline', body: 'Working offline - changes will sync later', type: 'system', priority: 'normal' });
    });

    // Performance monitoring
    performanceService.on('web-vitals-report', (vitals) => {
      const score = performanceService.getPerformanceScore();
      setAppState(prev => ({ ...prev, performanceScore: score }));
    });

    // WebRTC connections
    // webrtcService.on('peer-connected', () => {
    //   setAppState(prev => ({ ...prev, activeConnections: prev.activeConnections + 1 }));
    // });

    // webrtcService.on('peer-disconnected', () => {
    //   setAppState(prev => ({ ...prev, activeConnections: Math.max(0, prev.activeConnections - 1) }));
    // });

    // ML service events
    // mlService.on('voice-activity-detected', () => {
    //   // Auto-start recording on voice activity
    //   if (!appState.isRecording) {
    //     startRecording();
    //   }
    // });

    // mlService.on('sentiment-analyzed', (sentiment: Sentiment) => {
    //   console.log('[App] Sentiment:', sentiment);
    // });

    // Hotkey events
    hotkeyService.on('hotkey-pressed', (hotkeyId: string) => {
      handleHotkeyAction(hotkeyId);
    });

    // Mobile features
    mobileFeaturesService.on('capabilities-detected', (capabilities) => {
      console.log('[App] Device capabilities:', capabilities);
    });

    // Battery status
    mobileFeaturesService.getBatteryInfo().then((battery) => {
      setAppState(prev => ({ ...prev, batteryLevel: battery.level }));
    }).catch(() => {
      // Battery API not supported
    });
  };

  const cleanupServices = () => {
    // speechService.cleanup();
    // webrtcService.cleanup();
    mobileFeaturesService.destroy();
    performanceService.destroy();
    hotkeyService.dispose();
  };

  const loadUserPreferences = () => {
    // Load saved preferences from localStorage
    const savedEngine = localStorage.getItem('preferred-engine');
    if (savedEngine) {
      setAppState(prev => ({ ...prev, currentEngine: savedEngine }));
    }
  };

  const addTranscriptionResult = (result: any) => {
    const transcriptionResult: TranscriptionResult = {
      text: result.text,
      confidence: result.confidence,
      timestamp: new Date(),
      engine: result.engine || appState.currentEngine,
      language: result.language || 'en-US'
    };

    setTranscriptionHistory(prev => [transcriptionResult, ...prev.slice(0, 49)]); // Keep last 50

    // Update analytics
    analyticsService.trackEvent({
      category: 'Transcription',
      action: 'Transcription Recorded',
      label: result.engine || appState.currentEngine,
      value: result.confidence,
      customParameters: {
        duration: result.duration || 0,
        wordCount: result.text.split(' ').length,
      }
    });
  };

  const handleHotkeyAction = (hotkeyId: string) => {
    switch (hotkeyId) {
      case 'toggle-recording':
        toggleRecording();
        break;
      case 'start-recording':
        startRecording();
        break;
      case 'stop-recording':
        stopRecording();
        break;
      case 'clear-transcript':
        clearTranscript();
        break;
      case 'copy-transcript':
        copyTranscript();
        break;
      case 'switch-engine':
        switchEngine();
        break;
      case 'toggle-settings':
        setShowHotkeySettings(true);
        break;
      case 'show-analytics':
        setShowAnalytics(true);
        break;
      case 'toggle-webrtc':
        setShowWebRTC(true);
        break;
      default:
        console.log('[App] Unknown hotkey:', hotkeyId);
    }
  };

  const startRecording = async () => {
    try {
      // await speechService.startRecording();
      // Request wake lock to prevent screen sleep during recording
      mobileFeaturesService.requestWakeLock();
    } catch (error) {
      console.error('[App] Failed to start recording:', error);
      notificationService.showNotification({ title: 'Error', body: 'Failed to start recording', type: 'system', priority: 'high' });
    }
  };

  const stopRecording = async () => {
    try {
      // await speechService.stopRecording();
      mobileFeaturesService.releaseWakeLock();
    } catch (error) {
      console.error('[App] Failed to stop recording:', error);
    }
  };

  const toggleRecording = () => {
    if (appState.isRecording) {
      stopRecording();
    } else {
      startRecording();
    }
  };

  const clearTranscript = () => {
    setAppState(prev => ({ ...prev, transcript: '', confidence: 0 }));
    setTranscriptionHistory([]);
  };

  const copyTranscript = async () => {
    try {
      await navigator.clipboard.writeText(appState.transcript);
      notificationService.showNotification({ title: 'Success', body: 'Transcript copied to clipboard', type: 'system', priority: 'normal' });
      mobileFeaturesService.vibrate(VibrationPattern.SUCCESS);
    } catch (error) {
      console.error('[App] Failed to copy transcript:', error);
      notificationService.showNotification({ title: 'Error', body: 'Failed to copy transcript', type: 'system', priority: 'high' });
    }
  };

  const switchEngine = () => {
    const engines = ['Google Cloud', 'Azure', 'AWS', 'OpenAI Whisper'];
    const currentIndex = engines.indexOf(appState.currentEngine);
    const nextIndex = (currentIndex + 1) % engines.length;
    const nextEngine = engines[nextIndex];
    
    setAppState(prev => ({ ...prev, currentEngine: nextEngine }));
    // speechService.switchEngine(nextEngine);
    localStorage.setItem('preferred-engine', nextEngine);
    
    notificationService.showNotification({ title: 'Engine Switched', body: `Switched to ${nextEngine}`, type: 'system', priority: 'normal' });
  };

  const shareTranscript = async () => {
    if (!appState.transcript) return;
    
    try {
      await mobileFeaturesService.shareContent({
        title: 'Voice Assistant Transcript',
        text: appState.transcript,
        url: window.location.href
      });
    } catch (error) {
      // Fallback to copy
      copyTranscript();
    }
  };

  const exportTranscriptionHistory = () => {
    const dataStr = JSON.stringify(transcriptionHistory, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `voice-transcriptions-${new Date().toISOString().split('T')[0]}.json`;
    link.click();
    
    URL.revokeObjectURL(url);
    notificationService.showNotification({ title: 'Export Complete', body: 'Transcription history exported', type: 'system', priority: 'normal' });
  };

  const getStatusColor = () => {
    if (!appState.isOnline) return '#ef4444'; // Red for offline
    if (appState.isRecording) return '#ef4444'; // Red for recording
    if (appState.isTranscribing) return '#f59e0b'; // Amber for transcribing
    return '#10b981'; // Green for ready
  };

  return (
    <div className="voice-assistant-app">
      {/* Header */}
      <header className="app-header">
        <div className="header-left">
          <div className="app-logo">
            <img src="/icons/icon-96x96.png" alt="Voice Assistant" />
            <h1>Voice Assistant</h1>
          </div>
          <div className="status-indicators">
            <div className={`status-dot ${appState.isOnline ? 'online' : 'offline'}`} />
            <span className="status-text">
              {appState.isOnline ? 'Online' : 'Offline'}
            </span>
            {appState.batteryLevel !== undefined && (
              <div className="battery-indicator">
                🔋 {Math.round(appState.batteryLevel * 100)}%
              </div>
            )}
          </div>
        </div>
        
        <div className="header-right">
          <div className="engine-selector">
            <select 
              value={appState.currentEngine}
              onChange={(e) => setAppState(prev => ({ ...prev, currentEngine: e.target.value }))}
            >
              <option value="Google Cloud">Google Cloud</option>
              <option value="Azure">Azure Cognitive Services</option>
              <option value="AWS">AWS Transcribe</option>
              <option value="OpenAI Whisper">OpenAI Whisper</option>
            </select>
          </div>
          
          <button 
            className="settings-btn"
            onClick={() => setShowHotkeySettings(true)}
            title="Settings (Ctrl+,)"
          >
            ⚙️
          </button>
        </div>
      </header>

      {/* Main Content */}
      <main className="app-main">
        {/* Recording Controls */}
        <div className="recording-section">
          <div className="audio-visualizer">
            <canvas ref={audioVisualizerRef} width="300" height="100" />
          </div>
          
          <div className="recording-controls">
            <button
              className={`record-btn ${appState.isRecording ? 'recording' : ''}`}
              onClick={toggleRecording}
              disabled={appState.isTranscribing}
            >
              {appState.isRecording ? (
                <>⏹️ Stop Recording</>
              ) : (
                <>🎤 Start Recording</>
              )}
            </button>
            
            <div className="control-buttons">
              <button onClick={clearTranscript} title="Clear (Ctrl+X)">
                🗑️ Clear
              </button>
              <button onClick={copyTranscript} title="Copy (Ctrl+C)">
                📋 Copy
              </button>
              <button onClick={shareTranscript} title="Share (Ctrl+S)">
                📤 Share
              </button>
            </div>
          </div>
        </div>

        {/* Transcription Display */}
        <div className="transcription-section">
          <div className="transcription-header">
            <h3>Live Transcription</h3>
            <div className="confidence-meter">
              <span>Confidence: {Math.round(appState.confidence * 100)}%</span>
              <div className="confidence-bar">
                <div 
                  className="confidence-fill"
                  style={{ width: `${appState.confidence * 100}%` }}
                />
              </div>
            </div>
          </div>
          
          <div 
            ref={transcriptionRef}
            className="transcription-display"
            contentEditable
            suppressContentEditableWarning
          >
            {appState.transcript || 'Start recording to see transcription...'}
          </div>
          
          {appState.isTranscribing && (
            <div className="transcribing-indicator">
              <div className="loading-dots">
                <span></span>
                <span></span>
                <span></span>
              </div>
              <span>Transcribing...</span>
            </div>
          )}
        </div>

        {/* Quick Actions */}
        <div className="quick-actions">
          <button 
            onClick={() => setShowAnalytics(true)}
            className="action-card"
          >
            <div className="action-icon">📊</div>
            <div className="action-info">
              <h4>Analytics</h4>
              <p>{analyticsData.sessionsToday} sessions today</p>
            </div>
          </button>
          
          <button 
            onClick={() => setShowWebRTC(true)}
            className="action-card"
          >
            <div className="action-icon">🔗</div>
            <div className="action-info">
              <h4>Collaborate</h4>
              <p>{appState.activeConnections} active connections</p>
            </div>
          </button>
          
          <button 
            onClick={exportTranscriptionHistory}
            className="action-card"
          >
            <div className="action-icon">💾</div>
            <div className="action-info">
              <h4>Export</h4>
              <p>{transcriptionHistory.length} transcriptions</p>
            </div>
          </button>
          
          <button 
            onClick={() => setShowPWAInstall(true)}
            className="action-card"
          >
            <div className="action-icon">📱</div>
            <div className="action-info">
              <h4>Install</h4>
              <p>Add to home screen</p>
            </div>
          </button>
        </div>

        {/* Recent Transcriptions */}
        <div className="history-section">
          <h3>Recent Transcriptions</h3>
          <div className="history-list">
            {transcriptionHistory.slice(0, 5).map((result, index) => (
              <div key={index} className="history-item">
                <div className="history-content">
                  <p>{result.text}</p>
                  <div className="history-meta">
                    <span className="timestamp">
                      {result.timestamp.toLocaleTimeString()}
                    </span>
                    <span className="engine">{result.engine}</span>
                    <span className="confidence">
                      {Math.round(result.confidence * 100)}%
                    </span>
                  </div>
                </div>
              </div>
            ))}
            {transcriptionHistory.length === 0 && (
              <div className="no-history">
                <p>No transcriptions yet. Start recording to see your history here.</p>
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Modals */}
      {showHotkeySettings && (
        <HotkeySettings 
          isOpen={showHotkeySettings}
          onClose={() => setShowHotkeySettings(false)}
        />
      )}
      
      {showPWAInstall && (
        <PWAInstallPrompt
          onClose={() => setShowPWAInstall(false)}
          onInstall={() => setShowPWAInstall(false)}
        />
      )}

    </div>
  );
};

export default VoiceAssistantApp;