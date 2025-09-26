using System.ComponentModel;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Interfaces;

namespace VoiceInputAssistant.ViewModels;

/// <summary>
/// Settings ViewModel - alias for SettingsWindowViewModel for compatibility
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsWindowViewModel _settingsWindowViewModel;

    public SettingsViewModel(
        ILogger<SettingsWindowViewModel> logger,
        Core.Services.Interfaces.ISettingsService settingsService,
        Core.Services.Interfaces.IAudioDeviceService audioDeviceService,
        Core.Services.Interfaces.IHotkeyService hotkeyService)
    {
        // Ensure we use Desktop interfaces directly
        _settingsWindowViewModel = new SettingsWindowViewModel(
            logger, settingsService, audioDeviceService, hotkeyService);
        
        // Forward property changed events
        _settingsWindowViewModel.PropertyChanged += (sender, args) => 
            PropertyChanged?.Invoke(this, args);
    }

    // Expose properties from SettingsWindowViewModel
    public Models.UserSettings Settings => _settingsWindowViewModel.Settings;
    public string StatusMessage => _settingsWindowViewModel.StatusMessage;
    public bool HasUnsavedChanges => _settingsWindowViewModel.HasUnsavedChanges;

    // Commands
    public System.Windows.Input.ICommand ApplySettingsCommand => _settingsWindowViewModel.ApplySettingsCommand;
    public System.Windows.Input.ICommand OkCommand => _settingsWindowViewModel.OkCommand;
    public System.Windows.Input.ICommand CancelCommand => _settingsWindowViewModel.CancelCommand;
    public System.Windows.Input.ICommand TestMicrophoneCommand => _settingsWindowViewModel.TestMicrophoneCommand;
    public System.Windows.Input.ICommand OpenLogFolderCommand => _settingsWindowViewModel.OpenLogFolderCommand;
    public System.Windows.Input.ICommand ExportSettingsCommand => _settingsWindowViewModel.ExportSettingsCommand;
    public System.Windows.Input.ICommand ImportSettingsCommand => _settingsWindowViewModel.ImportSettingsCommand;
    public System.Windows.Input.ICommand ResetSettingsCommand => _settingsWindowViewModel.ResetSettingsCommand;

    public event PropertyChangedEventHandler? PropertyChanged;
}