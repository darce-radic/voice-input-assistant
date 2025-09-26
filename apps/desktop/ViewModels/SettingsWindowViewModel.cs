using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Commands;
using VoiceInputAssistant.Core.Models;
using VoiceInputAssistant.Core.Services.Interfaces;
using CoreModels = VoiceInputAssistant.Core.Models;
using Models = VoiceInputAssistant.Models;

namespace VoiceInputAssistant.ViewModels
{
    /// <summary>
    /// ViewModel for the settings window
    /// </summary>
    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<SettingsWindowViewModel> _logger;
        private readonly ISettingsService _settingsService;
        private readonly IAudioDeviceService _audioDeviceService;
        private readonly IHotkeyService _hotkeyService;
        
        // Settings model
        private Models.UserSettings _settings = new();
        private Models.UserSettings _originalSettings = new();
        
        // UI state
        private string _statusMessage = "Ready";
        private bool _hasUnsavedChanges;
        private SettingsCategory _selectedCategory = new();
        
        // Collections for dropdowns
        private ObservableCollection<SettingsCategory> _settingsCategories = new();
        private ObservableCollection<string> _availableThemes = new();
        private ObservableCollection<Models.SpeechEngine> _availableEngines = new();
        private ObservableCollection<Models.RecognitionQuality> _availableQualities = new();
        private ObservableCollection<Models.VoiceActivationMode> _availableActivationModes = new();
        private ObservableCollection<Models.PostProcessingMode> _availablePostProcessingModes = new();
        private ObservableCollection<string> _availableLogLevels = new();
        private ObservableCollection<Models.AudioDevice> _availableInputDevices = new();

        // Commands
        public ICommand ApplySettingsCommand { get; private set; } = null!;
        public ICommand OkCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand TestMicrophoneCommand { get; private set; } = null!;
        public ICommand ChangeHotkeyCommand { get; private set; } = null!;
        public ICommand OpenLogFolderCommand { get; private set; } = null!;
        public ICommand ExportSettingsCommand { get; private set; } = null!;
        public ICommand ImportSettingsCommand { get; private set; } = null!;
        public ICommand ResetSettingsCommand { get; private set; } = null!;

        public SettingsWindowViewModel(
            ILogger<SettingsWindowViewModel> logger,
            ISettingsService settingsService,
            IAudioDeviceService audioDeviceService,
            IHotkeyService hotkeyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _audioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));

            InitializeCollections();
            InitializeCommands();
            
            _logger.LogDebug("SettingsWindowViewModel initialized");
        }

        #region Properties

        public Models.UserSettings Settings
        {
            get => _settings;
            set
            {
                if (SetProperty(ref _settings, value))
                {
                    UpdateChangeTracking();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public SettingsCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        // Category visibility properties
        public bool IsGeneralSelected => SelectedCategory?.Id == "General";
        public bool IsSpeechSelected => SelectedCategory?.Id == "Speech";
        public bool IsAudioSelected => SelectedCategory?.Id == "Audio";
        public bool IsHotkeysSelected => SelectedCategory?.Id == "Hotkeys";
        public bool IsAdvancedSelected => SelectedCategory?.Id == "Advanced";

        public bool ShowVadSettings => Settings?.VoiceActivationMode == Models.VoiceActivationMode.VoiceActivated;

        // Collections
        public ObservableCollection<SettingsCategory> SettingsCategories
        {
            get => _settingsCategories;
            set => SetProperty(ref _settingsCategories, value);
        }

        public ObservableCollection<string> AvailableThemes
        {
            get => _availableThemes;
            set => SetProperty(ref _availableThemes, value);
        }

        public ObservableCollection<Models.SpeechEngine> AvailableEngines
        {
            get => _availableEngines;
            set => SetProperty(ref _availableEngines, value);
        }

        public ObservableCollection<Models.RecognitionQuality> AvailableQualities
        {
            get => _availableQualities;
            set => SetProperty(ref _availableQualities, value);
        }

        public ObservableCollection<Models.VoiceActivationMode> AvailableActivationModes
        {
            get => _availableActivationModes;
            set => SetProperty(ref _availableActivationModes, value);
        }

        public ObservableCollection<Models.PostProcessingMode> AvailablePostProcessingModes
        {
            get => _availablePostProcessingModes;
            set => SetProperty(ref _availablePostProcessingModes, value);
        }

        public ObservableCollection<string> AvailableLogLevels
        {
            get => _availableLogLevels;
            set => SetProperty(ref _availableLogLevels, value);
        }

        public ObservableCollection<Models.AudioDevice> AvailableInputDevices
        {
            get => _availableInputDevices;
            set => SetProperty(ref _availableInputDevices, value);
        }

        #endregion

        #region Methods

        private void InitializeCollections()
        {
            // Settings categories
            SettingsCategories = new ObservableCollection<SettingsCategory>
            {
                new SettingsCategory { Id = "General", Title = "General", Icon = "SettingsOutline" },
                new SettingsCategory { Id = "Speech", Title = "Speech Recognition", Icon = "Microphone" },
                new SettingsCategory { Id = "Audio", Title = "Audio", Icon = "VolumeHigh" },
                new SettingsCategory { Id = "Hotkeys", Title = "Hotkeys", Icon = "Keyboard" },
                new SettingsCategory { Id = "Advanced", Title = "Advanced", Icon = "SettingsBox" }
            };

            // Select first category by default
            SelectedCategory = SettingsCategories.FirstOrDefault() ?? SettingsCategories.First();

            // Available themes
            AvailableThemes = new ObservableCollection<string>
            {
                "Light",
                "Dark",
                "Auto"
            };

            // Available engines
            AvailableEngines = new ObservableCollection<Models.SpeechEngine>
            {
                Models.SpeechEngine.WindowsSpeech,
                Models.SpeechEngine.WhisperLocal,
                Models.SpeechEngine.WhisperOpenAI,
                Models.SpeechEngine.AzureSpeech,
                Models.SpeechEngine.GoogleSpeech,
                Models.SpeechEngine.VoskLocal
            };

            // Available qualities
            AvailableQualities = new ObservableCollection<Models.RecognitionQuality>
            {
                Models.RecognitionQuality.Fast,
                Models.RecognitionQuality.Balanced,
                Models.RecognitionQuality.HighAccuracy
            };

            // Available activation modes
            AvailableActivationModes = new ObservableCollection<Models.VoiceActivationMode>
            {
                Models.VoiceActivationMode.PushToTalk,
                Models.VoiceActivationMode.VoiceActivated,
                Models.VoiceActivationMode.ToggleMode,
                Models.VoiceActivationMode.Continuous
            };

            // Available post-processing modes
            AvailablePostProcessingModes = new ObservableCollection<Models.PostProcessingMode>
            {
                Models.PostProcessingMode.None,
                Models.PostProcessingMode.BasicCorrection,
                Models.PostProcessingMode.Advanced,
                Models.PostProcessingMode.Custom
            };

            // Available log levels
            AvailableLogLevels = new ObservableCollection<string>
            {
                "Critical",
                "Error", 
                "Warning",
                "Information",
                "Debug",
                "Trace"
            };

            // Initialize empty audio devices (will be loaded later)
            AvailableInputDevices = new ObservableCollection<Models.AudioDevice>();
        }

        private void InitializeCommands()
        {
            ApplySettingsCommand = new RelayCommand(ApplySettings, () => HasUnsavedChanges);
            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);
            TestMicrophoneCommand = new RelayCommand(async () => await TestMicrophoneAsync());
            ChangeHotkeyCommand = new RelayCommand<string>(ChangeHotkey);
            OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
            ExportSettingsCommand = new RelayCommand(async () => await ExportSettingsAsync());
            ImportSettingsCommand = new RelayCommand(async () => await ImportSettingsAsync());
            ResetSettingsCommand = new RelayCommand(ResetSettings);
        }

        public async Task LoadSettings()
        {
            try
            {
                StatusMessage = "Loading settings...";

                // Load current settings from Core and convert to desktop settings
                var coreSettings = await _settingsService.GetSettingsAsync();
                _originalSettings = Models.ModelConverters.AdaptUserSettings(coreSettings);
                Settings = _originalSettings.Clone(); // Create a copy for editing

                // Load audio devices
                await LoadAudioDevicesAsync();

                StatusMessage = "Settings loaded";
                HasUnsavedChanges = false;

                _logger.LogDebug("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
                StatusMessage = "Failed to load settings";
                
                MessageBox.Show(
                    "Failed to load settings. Using default values.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                // Use default settings
                Settings = new Models.UserSettings();
                _originalSettings = new Models.UserSettings();
            }
        }

        private async Task LoadAudioDevicesAsync()
        {
            try
            {
                var devices = await _audioDeviceService.GetInputDevicesAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableInputDevices.Clear();
                    foreach (var device in devices)
                    {
                        AvailableInputDevices.Add(Models.ModelConverters.ConvertAudioDevice(device));
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load audio devices");
            }
        }

        public void ApplySettings()
        {
            try
            {
                StatusMessage = "Saving settings...";

                // Convert desktop settings to Core settings and save
                Task.Run(async () =>
                {
                    var coreSettings = Models.ModelConverters.AdaptUserSettingsBack(Settings);
                    await _settingsService.SaveSettingsAsync(coreSettings);
                    _originalSettings = Settings.Clone();
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HasUnsavedChanges = false;
                        StatusMessage = "Settings saved successfully";
                    });
                });

                _logger.LogInformation("Settings applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply settings");
                StatusMessage = "Failed to save settings";
                
                MessageBox.Show(
                    "Failed to save settings. Please check the logs for more details.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Ok()
        {
            try
            {
                if (HasUnsavedChanges)
                {
                    ApplySettings();
                }

                // Close the window
                Application.Current.Windows.OfType<Views.SettingsWindow>()
                    .FirstOrDefault()?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OK command");
            }
        }

        private void Cancel()
        {
            try
            {
                if (HasUnsavedChanges)
                {
                    var result = MessageBox.Show(
                        "You have unsaved changes. Are you sure you want to cancel?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                DiscardChanges();
                
                // Close the window
                Application.Current.Windows.OfType<Views.SettingsWindow>()
                    .FirstOrDefault()?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Cancel command");
            }
        }

        public void DiscardChanges()
        {
            try
            {
                Settings = _originalSettings?.Clone() ?? new Models.UserSettings();
                HasUnsavedChanges = false;
                StatusMessage = "Changes discarded";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discard changes");
            }
        }

        private async Task TestMicrophoneAsync()
        {
            try
            {
                StatusMessage = "Testing microphone...";

                // Test the microphone for 3 seconds
                var coreDevice = Settings?.InputDevice != null ? Models.ModelConverters.ConvertAudioDeviceBack(Settings.InputDevice) : null;
                if (coreDevice != null)
                {
                    var isWorking = await _audioDeviceService.TestInputDeviceAsync(coreDevice, TimeSpan.FromSeconds(3));

                    if (isWorking)
                    {
                        StatusMessage = "Microphone test successful";
                        MessageBox.Show(
                            "Microphone is working correctly!",
                            "Test Results",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusMessage = "Microphone test failed";
                        MessageBox.Show(
                            "Microphone test failed. Please check your audio device settings.",
                            "Test Results",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    StatusMessage = "No input device selected";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Microphone test failed");
                StatusMessage = "Microphone test error";
                
                MessageBox.Show(
                    $"Error testing microphone: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ChangeHotkey(string hotkeyType)
        {
            try
            {
                StatusMessage = $"Press new hotkey for {hotkeyType}...";
                
                // This would open a hotkey capture dialog
                _logger.LogDebug("Changing hotkey for {HotkeyType}", hotkeyType);
                StatusMessage = "Hotkey change not yet implemented";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change hotkey for {HotkeyType}", hotkeyType);
            }
        }

        private void OpenLogFolder()
        {
            try
            {
                var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoiceInputAssistant", "Logs");
                
                if (System.IO.Directory.Exists(logPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logPath,
                        UseShellExecute = true
                    });
                    
                    StatusMessage = "Log folder opened";
                }
                else
                {
                    StatusMessage = "Log folder not found";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open log folder");
                StatusMessage = "Failed to open log folder";
            }
        }

        private async Task ExportSettingsAsync()
        {
            try
            {
                StatusMessage = "Exporting settings...";

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Export Settings",
                    FileName = $"VoiceInputAssistant-Settings-{DateTime.Now:yyyy-MM-dd}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _settingsService.ExportSettingsAsync(dialog.FileName);
                    StatusMessage = "Settings exported successfully";
                    
                    MessageBox.Show(
                        "Settings exported successfully!",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export settings");
                StatusMessage = "Export failed";
                
                MessageBox.Show(
                    $"Failed to export settings: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task ImportSettingsAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Import Settings"
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = "Importing settings...";
                    
                    var importedCoreSettings = await _settingsService.ImportSettingsAsync(dialog.FileName);
                    Settings = Models.ModelConverters.AdaptUserSettings(importedCoreSettings);
                    
                    StatusMessage = "Settings imported successfully";
                    
                    MessageBox.Show(
                        "Settings imported successfully! Click Apply to save the changes.",
                        "Import Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import settings");
                StatusMessage = "Import failed";
                
                MessageBox.Show(
                    $"Failed to import settings: {ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetSettings()
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to reset all settings to their default values? This action cannot be undone.",
                    "Reset Settings",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    Settings = new Models.UserSettings();
                    StatusMessage = "Settings reset to defaults";
                    
                    _logger.LogInformation("Settings reset to defaults");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset settings");
                StatusMessage = "Failed to reset settings";
            }
        }

        private void UpdateChangeTracking()
        {
            if (_originalSettings == null) return;
            
            // Simple comparison - in a real app, you might want a more sophisticated approach
            HasUnsavedChanges = !Settings.Equals(_originalSettings);
            
            // Update visibility properties when settings change
            OnPropertyChanged(nameof(ShowVadSettings));
        }

        private void OnCategoryChanged()
        {
            // Update visibility properties when category changes
            OnPropertyChanged(nameof(IsGeneralSelected));
            OnPropertyChanged(nameof(IsSpeechSelected));
            OnPropertyChanged(nameof(IsAudioSelected));
            OnPropertyChanged(nameof(IsHotkeysSelected));
            OnPropertyChanged(nameof(IsAdvancedSelected));
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
            
            // Track changes for settings
            if (propertyName == nameof(Settings))
                UpdateChangeTracking();
            
            // Update category visibility when category changes
            if (propertyName == nameof(SelectedCategory))
                OnCategoryChanged();
                
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Represents a settings category in the navigation
    /// </summary>
    public class SettingsCategory
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}