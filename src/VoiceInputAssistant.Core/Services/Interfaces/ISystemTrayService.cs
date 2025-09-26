using System;
using System.Drawing;

namespace VoiceInputAssistant.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for system tray integration service
    /// </summary>
    public interface ISystemTrayService
    {
        /// <summary>
        /// Event raised when the user requests to show the main window
        /// </summary>
        event EventHandler ShowMainWindow;

        /// <summary>
        /// Event raised when the user requests to exit the application
        /// </summary>
        event EventHandler ExitApplication;

        /// <summary>
        /// Gets whether the tray icon is currently visible
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Shows the system tray icon with optional balloon tip
        /// </summary>
        /// <param name="title">Optional title for the balloon tip</param>
        /// <param name="message">Optional message for the balloon tip</param>
        void ShowTrayIcon(string title = null, string message = null);

        /// <summary>
        /// Hides the system tray icon
        /// </summary>
        void HideTrayIcon();

        /// <summary>
        /// Shows a balloon tip notification
        /// </summary>
        /// <param name="title">Title of the notification</param>
        /// <param name="message">Message content</param>
        /// <param name="tipType">Type of tip (info, warning, error)</param>
        void ShowBalloonTip(string title, string message, BalloonTipType tipType = BalloonTipType.Info);

        /// <summary>
        /// Updates the tooltip text for the tray icon
        /// </summary>
        /// <param name="text">New tooltip text</param>
        void UpdateTrayIconText(string text);

        /// <summary>
        /// Updates the tray icon image
        /// </summary>
        /// <param name="icon">New icon to display</param>
        void UpdateTrayIcon(Icon icon);
    }

    /// <summary>
    /// Types of balloon tip notifications
    /// </summary>
    public enum BalloonTipType
    {
        None,
        Info,
        Warning,
        Error
    }
}