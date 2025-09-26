// Voice Input Assistant - Electron Main Process
// Windows desktop application with native OS integration

const { app, BrowserWindow, Menu, Tray, shell, ipcMain, globalShortcut, dialog } = require('electron');
const path = require('path');
const isDev = require('electron-is-dev');
const { autoUpdater } = require('electron-updater');
const windowStateKeeper = require('electron-window-state');
const contextMenu = require('electron-context-menu');

// Configure auto-updater
autoUpdater.checkForUpdatesAndNotify();

// Keep a global reference of the window object
let mainWindow;
let tray = null;
let isQuitting = false;

// Enable live reload for development
if (isDev) {
  require('electron-reload')(__dirname, {
    electron: path.join(__dirname, '..', 'node_modules', '.bin', 'electron'),
    hardResetMethod: 'exit'
  });
}

function createWindow() {
  // Load previous window state or set defaults
  let mainWindowState = windowStateKeeper({
    defaultWidth: 1200,
    defaultHeight: 800
  });

  // Create the browser window
  mainWindow = new BrowserWindow({
    x: mainWindowState.x,
    y: mainWindowState.y,
    width: mainWindowState.width,
    height: mainWindowState.height,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      enableRemoteModule: false,
      preload: path.join(__dirname, 'preload.js'),
      webSecurity: !isDev
    },
    icon: path.join(__dirname, 'assets', 'icon.png'),
    title: 'Voice Input Assistant',
    show: false, // Don't show until ready-to-show
    titleBarStyle: process.platform === 'darwin' ? 'hiddenInset' : 'default',
    frame: true,
    backgroundColor: '#ffffff'
  });

  // Let windowStateKeeper manage the window
  mainWindowState.manage(mainWindow);

  // Load the app
  const startUrl = isDev 
    ? 'http://localhost:3000' 
    : `file://${path.join(__dirname, '../build/index.html')}`;
  
  mainWindow.loadURL(startUrl);

  // Show window when ready
  mainWindow.once('ready-to-show', () => {
    mainWindow.show();
    
    // Focus on launch
    if (isDev) {
      mainWindow.webContents.openDevTools();
    }
    
    // Register global shortcuts
    registerGlobalShortcuts();
  });

  // Handle window closed
  mainWindow.on('closed', () => {
    mainWindow = null;
  });

  // Handle minimize to tray
  mainWindow.on('minimize', (event) => {
    if (tray) {
      event.preventDefault();
      mainWindow.hide();
    }
  });

  // Handle close button
  mainWindow.on('close', (event) => {
    if (!isQuitting) {
      event.preventDefault();
      if (tray) {
        mainWindow.hide();
      } else {
        app.quit();
      }
    }
  });

  // Handle navigation
  mainWindow.webContents.on('new-window', (event, navigationUrl) => {
    event.preventDefault();
    shell.openExternal(navigationUrl);
  });

  // Handle download
  mainWindow.webContents.session.on('will-download', (event, item, webContents) => {
    // Set the save path, making Electron not to prompt a save dialog
    const downloadsPath = app.getPath('downloads');
    const fileName = item.getFilename();
    item.setSavePath(path.join(downloadsPath, fileName));

    item.on('updated', (event, state) => {
      if (state === 'interrupted') {
        console.log('Download is interrupted but can be resumed');
      } else if (state === 'progressing') {
        if (item.isPaused()) {
          console.log('Download is paused');
        } else {
          console.log(`Received bytes: ${item.getReceivedBytes()}`);
        }
      }
    });

    item.once('done', (event, state) => {
      if (state === 'completed') {
        console.log('Download successfully');
        // Show notification
        new Notification('Download Complete', {
          body: `${fileName} has been downloaded successfully.`
        });
      } else {
        console.log(`Download failed: ${state}`);
      }
    });
  });

  // Set up context menu
  contextMenu({
    showInspectElement: isDev
  });
}

function createTray() {
  if (process.platform === 'darwin') return; // macOS handles this differently

  tray = new Tray(path.join(__dirname, 'assets', 'tray-icon.png'));
  
  const contextMenu = Menu.buildFromTemplate([
    {
      label: 'Show Voice Input Assistant',
      click: () => {
        if (mainWindow) {
          mainWindow.show();
          if (mainWindow.isMinimized()) mainWindow.restore();
          mainWindow.focus();
        }
      }
    },
    { type: 'separator' },
    {
      label: 'Start Recording',
      click: () => {
        if (mainWindow) {
          mainWindow.webContents.send('shortcut-triggered', 'start-recording');
        }
      }
    },
    {
      label: 'Stop Recording',
      click: () => {
        if (mainWindow) {
          mainWindow.webContents.send('shortcut-triggered', 'stop-recording');
        }
      }
    },
    { type: 'separator' },
    {
      label: 'Settings',
      click: () => {
        if (mainWindow) {
          mainWindow.webContents.send('navigate-to', '/settings');
          mainWindow.show();
        }
      }
    },
    { type: 'separator' },
    {
      label: 'Quit',
      click: () => {
        isQuitting = true;
        app.quit();
      }
    }
  ]);
  
  tray.setToolTip('Voice Input Assistant');
  tray.setContextMenu(contextMenu);
  
  // Handle tray double-click
  tray.on('double-click', () => {
    if (mainWindow) {
      mainWindow.show();
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.focus();
    }
  });
}

function registerGlobalShortcuts() {
  // Register global shortcuts for voice recording
  const shortcuts = [
    {
      key: 'CommandOrControl+Shift+R',
      action: 'toggle-recording'
    },
    {
      key: 'CommandOrControl+Shift+S',
      action: 'stop-recording'
    },
    {
      key: 'CommandOrControl+Shift+O',
      action: 'open-app'
    }
  ];

  shortcuts.forEach(({ key, action }) => {
    const success = globalShortcut.register(key, () => {
      if (mainWindow) {
        mainWindow.webContents.send('shortcut-triggered', action);
        
        // Show app for certain actions
        if (action === 'open-app') {
          mainWindow.show();
          if (mainWindow.isMinimized()) mainWindow.restore();
          mainWindow.focus();
        }
      }
    });

    if (!success) {
      console.log(`Failed to register global shortcut: ${key}`);
    }
  });
}

function createMenu() {
  const template = [
    {
      label: 'File',
      submenu: [
        {
          label: 'New Recording',
          accelerator: 'CmdOrCtrl+N',
          click: () => {
            if (mainWindow) {
              mainWindow.webContents.send('menu-action', 'new-recording');
            }
          }
        },
        {
          label: 'Open Recent',
          submenu: [
            {
              label: 'Clear Recent',
              click: () => {
                if (mainWindow) {
                  mainWindow.webContents.send('menu-action', 'clear-recent');
                }
              }
            }
          ]
        },
        { type: 'separator' },
        {
          label: 'Export Transcription',
          accelerator: 'CmdOrCtrl+E',
          click: () => {
            if (mainWindow) {
              mainWindow.webContents.send('menu-action', 'export-transcription');
            }
          }
        },
        { type: 'separator' },
        {
          label: 'Quit',
          accelerator: process.platform === 'darwin' ? 'Cmd+Q' : 'Ctrl+Q',
          click: () => {
            isQuitting = true;
            app.quit();
          }
        }
      ]
    },
    {
      label: 'Edit',
      submenu: [
        { role: 'undo' },
        { role: 'redo' },
        { type: 'separator' },
        { role: 'cut' },
        { role: 'copy' },
        { role: 'paste' },
        { role: 'selectall' }
      ]
    },
    {
      label: 'Recording',
      submenu: [
        {
          label: 'Start Recording',
          accelerator: 'CmdOrCtrl+Shift+R',
          click: () => {
            if (mainWindow) {
              mainWindow.webContents.send('shortcut-triggered', 'start-recording');
            }
          }
        },
        {
          label: 'Stop Recording',
          accelerator: 'CmdOrCtrl+Shift+S',
          click: () => {
            if (mainWindow) {
              mainWindow.webContents.send('shortcut-triggered', 'stop-recording');
            }
          }
        },
        { type: 'separator' },
        {
          label: 'Recording Settings',
          click: () => {
            if (mainWindow) {
              mainWindow.webContents.send('navigate-to', '/settings/recording');
            }
          }
        }
      ]
    },
    {
      label: 'View',
      submenu: [
        { role: 'reload' },
        { role: 'forceReload' },
        { role: 'toggleDevTools' },
        { type: 'separator' },
        { role: 'resetZoom' },
        { role: 'zoomIn' },
        { role: 'zoomOut' },
        { type: 'separator' },
        { role: 'togglefullscreen' }
      ]
    },
    {
      label: 'Window',
      submenu: [
        { role: 'minimize' },
        { role: 'close' },
        { type: 'separator' },
        {
          label: 'Hide to Tray',
          accelerator: 'CmdOrCtrl+H',
          click: () => {
            if (mainWindow && tray) {
              mainWindow.hide();
            }
          }
        }
      ]
    },
    {
      label: 'Help',
      submenu: [
        {
          label: 'About Voice Input Assistant',
          click: () => {
            dialog.showMessageBox(mainWindow, {
              type: 'info',
              title: 'About Voice Input Assistant',
              message: 'Voice Input Assistant',
              detail: `Version ${app.getVersion()}\nAI-powered voice transcription and processing application.`,
              buttons: ['OK']
            });
          }
        },
        {
          label: 'Check for Updates',
          click: () => {
            autoUpdater.checkForUpdatesAndNotify();
          }
        },
        { type: 'separator' },
        {
          label: 'Support',
          click: () => {
            shell.openExternal('https://voiceinputassistant.com/support');
          }
        },
        {
          label: 'Privacy Policy',
          click: () => {
            shell.openExternal('https://voiceinputassistant.com/privacy');
          }
        }
      ]
    }
  ];

  const menu = Menu.buildFromTemplate(template);
  Menu.setApplicationMenu(menu);
}

// App event handlers
app.whenReady().then(() => {
  createWindow();
  createTray();
  createMenu();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  isQuitting = true;
});

app.on('will-quit', () => {
  // Unregister all shortcuts
  globalShortcut.unregisterAll();
});

// Handle app updates
autoUpdater.on('update-available', () => {
  dialog.showMessageBox(mainWindow, {
    type: 'info',
    title: 'Update Available',
    message: 'A new version of Voice Input Assistant is available. It will be downloaded in the background.',
    buttons: ['OK']
  });
});

autoUpdater.on('update-downloaded', () => {
  dialog.showMessageBox(mainWindow, {
    type: 'info',
    title: 'Update Ready',
    message: 'Update downloaded. The application will restart to apply the update.',
    buttons: ['Restart Now', 'Later']
  }).then((result) => {
    if (result.response === 0) {
      autoUpdater.quitAndInstall();
    }
  });
});

// IPC handlers
ipcMain.handle('get-app-version', () => {
  return app.getVersion();
});

ipcMain.handle('get-app-path', (event, name) => {
  return app.getPath(name);
});

ipcMain.handle('show-save-dialog', async (event, options) => {
  const result = await dialog.showSaveDialog(mainWindow, options);
  return result;
});

ipcMain.handle('show-open-dialog', async (event, options) => {
  const result = await dialog.showOpenDialog(mainWindow, options);
  return result;
});

ipcMain.handle('show-message-box', async (event, options) => {
  const result = await dialog.showMessageBox(mainWindow, options);
  return result;
});

// Handle protocol for deep linking (e.g., voice-assistant://start-recording)
app.setAsDefaultProtocolClient('voice-assistant');

app.on('second-instance', (event, commandLine, workingDirectory) => {
  // Someone tried to run a second instance, focus our window instead
  if (mainWindow) {
    if (mainWindow.isMinimized()) mainWindow.restore();
    mainWindow.focus();
  }
});

// Handle protocol URLs
app.on('open-url', (event, url) => {
  event.preventDefault();
  
  if (mainWindow) {
    const action = url.replace('voice-assistant://', '');
    mainWindow.webContents.send('protocol-action', action);
    mainWindow.show();
    if (mainWindow.isMinimized()) mainWindow.restore();
    mainWindow.focus();
  }
});

// Security: prevent new window creation
app.on('web-contents-created', (event, contents) => {
  contents.on('new-window', (navigationEvent, url) => {
    navigationEvent.preventDefault();
    shell.openExternal(url);
  });
});

module.exports = { mainWindow };