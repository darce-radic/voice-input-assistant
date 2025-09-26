using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Models;
using VoiceInputAssistant.Services.Interfaces;

namespace VoiceInputAssistant.ViewModels;

/// <summary>
/// Main ViewModel - alias for MainWindowViewModel for compatibility
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public MainViewModel(
        ILogger<MainWindowViewModel> logger,
        ISpeechRecognitionService speechService,
        IApplicationProfileService profileService,
        ISettingsService settingsService,
        IUsageAnalyticsService analyticsService,
        IErrorHandlingService errorHandlingService)
    {
        _mainWindowViewModel = new MainWindowViewModel(
            logger, speechService, profileService, settingsService, analyticsService, errorHandlingService);
        
        // Forward property changed events
        _mainWindowViewModel.PropertyChanged += (sender, args) => 
            PropertyChanged?.Invoke(this, args);
    }

    // Expose all properties and commands from MainWindowViewModel
    public string RecognitionStatus => _mainWindowViewModel.RecognitionStatus;
    public string ActiveEngine => _mainWindowViewModel.ActiveEngine;
    public string ActiveProfile => _mainWindowViewModel.ActiveProfile;
    public string StatusMessage => _mainWindowViewModel.StatusMessage;
    public string Version => _mainWindowViewModel.Version;
    public bool IsListening => _mainWindowViewModel.IsListening;
    public bool IsLoadingHistory => _mainWindowViewModel.IsLoadingHistory;
    public bool HasRecentActivity => _mainWindowViewModel.HasRecentActivity;
    public double AudioLevel => _mainWindowViewModel.AudioLevel;
    public string ListeningButtonText => _mainWindowViewModel.ListeningButtonText;
    public string ListeningIcon => _mainWindowViewModel.ListeningIcon;
    public bool CanToggleListening => _mainWindowViewModel.CanToggleListening;

    // Commands
    public System.Windows.Input.ICommand ToggleListeningCommand => _mainWindowViewModel.ToggleListeningCommand;
    public System.Windows.Input.ICommand TestRecognitionCommand => _mainWindowViewModel.TestRecognitionCommand;
    public System.Windows.Input.ICommand OpenProfilesCommand => _mainWindowViewModel.OpenProfilesCommand;
    public System.Windows.Input.ICommand ViewHistoryCommand => _mainWindowViewModel.ViewHistoryCommand;
    public System.Windows.Input.ICommand OpenSettingsCommand => _mainWindowViewModel.OpenSettingsCommand;
    public System.Windows.Input.ICommand ShowHelpCommand => _mainWindowViewModel.ShowHelpCommand;

    public event PropertyChangedEventHandler? PropertyChanged;
}