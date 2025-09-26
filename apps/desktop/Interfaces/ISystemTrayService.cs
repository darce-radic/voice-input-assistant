using System;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for system tray service functionality
/// </summary>
public interface ISystemTrayService : IDisposable
{
    /// <summary>
    /// Gets whether the tray icon is currently visible
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// Event fired when user requests to show the main window
    /// </summary>
    event EventHandler ShowMainWindow;
    
    /// <summary>
    /// Event fired when user requests to exit the application
    /// </summary>
    event EventHandler ExitApplication;
    
    /// <summary>
    /// Show the tray icon with optional balloon tip
    /// </summary>
    /// <param name="title">Optional balloon tip title</param>
    /// <param name="message">Optional balloon tip message</param>
    void ShowTrayIcon(string? title = null, string? message = null);
    
    /// <summary>
    /// Hide the tray icon
    /// </summary>
    void HideTrayIcon();
    
    /// <summary>
    /// Show a balloon tip notification
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message</param>
    /// <param name="tipType">Type of notification</param>
    void ShowBalloonTip(string title, string message, BalloonTipType tipType = BalloonTipType.Info);
    
    /// <summary>
    /// Update the tray icon tooltip text
    /// </summary>
    /// <param name="text">New tooltip text</param>
    void UpdateTrayIconText(string text);
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