using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Models;
using VoiceInputAssistant.Services.Interfaces;
using VoiceInputAssistant.Extensions;
using VoiceInputAssistant.Commands;
using InterfacesNS = VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Views;

namespace VoiceInputAssistant.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window with optimized performance and maintainability
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Constants
        
        private const int MaxRecentRecognitions = 10;
        private const int StatusMessageTruncateLength = 50;
        private const string DefaultProfileName = "Default";
        private const string DefaultActiveEngine = "None";
        
        // Status messages
        private const string StatusReady = "Ready";
        private const string StatusListening = "Listening";
        private const string StatusError = "Error";
        private const string StatusNotReady = "Not Ready";
        private const string StatusInitializing = "Initializing...";
        
        // Button states
        private const string StartListeningText = "Start Listening";
        private const string StopListeningText = "Stop Listening";
        private const string MicrophoneIcon = "Microphone";
        private const string MicrophoneOffIcon = "MicrophoneOff";
        
        #endregion
        
        #region Static Resources
        
        // Static brushes for better performance
        private static readonly Brush GreenBrush = Brushes.Green;
        private static readonly Brush RedBrush = Brushes.Red;
        private static readonly Brush OrangeBrush = Brushes.Orange;
        private static readonly Brush OrangeRedBrush = Brushes.OrangeRed;
        private static readonly Brush DodgerBlueBrush = Brushes.DodgerBlue;
        
        #endregion
        
        #region Fields
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly InterfacesNS.ISpeechRecognitionService _speechService;
        private readonly InterfacesNS.IApplicationProfileService _profileService;
        private readonly InterfacesNS.ISettingsService _settingsService;
        private readonly InterfacesNS.IUsageAnalyticsService _analyticsService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly Dispatcher _dispatcher;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        private bool _disposed;
        
        // Private fields for properties
        private string _recognitionStatus = StatusReady;
        private string _activeEngine = DefaultActiveEngine;
        private string _activeProfile = DefaultProfileName;
        private string _statusMessage = StatusReady;
        private string _version = "1.0.0";
        private bool _isListening;
        private bool _isLoadingHistory;
        private bool _hasRecentActivity;
        private Brush _statusColor = GreenBrush;
        private double _audioLevel;
        private string _listeningButtonText = StartListeningText;
        private string _listeningIcon = MicrophoneIcon;
        private Brush _listeningButtonColor = DodgerBlueBrush;
        
        // Collections
        private ObservableCollection<SpeechRecognitionResult> _recentRecognitions;
        
        // Commands
        public ICommand ToggleListeningCommand { get; }
        public ICommand TestRecognitionCommand { get; }
        public ICommand OpenProfilesCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            InterfacesNS.ISpeechRecognitionService speechService,
            InterfacesNS.IApplicationProfileService profileService,
            InterfacesNS.ISettingsService settingsService,
            InterfacesNS.IUsageAnalyticsService analyticsService,
            IErrorHandlingService errorHandlingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _dispatcher = Dispatcher.CurrentDispatcher;
            _cancellationTokenSource = new CancellationTokenSource();

            // Initialize collections
            _recentRecognitions = new ObservableCollection<SpeechRecognitionResult>();

            // Initialize commands
            ToggleListeningCommand = new Commands.RelayCommand(async () => await ToggleListeningAsync(), () => CanToggleListening);
            TestRecognitionCommand = new Commands.RelayCommand(async () => await TestRecognitionAsync(), () => !IsListening);
            OpenProfilesCommand = new Commands.RelayCommand(OpenProfiles);
            ViewHistoryCommand = new Commands.RelayCommand(ViewHistory);
            OpenSettingsCommand = new Commands.RelayCommand(OpenSettings);
            ShowHelpCommand = new Commands.RelayCommand(ShowHelp);

            // Subscribe to service events
            _speechService.InterimResultReceived += OnPartialResultReceived;
            _speechService.FinalResultReceived += OnRecognitionCompleted;
            _speechService.ErrorOccurred += OnRecognitionFailed;
            _speechService.VoiceActivityChanged += OnVoiceActivityChanged;

            _profileService.ActiveProfileChanged += OnActiveProfileChanged;
            
            // Initialize data
            Task.Run(async () => await InitializeAsync());
            
            _logger.LogDebug("MainWindowViewModel initialized");
        }

        #endregion

        #region Properties

        public string RecognitionStatus
        {
            get => _recognitionStatus;
            set => SetProperty(ref _recognitionStatus, value);
        }

        public string ActiveEngine
        {
            get => _activeEngine;
            set => SetProperty(ref _activeEngine, value);
        }

        public string ActiveProfile
        {
            get => _activeProfile;
            set => SetProperty(ref _activeProfile, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public bool IsListening
        {
            get => _isListening;
            set
            {
                if (SetProperty(ref _isListening, value))
                {
                    UpdateListeningButtonState();
                    OnPropertyChanged(nameof(CanToggleListening));
                }
            }
        }

        public bool IsLoadingHistory
        {
            get => _isLoadingHistory;
            set => SetProperty(ref _isLoadingHistory, value);
        }

        public bool HasRecentActivity
        {
            get => _hasRecentActivity;
            set => SetProperty(ref _hasRecentActivity, value);
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public double AudioLevel
        {
            get => _audioLevel;
            set => SetProperty(ref _audioLevel, value);
        }

        public string ListeningButtonText
        {
            get => _listeningButtonText;
            set => SetProperty(ref _listeningButtonText, value);
        }

        public string ListeningIcon
        {
            get => _listeningIcon;
            set => SetProperty(ref _listeningIcon, value);
        }

        public Brush ListeningButtonColor
        {
            get => _listeningButtonColor;
            set => SetProperty(ref _listeningButtonColor, value);
        }

        public ObservableCollection<SpeechRecognitionResult> RecentRecognitions
        {
            get => _recentRecognitions;
            set => SetProperty(ref _recentRecognitions, value);
        }

        public bool CanToggleListening => _speechService?.IsInitialized == true;

        #endregion

        #region Methods

        private async Task InitializeAsync()
        {
            try
            {
                // Load version information
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                Version = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

                // Initialize services and load data
                await RefreshStatusAsync();
                await LoadRecentActivityAsync();

                _logger.LogInformation("MainWindowViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MainWindowViewModel");
                StatusMessage = "Initialization failed - see logs for details";
                StatusColor = Brushes.Red;
            }
        }

        public async Task RefreshStatusAsync()
        {
            if (_disposed) return;
            
            try
            {
                // Get all data concurrently for better performance
                var tasks = new List<Task>
                {
                    UpdateRecognitionStatusAsync(),
                    UpdateActiveEngineAsync(),
                    UpdateActiveProfileAsync()
                };
                
                await Task.WhenAll(tasks).ConfigureAwait(false);
                
                await InvokeOnUIThreadAsync(() =>
                {
                    StatusMessage = _speechService.IsInitialized ? StatusReady : StatusInitializing;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh status");
                await InvokeOnUIThreadAsync(() => StatusMessage = "Error refreshing status");
            }
        }
        
        private async Task UpdateRecognitionStatusAsync()
        {
            await InvokeOnUIThreadAsync(() =>
            {
                if (_speechService.IsListening)
                {
                    RecognitionStatus = StatusListening;
                    StatusColor = OrangeBrush;
                }
                else if (_speechService.IsInitialized)
                {
                    RecognitionStatus = StatusReady;
                    StatusColor = GreenBrush;
                }
                else
                {
                    RecognitionStatus = StatusNotReady;
                    StatusColor = RedBrush;
                }
            });
        }
        
        private async Task UpdateActiveEngineAsync()
        {
            await _logger.SafeExecuteAsync(async () =>
            {
                var settings = await _settingsService.GetSettingsAsync().ConfigureAwait(false);
                await InvokeOnUIThreadAsync(() => ActiveEngine = settings.PreferredEngine.ToString());
            }, "Update Active Engine", showErrorDialog: false);
        }
        
        private async Task UpdateActiveProfileAsync()
        {
            await _logger.SafeExecuteAsync(async () =>
            {
                var currentProfile = await _profileService.GetActiveProfileAsync().ConfigureAwait(false);
                await InvokeOnUIThreadAsync(() => ActiveProfile = currentProfile?.Name ?? DefaultProfileName);
            }, "Update Active Profile", showErrorDialog: false);
        }
        
        /// <summary>
        /// Helper method to invoke actions on the UI thread with proper error handling
        /// </summary>
        private async Task InvokeOnUIThreadAsync(Action action)
        {
            if (_disposed) return;
            
            if (_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                await _dispatcher.InvokeAsync(action, DispatcherPriority.Normal, _cancellationTokenSource.Token);
            }
        }

        private async Task LoadRecentActivityAsync()
        {
            if (_disposed) return;
            
            try
            {
                IsLoadingHistory = true;
                
                // Load recent recognition results from analytics service
                var recentResults = await _analyticsService
                    .GetRecentRecognitionsAsync(MaxRecentRecognitions)
                    .ConfigureAwait(false);
                
                await InvokeOnUIThreadAsync(() =>
                {
                    RecentRecognitions.Clear();
                    
                    // Use AddRange-like approach for better performance
                    foreach (var result in recentResults)
                    {
                        RecentRecognitions.Add(result);
                    }
                    
                    HasRecentActivity = RecentRecognitions.Count > 0;
                });
                
                _logger.LogDebug("Loaded {Count} recent recognition results", RecentRecognitions.Count);
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _logger.LogDebug("Load recent activity was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load recent activity");
                await InvokeOnUIThreadAsync(() => StatusMessage = "Failed to load recent activity");
            }
            finally
            {
                IsLoadingHistory = false;
            }
        }

        private async Task ToggleListeningAsync()
        {
            var success = await _logger.SafeExecuteAsync(async () =>
            {
                if (IsListening)
                {
                    await _speechService.StopListeningAsync();
                }
                else
                {
                    await _speechService.StartListeningAsync();
                }
            }, "Toggle Speech Recognition", showErrorDialog: true);
            
            if (!success)
            {
                StatusMessage = "Error toggling recognition";
                StatusColor = RedBrush;
            }
        }

        private async Task TestRecognitionAsync()
        {
            StatusMessage = "Testing recognition...";
            
            var success = await _logger.SafeExecuteAsync(async () =>
            {
                // Start a test recognition session
                await _speechService.StartListeningAsync();
                
                // This will be handled by the recognition events
                _logger.LogInformation("Test recognition started");
            }, "Test Speech Recognition", showErrorDialog: true);
            
            if (!success)
            {
                StatusMessage = "Test failed - check settings";
                StatusColor = RedBrush;
            }
        }

        private void OpenProfiles()
        {
            try
            {
                // Open profiles management window
                _logger.LogDebug("Opening profiles management");
                StatusMessage = "Profiles management not yet implemented";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open profiles");
            }
        }

        private void ViewHistory()
        {
            try
            {
                // Open history viewer
                _logger.LogDebug("Opening history viewer");
                StatusMessage = "History viewer not yet implemented";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open history");
            }
        }

        private async void OpenSettings()
        {
            var success = await _logger.SafeExecute(() =>
            {
                var result = SettingsWindow.ShowSettingsDialog(App.Current.MainWindow);
                if (result == true)
                {
                    // Settings were saved, refresh our data
                    Task.Run(async () => await RefreshStatusAsync());
                }
            }, "Open Settings Window", showErrorDialog: true);
            
            if (!success)
            {
                StatusMessage = "Failed to open settings";
            }
        }

        private void ShowHelp()
        {
            try
            {
                // Show help content
                _logger.LogDebug("Showing help");
                StatusMessage = "Help system not yet implemented";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show help");
            }
        }

        public void CancelListening()
        {
            try
            {
                if (IsListening)
                {
                    _speechService.StopListeningAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel listening");
            }
        }

        public void Shutdown()
        {
            Dispose();
        }
        
        private void UpdateListeningButtonState()
        {
            if (IsListening)
            {
                ListeningButtonText = StopListeningText;
                ListeningIcon = MicrophoneOffIcon;
                ListeningButtonColor = OrangeRedBrush;
            }
            else
            {
                ListeningButtonText = StartListeningText;
                ListeningIcon = MicrophoneIcon;
                ListeningButtonColor = DodgerBlueBrush;
            }
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                try
                {
                    // Cancel any pending operations
                    _cancellationTokenSource?.Cancel();
                    
                    // Unsubscribe from events
                    if (_speechService != null)
                    {
                        _speechService.InterimResultReceived -= OnPartialResultReceived;
                        _speechService.FinalResultReceived -= OnRecognitionCompleted;
                        _speechService.ErrorOccurred -= OnRecognitionFailed;
                        _speechService.VoiceActivityChanged -= OnVoiceActivityChanged;
                    }

                    if (_profileService != null)
                    {
                        _profileService.ActiveProfileChanged -= OnActiveProfileChanged;
                    }
                    
                    // Dispose of disposable resources
                    _cancellationTokenSource?.Dispose();

                    _logger?.LogDebug("MainWindowViewModel disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during MainWindowViewModel disposal");
                }
            }
            
            _disposed = true;
        }
        
        #endregion

        #region Event Handlers


        private void OnRecognitionCompleted(object? sender, InterfacesNS.SpeechRecognitionEventArgs e)
        {
            if (_disposed) return;
            
            _ = InvokeOnUIThreadAsync(() =>
            {
                IsListening = false;
                RecognitionStatus = StatusReady;
                StatusColor = GreenBrush;
                
                var text = e.Result.Text ?? string.Empty;
                var truncatedText = text.Length > StatusMessageTruncateLength 
                    ? text.Substring(0, StatusMessageTruncateLength) + "..." 
                    : text;
                StatusMessage = $"Recognized: {truncatedText}";

                // Add to recent activity with size limit
                RecentRecognitions.Insert(0, e.Result);
                if (RecentRecognitions.Count > MaxRecentRecognitions)
                {
                    RecentRecognitions.RemoveAt(MaxRecentRecognitions);
                }
                
                HasRecentActivity = RecentRecognitions.Count > 0;
                AudioLevel = 0;
            });
        }

        private void OnRecognitionFailed(object? sender, InterfacesNS.SpeechRecognitionErrorEventArgs e)
        {
            if (_disposed) return;
            
            _ = InvokeOnUIThreadAsync(() =>
            {
                IsListening = false;
                RecognitionStatus = StatusError;
                StatusColor = RedBrush;
                StatusMessage = $"Recognition failed: {e.Message}";
                AudioLevel = 0;
            });
        }

        private void OnPartialResultReceived(object? sender, InterfacesNS.SpeechRecognitionEventArgs e)
        {
            if (_disposed) return;
            
            _ = InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = $"Partial: {e.Result.Text}";
            });
        }

        private void OnVoiceActivityChanged(object? sender, InterfacesNS.VoiceActivityEventArgs e)
        {
            if (_disposed) return;
            
            _ = InvokeOnUIThreadAsync(() =>
            {
                AudioLevel = e.EnergyLevel * 100; // Convert to percentage
                // Update listening state based on voice activity
                if (e.HasVoice && !IsListening)
                {
                    IsListening = true;
                    RecognitionStatus = StatusListening;
                    StatusColor = OrangeBrush;
                    StatusMessage = "Voice detected, listening...";
                }
            });
        }

        private void OnActiveProfileChanged(object? sender, ApplicationProfile profile)
        {
            if (_disposed) return;
            
            _ = InvokeOnUIThreadAsync(() =>
            {
                ActiveProfile = profile?.Name ?? DefaultProfileName;
                StatusMessage = $"Switched to profile: {ActiveProfile}";
            });
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
