using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.ViewModels;

namespace VoiceInputAssistant.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;
        private readonly ISystemTrayService? _systemTrayService;
        private bool _isClosingToTray;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize logging and services through DI
            var serviceProvider = App.ServiceProvider;
            _logger = serviceProvider?.GetService(typeof(ILogger<MainWindow>)) as ILogger<MainWindow>;
            _systemTrayService = serviceProvider?.GetService(typeof(ISystemTrayService)) as ISystemTrayService;
            
            // Set up global hotkeys
            SetupGlobalHotkeys();
            
            // Subscribe to system tray events
            if (_systemTrayService != null)
            {
                _systemTrayService.ShowMainWindow += OnShowMainWindowRequested;
                _systemTrayService.ExitApplication += OnExitApplicationRequested;
            }
        }

        private void SetupGlobalHotkeys()
        {
            try
            {
                // Register global hotkeys (this would typically be handled by a service)
                KeyGesture escapeGesture = new KeyGesture(Key.Escape);
                InputBinding escapeBinding = new InputBinding(
                    new RelayCommand(() => CancelListening()), escapeGesture);
                InputBindings.Add(escapeBinding);
                
                _logger?.LogInformation("Global hotkeys registered successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to register global hotkeys");
            }
        }

        private void CancelListening()
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.CancelListening();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cancelling listening");
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            try
            {
                if (WindowState == WindowState.Minimized)
                {
                    MinimizeToTray();
                }
                else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
                {
                    ShowInTaskbar = true;
                    _systemTrayService?.HideTrayIcon();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling window state change");
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // Check if we should close to tray instead of fully exiting
                if (!_isClosingToTray && ShouldMinimizeToTray())
                {
                    e.Cancel = true;
                    MinimizeToTray();
                    return;
                }

                // Perform cleanup
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.Shutdown();
                }

                // Unsubscribe from system tray events
                if (_systemTrayService != null)
                {
                    _systemTrayService.ShowMainWindow -= OnShowMainWindowRequested;
                    _systemTrayService.ExitApplication -= OnExitApplicationRequested;
                }

                _logger?.LogInformation("MainWindow closing");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during window closing");
            }
        }

        private bool ShouldMinimizeToTray()
        {
            try
            {
                // Try to get settings from the service provider
                var settingsService = App.ServiceProvider?.GetService(typeof(VoiceInputAssistant.Interfaces.ISettingsService)) 
                    as VoiceInputAssistant.Interfaces.ISettingsService;
                    
                if (settingsService != null)
                {
                    // Since this is a synchronous method but the service is async, 
                    // we'll use a default value and consider making this async in the future
                    // var settings = await settingsService.GetSettingsAsync();
                    // return settings?.MinimizeToTray ?? true;
                    
                    // For now, return default value
                    return true;
                }
                
                // Default to true if settings service is not available
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to retrieve minimize to tray setting, using default");
                return true;
            }
        }

        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            MinimizeToTray();
        }

        private void MinimizeToTray()
        {
            try
            {
                Hide();
                ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
                
                _systemTrayService?.ShowTrayIcon("Voice Input Assistant", "Application minimized to tray");
                
                _logger?.LogDebug("Application minimized to system tray");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to minimize to tray");
            }
        }

        private void OnShowMainWindowRequested(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnExitApplicationRequested(object? sender, EventArgs e)
        {
            _isClosingToTray = false;
            Close();
        }

        public void ShowMainWindow()
        {
            try
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Show();
                ShowInTaskbar = true;
                Activate();
                Focus();
                
                _systemTrayService?.HideTrayIcon();
                
                _logger?.LogDebug("Main window restored from tray");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to show main window");
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Additional initialization that requires the window handle
            try
            {
                // Register for system events, additional hotkeys, etc.
                _logger?.LogDebug("MainWindow source initialized");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during source initialization");
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            
            // Update view model when window is activated
            if (DataContext is MainWindowViewModel viewModel)
            {
                Task.Run(async () => await viewModel.RefreshStatusAsync());
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // Handle global hotkeys when window has focus
            if (e.Key == Key.Escape && DataContext is MainWindowViewModel viewModel)
            {
                viewModel.CancelListening();
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Simple relay command implementation for XAML bindings
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}