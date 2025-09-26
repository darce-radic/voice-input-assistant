using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;
using Application = System.Windows.Application;

namespace VoiceInputAssistant.Services
{
    /// <summary>
    /// Service for managing system tray integration
    /// </summary>
    public class SystemTrayService : ISystemTrayService, IDisposable
    {
    private readonly ILogger<SystemTrayService> _logger;
    private NotifyIcon _notifyIcon = null!;
    private bool _disposed;

    public event EventHandler ShowMainWindow = null!;
    public event EventHandler ExitApplication = null!;

        public bool IsVisible => _notifyIcon?.Visible ?? false;

        public SystemTrayService(ILogger<SystemTrayService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeTrayIcon();
            _logger.LogDebug("SystemTrayService initialized");
        }

        private void InitializeTrayIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = LoadTrayIcon(),
                    Text = "Voice Input Assistant",
                    Visible = false
                };

                // Create context menu
                var contextMenu = new ContextMenuStrip();
                
                // Show/Hide main window
                var showHideItem = new ToolStripMenuItem("Show Window")
                {
                    Font = new Font(contextMenu.Font, FontStyle.Bold)
                };
                showHideItem.Click += (s, e) => ShowMainWindow?.Invoke(this, EventArgs.Empty);
                contextMenu.Items.Add(showHideItem);
                
                contextMenu.Items.Add(new ToolStripSeparator());
                
                // Quick actions
                var startListeningItem = new ToolStripMenuItem("Start Listening");
                startListeningItem.Click += (s, e) => 
                {
                    // This would trigger the voice recognition service
                    _logger.LogDebug("Start listening requested from system tray");
                };
                contextMenu.Items.Add(startListeningItem);
                
                var settingsItem = new ToolStripMenuItem("Settings");
                settingsItem.Click += (s, e) => 
                {
                    ShowMainWindow?.Invoke(this, EventArgs.Empty);
                    // Settings window would be opened by the main window
                };
                contextMenu.Items.Add(settingsItem);
                
                contextMenu.Items.Add(new ToolStripSeparator());
                
                // Exit
                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (s, e) => ExitApplication?.Invoke(this, EventArgs.Empty);
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
                
                // Double-click to show main window
                _notifyIcon.MouseDoubleClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ShowMainWindow?.Invoke(this, EventArgs.Empty);
                    }
                };

                _logger.LogDebug("System tray icon initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize system tray icon");
                throw;
            }
        }

        private Icon LoadTrayIcon()
        {
            try
            {
                // Try to load from embedded resources first
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("VoiceInputAssistant.Assets.tray-icon.ico"))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }

                // Try to load from file system
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "tray-icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                // Fall back to application icon
                iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                // Use system default if no icon found
                _logger.LogWarning("No tray icon found, using system default");
                return SystemIcons.Application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load tray icon, using system default");
                return SystemIcons.Application;
            }
        }

        public void ShowTrayIcon(string? title = null, string? message = null)
        {
            try
            {
                if (_notifyIcon == null || _disposed) return;

                _notifyIcon.Visible = true;
                
                if (!string.IsNullOrEmpty(message))
                {
                    var balloonTitle = title ?? "Voice Input Assistant";
                    _notifyIcon.ShowBalloonTip(3000, balloonTitle, message, ToolTipIcon.Info);
                }

                _logger.LogDebug("System tray icon shown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show tray icon");
            }
        }

        public void HideTrayIcon()
        {
            try
            {
                if (_notifyIcon == null || _disposed) return;

                _notifyIcon.Visible = false;
                _logger.LogDebug("System tray icon hidden");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide tray icon");
            }
        }

        public void ShowBalloonTip(string title, string message, BalloonTipType tipType = BalloonTipType.Info)
        {
            try
            {
                if (_notifyIcon == null || _disposed) return;

                var toolTipIcon = tipType switch
                {
                    BalloonTipType.Info => ToolTipIcon.Info,
                    BalloonTipType.Warning => ToolTipIcon.Warning,
                    BalloonTipType.Error => ToolTipIcon.Error,
                    _ => ToolTipIcon.None
                };

                _notifyIcon.ShowBalloonTip(5000, title, message, toolTipIcon);
                _logger.LogDebug("Balloon tip shown: {Title} - {Message}", title, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show balloon tip");
            }
        }

        public void UpdateTrayIconText(string text)
        {
            try
            {
                if (_notifyIcon == null || _disposed) return;

                // Truncate text if too long (Windows limitation)
                var maxLength = 63; // Windows NotifyIcon text limit
                if (text.Length > maxLength)
                {
                    text = text.Substring(0, maxLength);
                }

                _notifyIcon.Text = text;
                _logger.LogDebug("Tray icon text updated: {Text}", text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update tray icon text");
            }
        }

        public void UpdateTrayIcon(Icon icon)
        {
            try
            {
                if (_notifyIcon == null || _disposed || icon == null) return;

                _notifyIcon.Icon = icon;
                _logger.LogDebug("Tray icon updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update tray icon");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (_notifyIcon != null)
                        {
                            _notifyIcon.Visible = false;
                            _notifyIcon.Dispose();
                            _notifyIcon = null!;
                        }

                        _logger?.LogDebug("SystemTrayService disposed");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error disposing SystemTrayService");
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~SystemTrayService()
        {
            Dispose(disposing: false);
        }
    }
}
