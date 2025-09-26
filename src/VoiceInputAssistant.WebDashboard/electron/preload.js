// Voice Input Assistant - Electron Preload Script
// Secure communication bridge between main process and renderer

const { contextBridge, ipcRenderer } = require('electron');

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electronAPI', {
  // App information
  getAppVersion: () => ipcRenderer.invoke('get-app-version'),
  getAppPath: (name) => ipcRenderer.invoke('get-app-path', name),
  
  // Dialog methods
  showSaveDialog: (options) => ipcRenderer.invoke('show-save-dialog', options),
  showOpenDialog: (options) => ipcRenderer.invoke('show-open-dialog', options),
  showMessageBox: (options) => ipcRenderer.invoke('show-message-box', options),
  
  // Event listeners
  onShortcutTriggered: (callback) => {
    ipcRenderer.on('shortcut-triggered', (event, action) => callback(action));
  },
  onMenuAction: (callback) => {
    ipcRenderer.on('menu-action', (event, action) => callback(action));
  },
  onNavigateTo: (callback) => {
    ipcRenderer.on('navigate-to', (event, path) => callback(path));
  },
  onProtocolAction: (callback) => {
    ipcRenderer.on('protocol-action', (event, action) => callback(action));
  },
  
  // Remove event listeners
  removeAllListeners: (channel) => {
    ipcRenderer.removeAllListeners(channel);
  },
  
  // Platform information
  platform: process.platform,
  
  // Development mode check
  isDev: process.env.NODE_ENV === 'development',
});

// Expose secure file system operations
contextBridge.exposeInMainWorld('fileSystem', {
  // Read file (for imports)
  readFile: (filePath) => ipcRenderer.invoke('read-file', filePath),
  
  // Write file (for exports)
  writeFile: (filePath, content) => ipcRenderer.invoke('write-file', filePath, content),
  
  // Check if file exists
  fileExists: (filePath) => ipcRenderer.invoke('file-exists', filePath),
  
  // Get file info
  getFileInfo: (filePath) => ipcRenderer.invoke('get-file-info', filePath),
});

// Expose notification API
contextBridge.exposeInMainWorld('notifications', {
  show: (title, options) => {
    return new Notification(title, options);
  },
  
  requestPermission: () => {
    return Notification.requestPermission();
  },
  
  permission: Notification.permission
});

// Expose clipboard API
contextBridge.exposeInMainWorld('clipboard', {
  writeText: (text) => ipcRenderer.invoke('clipboard-write-text', text),
  readText: () => ipcRenderer.invoke('clipboard-read-text'),
  writeImage: (image) => ipcRenderer.invoke('clipboard-write-image', image),
  readImage: () => ipcRenderer.invoke('clipboard-read-image'),
});

// Expose system information
contextBridge.exposeInMainWorld('system', {
  getSystemInfo: () => ipcRenderer.invoke('get-system-info'),
  getCPUUsage: () => ipcRenderer.invoke('get-cpu-usage'),
  getMemoryUsage: () => ipcRenderer.invoke('get-memory-usage'),
  getNetworkStatus: () => ipcRenderer.invoke('get-network-status'),
});

// Security: Remove Node.js APIs from window object
delete window.require;
delete window.exports;
delete window.module;

// Log that preload script has loaded
console.log('Voice Input Assistant: Preload script loaded successfully');