using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.ViewModels;

namespace VoiceInputAssistant.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ILogger<SettingsWindow>? _logger;

        public SettingsWindow()
        {
            InitializeComponent();
            
            // Initialize logging through DI
            _logger = App.ServiceProvider?.GetService(typeof(ILogger<SettingsWindow>)) as ILogger<SettingsWindow>;
            
            // Set up event handlers
            Loaded += SettingsWindow_Loaded;
            Closing += SettingsWindow_Closing;
            
            _logger?.LogDebug("SettingsWindow initialized");
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure the window is properly positioned
                if (Owner != null)
                {
                    // Center relative to parent window
                    Left = Owner.Left + (Owner.Width - Width) / 2;
                    Top = Owner.Top + (Owner.Height - Height) / 2;
                }

                // Initialize settings if view model is available
                if (DataContext is SettingsWindowViewModel viewModel)
                {
                    _ = Task.Run(async () => await viewModel.LoadSettings());
                }
                
                _logger?.LogDebug("SettingsWindow loaded successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during SettingsWindow load");
            }
        }

        private void SettingsWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                // Check if there are unsaved changes
                if (DataContext is SettingsWindowViewModel viewModel && viewModel.HasUnsavedChanges)
                {
                    var result = MessageBox.Show(
                        "You have unsaved changes. Do you want to save them before closing?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question,
                        MessageBoxResult.Cancel);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            viewModel.ApplySettings();
                            break;
                        case MessageBoxResult.No:
                            viewModel.DiscardChanges();
                            break;
                        case MessageBoxResult.Cancel:
                            e.Cancel = true;
                            return;
                    }
                }
                
                _logger?.LogDebug("SettingsWindow closing");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during SettingsWindow closing");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            try
            {
                // Additional initialization that requires window handle
                _logger?.LogDebug("SettingsWindow source initialized");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during source initialization");
            }
        }

        /// <summary>
        /// Static method to show the settings window as a dialog
        /// </summary>
        /// <param name="owner">The parent window</param>
        /// <returns>Dialog result</returns>
        public static bool? ShowSettingsDialog(Window? owner = null)
        {
            try
            {
                var settingsWindow = new SettingsWindow
                {
                    Owner = owner,
                    WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
                };

                return settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                // Log error if possible
                var logger = App.ServiceProvider?.GetService(typeof(ILogger<SettingsWindow>)) as ILogger<SettingsWindow>;
                logger?.LogError(ex, "Failed to show SettingsWindow dialog");
                
                MessageBox.Show(
                    "Failed to open settings window. Please check the application logs for more details.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                return null;
            }
        }

        /// <summary>
        /// Handle keyboard shortcuts
        /// </summary>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            try
            {
                // Handle Escape key to close
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                    e.Handled = true;
                }
                // Handle Ctrl+S to save
                else if (e.Key == System.Windows.Input.Key.S && 
                        (e.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                {
                    if (DataContext is SettingsWindowViewModel viewModel)
                    {
                        viewModel.ApplySettings();
                    }
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling keyboard shortcut");
            }
        }
    }
}