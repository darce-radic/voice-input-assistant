import React, { useState, useEffect } from 'react';
import hotkeyService, { HotkeyConfig } from '../services/hotkeyService';

interface HotkeySettingsProps {
  isOpen?: boolean;
  onClose?: () => void;
}

const HotkeySettings: React.FC<HotkeySettingsProps> = ({ isOpen = true, onClose }) => {
  const [hotkeys, setHotkeys] = useState<HotkeyConfig[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('voice');
  const [editingHotkey, setEditingHotkey] = useState<string | null>(null);
  const [recordingKeys, setRecordingKeys] = useState(false);
  const [tempKeys, setTempKeys] = useState<string[]>([]);
  const [searchTerm, setSearchTerm] = useState('');

  const categories = [
    { id: 'voice', name: 'Voice Controls', icon: '🎤' },
    { id: 'transcription', name: 'Transcription', icon: '📝' },
    { id: 'navigation', name: 'Navigation', icon: '🧭' },
    { id: 'general', name: 'General', icon: '⚙️' }
  ];

  useEffect(() => {
    loadHotkeys();
    
    // Listen for hotkey service events
    hotkeyService.on('hotkey-updated', loadHotkeys);
    hotkeyService.on('hotkey-toggled', loadHotkeys);
    hotkeyService.on('configuration-loaded', loadHotkeys);
    
    return () => {
      hotkeyService.off('hotkey-updated', loadHotkeys);
      hotkeyService.off('hotkey-toggled', loadHotkeys);
      hotkeyService.off('configuration-loaded', loadHotkeys);
    };
  }, []);

  const loadHotkeys = () => {
    setHotkeys(hotkeyService.getAllHotkeys());
  };

  const filteredHotkeys = hotkeys.filter(hotkey => {
    const matchesCategory = selectedCategory === 'all' || hotkey.category === selectedCategory;
    const matchesSearch = searchTerm === '' || 
      hotkey.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      hotkey.description.toLowerCase().includes(searchTerm.toLowerCase());
    return matchesCategory && matchesSearch;
  });

  const handleToggleHotkey = (id: string) => {
    hotkeyService.toggleHotkey(id);
  };

  const handleEditHotkey = (id: string) => {
    const hotkey = hotkeyService.getHotkey(id);
    if (hotkey) {
      setEditingHotkey(id);
      setTempKeys([...hotkey.currentKeys]);
    }
  };

  const handleStartRecording = () => {
    setRecordingKeys(true);
    setTempKeys([]);
    
    // Listen for key combinations
    const handleKeyDown = (e: KeyboardEvent) => {
      e.preventDefault();
      e.stopPropagation();
      
      const keys: string[] = [];
      if (e.ctrlKey || e.metaKey) keys.push('Ctrl');
      if (e.shiftKey) keys.push('Shift');
      if (e.altKey) keys.push('Alt');
      
      const key = e.code || e.key;
      if (!['Control', 'Shift', 'Alt', 'Meta', 'ControlLeft', 'ControlRight', 
            'ShiftLeft', 'ShiftRight', 'AltLeft', 'AltRight', 'MetaLeft', 'MetaRight'].includes(key)) {
        keys.push(key);
      }
      
      if (keys.length > 0) {
        setTempKeys(keys);
      }
    };
    
    const handleKeyUp = () => {
      if (tempKeys.length > 0) {
        setRecordingKeys(false);
        document.removeEventListener('keydown', handleKeyDown, true);
        document.removeEventListener('keyup', handleKeyUp, true);
      }
    };
    
    document.addEventListener('keydown', handleKeyDown, true);
    document.addEventListener('keyup', handleKeyUp, true);
    
    // Auto-stop recording after 5 seconds
    setTimeout(() => {
      setRecordingKeys(false);
      document.removeEventListener('keydown', handleKeyDown, true);
      document.removeEventListener('keyup', handleKeyUp, true);
    }, 5000);
  };

  const handleSaveHotkey = () => {
    if (editingHotkey && tempKeys.length > 0) {
      // Check if key combination is already in use
      const isInUse = hotkeyService.isKeyCombinationInUse(tempKeys, editingHotkey);
      if (isInUse) {
        alert('This key combination is already in use by another hotkey.');
        return;
      }
      
      hotkeyService.updateHotkey(editingHotkey, { currentKeys: tempKeys });
      setEditingHotkey(null);
      setTempKeys([]);
    }
  };

  const handleCancelEdit = () => {
    setEditingHotkey(null);
    setTempKeys([]);
    setRecordingKeys(false);
  };

  const handleResetHotkey = (id: string) => {
    hotkeyService.resetHotkey(id);
  };

  const handleResetAll = () => {
    if (window.confirm('Are you sure you want to reset all hotkeys to their defaults?')) {
      hotkeyService.resetAllHotkeys();
    }
  };

  const handleExport = () => {
    const config = hotkeyService.exportConfiguration();
    const blob = new Blob([config], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = `voice-assistant-hotkeys-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handleImport = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target?.result as string;
        const success = hotkeyService.importConfiguration(content);
        if (success) {
          alert('Hotkey configuration imported successfully!');
        } else {
          alert('Failed to import configuration. Please check the file format.');
        }
      };
      reader.readAsText(file);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="hotkey-settings">
      <div className="settings-overlay" onClick={onClose} />
      <div className="settings-modal">
        <div className="settings-header">
          <h2>🎹 Keyboard Shortcuts</h2>
          <div className="header-actions">
            <button onClick={handleExport} className="export-btn">
              📤 Export
            </button>
            <label className="import-btn">
              📥 Import
              <input type="file" accept=".json" onChange={handleImport} style={{display: 'none'}} />
            </label>
            <button onClick={handleResetAll} className="reset-all-btn">
              🔄 Reset All
            </button>
            {onClose && (
              <button onClick={onClose} className="close-btn">✕</button>
            )}
          </div>
        </div>

        <div className="settings-content">
          {/* Search and Filter */}
          <div className="search-filter">
            <input
              type="text"
              placeholder="Search hotkeys..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="search-input"
            />
            <div className="category-tabs">
              {categories.map(category => (
                <button
                  key={category.id}
                  onClick={() => setSelectedCategory(category.id)}
                  className={`category-tab ${selectedCategory === category.id ? 'active' : ''}`}
                >
                  {category.icon} {category.name}
                </button>
              ))}
            </div>
          </div>

          {/* Hotkeys List */}
          <div className="hotkeys-list">
            {filteredHotkeys.map(hotkey => (
              <div key={hotkey.id} className={`hotkey-item ${!hotkey.enabled ? 'disabled' : ''}`}>
                <div className="hotkey-info">
                  <div className="hotkey-header">
                    <span className="hotkey-name">{hotkey.name}</span>
                    <div className="hotkey-controls">
                      <label className="enable-toggle">
                        <input
                          type="checkbox"
                          checked={hotkey.enabled}
                          onChange={() => handleToggleHotkey(hotkey.id)}
                        />
                        <span className="toggle-slider"></span>
                      </label>
                      <button 
                        onClick={() => handleEditHotkey(hotkey.id)}
                        className="edit-btn"
                        disabled={editingHotkey === hotkey.id}
                      >
                        ✏️
                      </button>
                      <button 
                        onClick={() => handleResetHotkey(hotkey.id)}
                        className="reset-btn"
                        title="Reset to default"
                      >
                        🔄
                      </button>
                    </div>
                  </div>
                  
                  <p className="hotkey-description">{hotkey.description}</p>
                  
                  <div className="hotkey-keys">
                    {editingHotkey === hotkey.id ? (
                      <div className="key-editor">
                        <div className="current-keys">
                          {recordingKeys ? (
                            <span className="recording">🎤 Recording keys... (press any combination)</span>
                          ) : tempKeys.length > 0 ? (
                            <span className="temp-keys">
                              {hotkeyService.formatKeyCombo(tempKeys)}
                            </span>
                          ) : (
                            <span className="no-keys">No keys recorded</span>
                          )}
                        </div>
                        <div className="editor-buttons">
                          <button 
                            onClick={handleStartRecording}
                            disabled={recordingKeys}
                            className="record-btn"
                          >
                            {recordingKeys ? '🎤 Recording...' : '🎤 Record Keys'}
                          </button>
                          <button 
                            onClick={handleSaveHotkey}
                            disabled={tempKeys.length === 0}
                            className="save-btn"
                          >
                            ✅ Save
                          </button>
                          <button 
                            onClick={handleCancelEdit}
                            className="cancel-btn"
                          >
                            ❌ Cancel
                          </button>
                        </div>
                      </div>
                    ) : (
                      <div className="key-combo">
                        {hotkeyService.formatKeyCombo(hotkey.currentKeys)}
                        {hotkey.global && <span className="global-badge">Global</span>}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {filteredHotkeys.length === 0 && (
            <div className="no-results">
              <p>No hotkeys found matching your search.</p>
            </div>
          )}
        </div>

        <div className="settings-footer">
          <div className="help-text">
            <p><strong>Pro Tips:</strong></p>
            <ul>
              <li>Global hotkeys work even when the app is not focused</li>
              <li>Use F-keys for quick voice control (F2 for record toggle)</li>
              <li>Combine Ctrl+Shift with letters for advanced functions</li>
              <li>Export your settings to share across devices</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};

export default HotkeySettings;