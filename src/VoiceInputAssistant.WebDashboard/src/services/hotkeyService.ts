/**
 * Configurable Keyboard Hotkey Service for Voice Control
 * Supports global hotkeys for voice recording, transcription, and app controls
 */

export interface HotkeyConfig {
  id: string;
  name: string;
  description: string;
  defaultKeys: string[];
  currentKeys: string[];
  action: HotkeyAction;
  enabled: boolean;
  global: boolean; // Works even when app is not focused
  category: 'voice' | 'navigation' | 'transcription' | 'general';
}

export interface HotkeyAction {
  type: 'start-recording' | 'stop-recording' | 'toggle-recording' | 'pause-recording' | 
        'start-transcription' | 'stop-transcription' | 'save-transcription' | 
        'clear-transcription' | 'copy-transcription' | 'export-transcription' |
        'switch-engine' | 'toggle-noise-reduction' | 'open-settings' | 'toggle-theme' |
        'focus-search' | 'new-session' | 'delete-session' | 'custom';
  handler: () => void | Promise<void>;
  parameters?: Record<string, any>;
}

export interface KeyboardEvent {
  key: string;
  code: string;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
  preventDefault: () => void;
  stopPropagation: () => void;
}

class HotkeyService {
  private hotkeys: Map<string, HotkeyConfig> = new Map();
  private keyStates: Map<string, boolean> = new Map();
  private isListening = false;
  private eventListeners: Map<string, Function[]> = new Map();
  
  // Key combination registry
  private activeKeyCombos: Map<string, HotkeyConfig> = new Map();
  
  constructor() {
    this.initializeDefaultHotkeys();
    this.setupEventListeners();
  }

  /**
   * Initialize default hotkey configurations
   */
  private initializeDefaultHotkeys(): void {
    const defaultHotkeys: Omit<HotkeyConfig, 'currentKeys'>[] = [
      // Voice Recording Controls
      {
        id: 'start-recording',
        name: 'Start Recording',
        description: 'Begin voice recording',
        defaultKeys: ['F2'],
        action: { type: 'start-recording', handler: () => this.emit('start-recording') },
        enabled: true,
        global: true,
        category: 'voice'
      },
      {
        id: 'stop-recording',
        name: 'Stop Recording',
        description: 'Stop voice recording',
        defaultKeys: ['F2'],
        action: { type: 'stop-recording', handler: () => this.emit('stop-recording') },
        enabled: true,
        global: true,
        category: 'voice'
      },
      {
        id: 'toggle-recording',
        name: 'Toggle Recording',
        description: 'Start/stop voice recording',
        defaultKeys: ['Ctrl', 'Shift', 'R'],
        action: { type: 'toggle-recording', handler: () => this.emit('toggle-recording') },
        enabled: true,
        global: true,
        category: 'voice'
      },
      {
        id: 'pause-recording',
        name: 'Pause Recording',
        description: 'Pause/resume current recording',
        defaultKeys: ['Ctrl', 'Shift', 'P'],
        action: { type: 'pause-recording', handler: () => this.emit('pause-recording') },
        enabled: true,
        global: true,
        category: 'voice'
      },

      // Transcription Controls
      {
        id: 'start-transcription',
        name: 'Start Transcription',
        description: 'Begin real-time transcription',
        defaultKeys: ['Ctrl', 'Shift', 'T'],
        action: { type: 'start-transcription', handler: () => this.emit('start-transcription') },
        enabled: true,
        global: false,
        category: 'transcription'
      },
      {
        id: 'stop-transcription',
        name: 'Stop Transcription',
        description: 'Stop real-time transcription',
        defaultKeys: ['Ctrl', 'Shift', 'S'],
        action: { type: 'stop-transcription', handler: () => this.emit('stop-transcription') },
        enabled: true,
        global: false,
        category: 'transcription'
      },
      {
        id: 'save-transcription',
        name: 'Save Transcription',
        description: 'Save current transcription',
        defaultKeys: ['Ctrl', 'S'],
        action: { type: 'save-transcription', handler: () => this.emit('save-transcription') },
        enabled: true,
        global: false,
        category: 'transcription'
      },
      {
        id: 'copy-transcription',
        name: 'Copy Transcription',
        description: 'Copy transcription to clipboard',
        defaultKeys: ['Ctrl', 'Shift', 'C'],
        action: { type: 'copy-transcription', handler: () => this.emit('copy-transcription') },
        enabled: true,
        global: false,
        category: 'transcription'
      },
      {
        id: 'clear-transcription',
        name: 'Clear Transcription',
        description: 'Clear current transcription',
        defaultKeys: ['Ctrl', 'Shift', 'Delete'],
        action: { type: 'clear-transcription', handler: () => this.emit('clear-transcription') },
        enabled: true,
        global: false,
        category: 'transcription'
      },

      // Voice Engine Controls
      {
        id: 'switch-engine',
        name: 'Switch Engine',
        description: 'Cycle through voice recognition engines',
        defaultKeys: ['Ctrl', 'Shift', 'E'],
        action: { type: 'switch-engine', handler: () => this.emit('switch-engine') },
        enabled: true,
        global: false,
        category: 'voice'
      },
      {
        id: 'toggle-noise-reduction',
        name: 'Toggle Noise Reduction',
        description: 'Enable/disable noise reduction',
        defaultKeys: ['Ctrl', 'Shift', 'N'],
        action: { type: 'toggle-noise-reduction', handler: () => this.emit('toggle-noise-reduction') },
        enabled: true,
        global: false,
        category: 'voice'
      },

      // Navigation Controls
      {
        id: 'open-settings',
        name: 'Open Settings',
        description: 'Open application settings',
        defaultKeys: ['Ctrl', ','],
        action: { type: 'open-settings', handler: () => this.emit('open-settings') },
        enabled: true,
        global: false,
        category: 'navigation'
      },
      {
        id: 'focus-search',
        name: 'Focus Search',
        description: 'Focus the search input',
        defaultKeys: ['Ctrl', 'K'],
        action: { type: 'focus-search', handler: () => this.emit('focus-search') },
        enabled: true,
        global: false,
        category: 'navigation'
      },
      {
        id: 'new-session',
        name: 'New Session',
        description: 'Create a new voice session',
        defaultKeys: ['Ctrl', 'N'],
        action: { type: 'new-session', handler: () => this.emit('new-session') },
        enabled: true,
        global: false,
        category: 'general'
      },
      {
        id: 'toggle-theme',
        name: 'Toggle Theme',
        description: 'Switch between light and dark theme',
        defaultKeys: ['Ctrl', 'Shift', 'L'],
        action: { type: 'toggle-theme', handler: () => this.emit('toggle-theme') },
        enabled: true,
        global: false,
        category: 'general'
      }
    ];

    // Initialize hotkeys with current keys same as default
    defaultHotkeys.forEach(config => {
      const hotkey: HotkeyConfig = {
        ...config,
        currentKeys: [...config.defaultKeys]
      };
      this.hotkeys.set(hotkey.id, hotkey);
      this.updateKeyCombination(hotkey);
    });
  }

  /**
   * Setup keyboard event listeners
   */
  private setupEventListeners(): void {
    // Global keydown listener
    document.addEventListener('keydown', this.handleKeyDown.bind(this), true);
    document.addEventListener('keyup', this.handleKeyUp.bind(this), true);
    
    // Prevent default browser shortcuts that conflict
    document.addEventListener('keydown', (e) => {
      const combo = this.getKeyCombo(e as any);
      if (this.activeKeyCombos.has(combo)) {
        e.preventDefault();
        e.stopPropagation();
      }
    }, true);

    // Focus and blur events to manage global hotkeys
    window.addEventListener('focus', () => {
      this.isListening = true;
      this.emit('focus-changed', true);
    });
    
    window.addEventListener('blur', () => {
      // Keep global hotkeys active even when window loses focus
      this.keyStates.clear();
      this.emit('focus-changed', false);
    });
  }

  /**
   * Handle keydown events
   */
  private handleKeyDown(event: KeyboardEvent): void {
    const key = event.code || event.key;
    this.keyStates.set(key, true);

    const combo = this.getKeyCombo(event);
    const hotkey = this.activeKeyCombos.get(combo);

    if (hotkey && hotkey.enabled) {
      // Check if we should handle global hotkeys when window is not focused
      if (!this.isListening && !hotkey.global) {
        return;
      }

      // Prevent default browser behavior
      event.preventDefault();
      event.stopPropagation();

      // Execute the hotkey action
      try {
        hotkey.action.handler();
        this.emit('hotkey-triggered', { hotkey, event });
      } catch (error) {
        console.error('Hotkey execution error:', error);
        this.emit('hotkey-error', { hotkey, error, event });
      }
    }
  }

  /**
   * Handle keyup events
   */
  private handleKeyUp(event: KeyboardEvent): void {
    const key = event.code || event.key;
    this.keyStates.delete(key);
  }

  /**
   * Generate key combination string from keyboard event
   */
  private getKeyCombo(event: KeyboardEvent): string {
    const modifiers: string[] = [];
    const key = event.code || event.key;

    if (event.ctrlKey || event.metaKey) modifiers.push('Ctrl');
    if (event.shiftKey) modifiers.push('Shift');
    if (event.altKey) modifiers.push('Alt');
    
    // Don't include the modifier key itself in the final key
    if (!['Control', 'Shift', 'Alt', 'Meta', 'ControlLeft', 'ControlRight', 
          'ShiftLeft', 'ShiftRight', 'AltLeft', 'AltRight', 'MetaLeft', 'MetaRight'].includes(key)) {
      modifiers.push(key);
    }

    return modifiers.join('+');
  }

  /**
   * Generate key combination string from key array
   */
  private getKeyComboFromArray(keys: string[]): string {
    return keys.join('+');
  }

  /**
   * Update key combination mapping
   */
  private updateKeyCombination(hotkey: HotkeyConfig): void {
    const combo = this.getKeyComboFromArray(hotkey.currentKeys);
    this.activeKeyCombos.set(combo, hotkey);
  }

  /**
   * Register a new hotkey
   */
  registerHotkey(config: Omit<HotkeyConfig, 'currentKeys'>): void {
    const hotkey: HotkeyConfig = {
      ...config,
      currentKeys: [...config.defaultKeys]
    };
    
    this.hotkeys.set(hotkey.id, hotkey);
    this.updateKeyCombination(hotkey);
    this.emit('hotkey-registered', hotkey);
  }

  /**
   * Unregister a hotkey
   */
  unregisterHotkey(id: string): boolean {
    const hotkey = this.hotkeys.get(id);
    if (!hotkey) return false;

    const combo = this.getKeyComboFromArray(hotkey.currentKeys);
    this.activeKeyCombos.delete(combo);
    this.hotkeys.delete(id);
    this.emit('hotkey-unregistered', id);
    
    return true;
  }

  /**
   * Update hotkey configuration
   */
  updateHotkey(id: string, updates: Partial<HotkeyConfig>): boolean {
    const hotkey = this.hotkeys.get(id);
    if (!hotkey) return false;

    // Remove old key combination
    const oldCombo = this.getKeyComboFromArray(hotkey.currentKeys);
    this.activeKeyCombos.delete(oldCombo);

    // Update hotkey
    Object.assign(hotkey, updates);
    
    // Add new key combination
    this.updateKeyCombination(hotkey);
    this.emit('hotkey-updated', hotkey);
    
    return true;
  }

  /**
   * Get all registered hotkeys
   */
  getAllHotkeys(): HotkeyConfig[] {
    return Array.from(this.hotkeys.values());
  }

  /**
   * Get hotkeys by category
   */
  getHotkeysByCategory(category: HotkeyConfig['category']): HotkeyConfig[] {
    return Array.from(this.hotkeys.values()).filter(h => h.category === category);
  }

  /**
   * Get hotkey by ID
   */
  getHotkey(id: string): HotkeyConfig | undefined {
    return this.hotkeys.get(id);
  }

  /**
   * Enable/disable a hotkey
   */
  toggleHotkey(id: string, enabled?: boolean): boolean {
    const hotkey = this.hotkeys.get(id);
    if (!hotkey) return false;

    hotkey.enabled = enabled !== undefined ? enabled : !hotkey.enabled;
    this.emit('hotkey-toggled', { id, enabled: hotkey.enabled });
    
    return true;
  }

  /**
   * Reset hotkey to default keys
   */
  resetHotkey(id: string): boolean {
    const hotkey = this.hotkeys.get(id);
    if (!hotkey) return false;

    return this.updateHotkey(id, { currentKeys: [...hotkey.defaultKeys] });
  }

  /**
   * Reset all hotkeys to defaults
   */
  resetAllHotkeys(): void {
    this.hotkeys.forEach((hotkey, id) => {
      this.resetHotkey(id);
    });
    this.emit('all-hotkeys-reset');
  }

  /**
   * Check if key combination is already in use
   */
  isKeyCombinationInUse(keys: string[], excludeId?: string): boolean {
    const combo = this.getKeyComboFromArray(keys);
    const existing = this.activeKeyCombos.get(combo);
    return existing !== undefined && existing.id !== excludeId;
  }

  /**
   * Get formatted key combination string for display
   */
  formatKeyCombo(keys: string[]): string {
    return keys.map(key => {
      // Format common keys for display
      const keyMap: Record<string, string> = {
        'Control': 'Ctrl',
        'Meta': 'Cmd',
        'ArrowUp': '↑',
        'ArrowDown': '↓',
        'ArrowLeft': '←',
        'ArrowRight': '→',
        'Space': 'Space',
        'Enter': 'Enter',
        'Escape': 'Esc',
        'Backspace': '⌫',
        'Delete': 'Del'
      };
      
      return keyMap[key] || key;
    }).join(' + ');
  }

  /**
   * Save hotkey configuration to localStorage
   */
  saveConfiguration(): void {
    const config = Array.from(this.hotkeys.values()).map(hotkey => ({
      id: hotkey.id,
      currentKeys: hotkey.currentKeys,
      enabled: hotkey.enabled
    }));
    
    localStorage.setItem('voiceAssistant-hotkeys', JSON.stringify(config));
    this.emit('configuration-saved', config);
  }

  /**
   * Load hotkey configuration from localStorage
   */
  loadConfiguration(): void {
    try {
      const saved = localStorage.getItem('voiceAssistant-hotkeys');
      if (!saved) return;

      const config = JSON.parse(saved);
      config.forEach((item: any) => {
        const hotkey = this.hotkeys.get(item.id);
        if (hotkey) {
          this.updateHotkey(item.id, {
            currentKeys: item.currentKeys,
            enabled: item.enabled
          });
        }
      });

      this.emit('configuration-loaded', config);
    } catch (error) {
      console.error('Failed to load hotkey configuration:', error);
      this.emit('configuration-load-error', error);
    }
  }

  /**
   * Export hotkey configuration
   */
  exportConfiguration(): string {
    const config = {
      version: '1.0',
      timestamp: new Date().toISOString(),
      hotkeys: Array.from(this.hotkeys.values())
    };
    
    return JSON.stringify(config, null, 2);
  }

  /**
   * Import hotkey configuration
   */
  importConfiguration(configJson: string): boolean {
    try {
      const config = JSON.parse(configJson);
      
      if (config.version !== '1.0') {
        throw new Error('Unsupported configuration version');
      }

      config.hotkeys.forEach((hotkey: HotkeyConfig) => {
        if (this.hotkeys.has(hotkey.id)) {
          this.updateHotkey(hotkey.id, {
            currentKeys: hotkey.currentKeys,
            enabled: hotkey.enabled
          });
        }
      });

      this.saveConfiguration();
      this.emit('configuration-imported', config);
      return true;
    } catch (error) {
      console.error('Failed to import hotkey configuration:', error);
      this.emit('configuration-import-error', error);
      return false;
    }
  }

  /**
   * Start listening for hotkeys
   */
  startListening(): void {
    this.isListening = true;
    this.emit('listening-started');
  }

  /**
   * Stop listening for hotkeys
   */
  stopListening(): void {
    this.isListening = false;
    this.keyStates.clear();
    this.emit('listening-stopped');
  }

  /**
   * Event system
   */
  on(event: string, callback: Function): void {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, []);
    }
    this.eventListeners.get(event)!.push(callback);
  }

  off(event: string, callback: Function): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    }
  }

  private emit(event: string, data?: any): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      listeners.forEach(callback => callback(data));
    }
  }

  /**
   * Cleanup resources
   */
  dispose(): void {
    document.removeEventListener('keydown', this.handleKeyDown.bind(this), true);
    document.removeEventListener('keyup', this.handleKeyUp.bind(this), true);
    
    this.hotkeys.clear();
    this.activeKeyCombos.clear();
    this.keyStates.clear();
    this.eventListeners.clear();
    
    this.isListening = false;
  }
}

// Create singleton instance
export const hotkeyService = new HotkeyService();
export default hotkeyService;