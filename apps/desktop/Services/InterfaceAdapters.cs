using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using CoreInterfaces = VoiceInputAssistant.Core.Services.Interfaces;
using CoreModels = VoiceInputAssistant.Core.Models;
using CoreEvents = VoiceInputAssistant.Core.Events;
using DesktopModels = VoiceInputAssistant.Models;
using DesktopInterfaces = VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Core.Events;
using VoiceInputAssistant.Core.Models;

#pragma warning disable CS0067 // Event is declared but never used
namespace VoiceInputAssistant.Services
{
/// <summary>
/// Adapts Desktop interfaces to Core interfaces and vice versa
/// </summary>
public class SettingsServiceAdapter : CoreInterfaces.ISettingsService
{
    private readonly DesktopInterfaces.ISettingsService _desktopService;

    public SettingsServiceAdapter(DesktopInterfaces.ISettingsService desktopService)
    {
        _desktopService = desktopService ?? throw new ArgumentNullException(nameof(desktopService));
    }

    public event EventHandler<CoreModels.UserSettings>? SettingsChanged;

    public async Task<CoreModels.UserSettings> GetSettingsAsync()
    {
        var settings = await _desktopService.GetSettingAsync<CoreModels.UserSettings>("user_settings");
        return settings ?? new CoreModels.UserSettings();
    }

    public async Task SaveSettingsAsync(CoreModels.UserSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        await _desktopService.SetSettingAsync("user_settings", settings);
        SettingsChanged?.Invoke(this, settings);
    }

    public async Task ResetSettingsAsync()
    {
        await _desktopService.DeleteSettingAsync("user_settings");
        var settings = new CoreModels.UserSettings();
        SettingsChanged?.Invoke(this, settings);
    }

    public async Task<CoreModels.UserSettings> ImportSettingsAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
        var json = await File.ReadAllTextAsync(filePath);
        var settings = System.Text.Json.JsonSerializer.Deserialize<CoreModels.UserSettings>(json);
        if (settings == null) throw new InvalidOperationException("Failed to parse settings file");
        return settings;
    }

    public async Task ExportSettingsAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
        var settings = await GetSettingsAsync();
        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<bool> ValidateSettingsAsync(CoreModels.UserSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        // Implement validation logic
        return true;
    }

    public async Task BackupSettingsAsync()
    {
        var settings = await GetSettingsAsync();
        var backupPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VoiceInputAssistant",
            "Backups",
            $"settings-{DateTime.Now:yyyy-MM-dd-HHmmss}.json");
        
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        await ExportSettingsAsync(backupPath);
    }

}

/// <summary>
/// Adapts Desktop interfaces to Core interfaces and vice versa
/// </summary>
public class HotkeyServiceAdapter : CoreInterfaces.IHotkeyService
{
    private readonly DesktopInterfaces.IHotkeyService _desktopService;

    public HotkeyServiceAdapter(DesktopInterfaces.IHotkeyService desktopService)
    {
        _desktopService = desktopService ?? throw new ArgumentNullException(nameof(desktopService));
    }

    public event EventHandler<CoreInterfaces.HotkeyPressedEventArgs>? HotkeyPressed;
    public event EventHandler<CoreInterfaces.MouseEventArgs>? MouseButtonPressed;
    public event EventHandler<CoreInterfaces.KeyboardEventArgs>? KeyPressed;

    public bool IsInitialized { get; private set; } = true; // Desktop service is always initialized
    public bool IsMonitoring { get; private set; }
    public bool IsPushToTalkPressed { get; private set; }

    public async Task InitializeAsync()
    {
        // Desktop service doesn't have InitializeAsync, so just set flag
        IsInitialized = true;
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        // Desktop service doesn't have ShutdownAsync, so just unregister all hotkeys
        await _desktopService.UnregisterAllHotkeysAsync();
        IsInitialized = false;
    }

    public async Task<bool> RegisterHotkeyAsync(CoreModels.HotkeyConfig hotkey)
    {
        var adapted = AdaptHotkeyConfigToDesktop(hotkey);
        return await _desktopService.RegisterHotkeyAsync(adapted);
    }

    public async Task<bool> UnregisterHotkeyAsync(string hotkeyId)
    {
        // Desktop service expects Guid, so convert string to Guid
        if (Guid.TryParse(hotkeyId, out var guid))
        {
            return await _desktopService.UnregisterHotkeyAsync(guid);
        }
        return false;
    }

    public async Task<IEnumerable<CoreModels.HotkeyConfig>> GetRegisteredHotkeysAsync()
    {
        var hotkeys = await _desktopService.GetRegisteredHotkeysAsync();
        return hotkeys.Select(AdaptHotkeyConfigToCore);
    }

    public async Task<bool> UpdateHotkeyAsync(CoreModels.HotkeyConfig hotkey)
    {
        // Desktop service doesn't have UpdateHotkeyAsync, so we unregister then register
        await UnregisterHotkeyAsync(hotkey.Id);
        return await RegisterHotkeyAsync(hotkey);
    }

    public async Task<bool> IsHotkeyAvailableAsync(CoreInterfaces.ModifierKeys modifiers, CoreInterfaces.VirtualKey key)
    {
        // Convert Core types to Desktop types
        var desktopModifiers = ConvertModifierKeysToDesktop(modifiers);
        var desktopKey = ConvertVirtualKeyToDesktop(key);
        return await _desktopService.IsHotkeyAvailableAsync(desktopModifiers, desktopKey);
    }

    public async Task SetInputMonitoringAsync(bool enabled)
    {
        IsMonitoring = enabled;
        // Desktop service doesn't have SetInputMonitoringAsync, so just track state
        await Task.CompletedTask;
    }

    public async Task SetPushToTalkHotkeyAsync(CoreModels.HotkeyConfig hotkey)
    {
        // Desktop service doesn't have SetPushToTalkHotkeyAsync, so just register as regular hotkey
        await RegisterHotkeyAsync(hotkey);
        IsPushToTalkPressed = false; // Reset state
    }

    public async Task SimulateKeyPressAsync(CoreInterfaces.VirtualKey key, CoreInterfaces.ModifierKeys modifiers = CoreInterfaces.ModifierKeys.None)
    {
        // Desktop service doesn't have SimulateKeyPressAsync, so just track the action
        await Task.CompletedTask;
        // TODO: Implement actual key simulation if needed
    }

    public async Task<CoreInterfaces.WindowInfo> GetFocusedWindowAsync()
    {
        // Desktop service doesn't have GetFocusedWindowAsync, so return a placeholder
        await Task.CompletedTask;
        return new CoreInterfaces.WindowInfo
        {
            Handle = IntPtr.Zero,
            Title = "Unknown",
            ProcessName = "Unknown",
            ClassName = "Unknown",
            ProcessId = 0,
            Bounds = new CoreInterfaces.Rectangle(),
            IsVisible = true,
            IsMinimized = false,
            IsMaximized = false
        };
    }

    public async Task UnregisterAllHotkeysAsync()
    {
        await _desktopService.UnregisterAllHotkeysAsync();
    }

    private static DesktopModels.HotkeyConfiguration AdaptHotkeyConfigToDesktop(CoreModels.HotkeyConfig config)
    {
        var desktopConfig = new DesktopModels.HotkeyConfiguration
        {
            Id = Guid.TryParse(config.Id, out var id) ? id : Guid.NewGuid(),
            DisplayName = config.Name ?? "",
            Description = config.Description ?? "",
            IsEnabled = config.IsEnabled,
            Key = ConvertVirtualKeyToDesktop(config.Key),
            Modifiers = ConvertModifierKeysToDesktop(config.Modifiers)
        };
        return desktopConfig;
    }

    private static CoreModels.HotkeyConfig AdaptHotkeyConfigToCore(DesktopModels.HotkeyConfiguration config)
    {
        return new CoreModels.HotkeyConfig
        {
            Id = config.Id.ToString(),
            Name = config.DisplayName ?? "",
            Description = config.Description ?? "",
            IsEnabled = config.IsEnabled,
            Key = ConvertVirtualKeyToCore(config.Key),
            Modifiers = ConvertModifierKeysToCore(config.Modifiers)
        };
    }

    private static System.Windows.Forms.Keys ConvertVirtualKeyToDesktop(CoreInterfaces.VirtualKey key)
    {
        return (System.Windows.Forms.Keys)(int)key;
    }

    private static CoreInterfaces.VirtualKey ConvertVirtualKeyToCore(System.Windows.Forms.Keys key)
    {
        return (CoreInterfaces.VirtualKey)(int)key;
    }

    private static DesktopModels.KeyModifiers ConvertModifierKeysToDesktop(CoreInterfaces.ModifierKeys modifiers)
    {
        var result = DesktopModels.KeyModifiers.None;
        if (modifiers.HasFlag(CoreInterfaces.ModifierKeys.Alt)) result |= DesktopModels.KeyModifiers.Alt;
        if (modifiers.HasFlag(CoreInterfaces.ModifierKeys.Control)) result |= DesktopModels.KeyModifiers.Control;
        if (modifiers.HasFlag(CoreInterfaces.ModifierKeys.Shift)) result |= DesktopModels.KeyModifiers.Shift;
        if (modifiers.HasFlag(CoreInterfaces.ModifierKeys.Windows)) result |= DesktopModels.KeyModifiers.Windows;
        return result;
    }

    private static CoreInterfaces.ModifierKeys ConvertModifierKeysToCore(DesktopModels.KeyModifiers modifiers)
    {
        var result = CoreInterfaces.ModifierKeys.None;
        if (modifiers.HasFlag(DesktopModels.KeyModifiers.Alt)) result |= CoreInterfaces.ModifierKeys.Alt;
        if (modifiers.HasFlag(DesktopModels.KeyModifiers.Control)) result |= CoreInterfaces.ModifierKeys.Control;
        if (modifiers.HasFlag(DesktopModels.KeyModifiers.Shift)) result |= CoreInterfaces.ModifierKeys.Shift;
        if (modifiers.HasFlag(DesktopModels.KeyModifiers.Windows)) result |= CoreInterfaces.ModifierKeys.Windows;
        return result;
    }
}
}
#pragma warning restore CS0067

/// <summary>
/// Adapts Desktop interfaces to Core interfaces and vice versa
/// </summary>
public class AudioDeviceServiceAdapter : CoreInterfaces.IAudioDeviceService
{
    private readonly DesktopInterfaces.IAudioDeviceService _desktopService;
    private bool _isInitialized;
    private bool _isRecording;

    public AudioDeviceServiceAdapter(DesktopInterfaces.IAudioDeviceService desktopService)
    {
        _desktopService = desktopService ?? throw new ArgumentNullException(nameof(desktopService));
    }

    public bool IsInitialized => _isInitialized;
    public bool IsRecording => _isRecording;

    public event EventHandler<CoreInterfaces.AudioDeviceChangedEventArgs>? DeviceChanged;
    public event EventHandler<CoreEvents.AudioLevelEventArgs>? AudioLevelChanged;
    public event EventHandler<CoreEvents.VoiceActivityEventArgs>? VoiceActivityDetected;

    public async Task InitializeAsync()
    {
        // Desktop service doesn't require initialization
        _isInitialized = true;
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        // Desktop service doesn't require shutdown
        _isInitialized = false;
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<CoreModels.AudioDevice>> GetInputDevicesAsync()
    {
        var devices = await _desktopService.GetInputDevicesAsync();
        return devices.Cast<CoreModels.AudioDevice>();
    }

    public async Task<IEnumerable<CoreModels.AudioDevice>> GetOutputDevicesAsync()
    {
        // Desktop service doesn't support output devices
        return Enumerable.Empty<CoreModels.AudioDevice>();
    }

    public async Task<CoreModels.AudioDevice> GetDefaultInputDeviceAsync()
    {
        var devices = await _desktopService.GetInputDevicesAsync();
        return devices.FirstOrDefault(d => d.IsDefault) ?? throw new InvalidOperationException("No default input device found");
    }

    public async Task<CoreModels.AudioDevice> GetDefaultOutputDeviceAsync()
    {
        // Desktop service doesn't support output devices
        throw new NotSupportedException("Output devices are not supported by the desktop service");
    }

    public async Task SetInputDeviceAsync(CoreModels.AudioDevice device)
    {
        // Desktop service doesn't support setting default devices
        await Task.CompletedTask;
    }

    public async Task<CoreModels.AudioDevice> GetCurrentInputDeviceAsync()
    {
        // Use default input device as current device since desktop doesn't track current
        return await GetDefaultInputDeviceAsync();
    }

    public async Task<bool> TestInputDeviceAsync(CoreModels.AudioDevice device, TimeSpan duration)
    {
        return await _desktopService.TestInputDeviceAsync(device, duration);
    }

    public async Task StartRecordingAsync(CoreInterfaces.AudioRecordingSettings settings)
    {
        // Map settings and start recording
        _isRecording = true;
        // TODO: Implement actual recording with settings
        await Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        _isRecording = false;
    }

    public float GetCurrentAudioLevel()
    {
        // Desktop service doesn't provide audio levels
        return 0;
    }

    public async Task ConfigureVoiceActivityDetectionAsync(CoreInterfaces.VadSettings settings)
    {
        // TODO: Implement VAD configuration
        await Task.CompletedTask;
    }

    public async Task SetVoiceActivityDetectionAsync(bool enabled)
    {
        // TODO: Implement VAD enable/disable
        await Task.CompletedTask;
    }

    public async Task ConfigureAudioPreprocessingAsync(CoreInterfaces.AudioPreprocessingSettings settings)
    {
        // TODO: Implement audio preprocessing configuration
        await Task.CompletedTask;
    }

    public async Task<byte[]> ProcessAudioAsync(byte[] audioData)
    {
        // Implement audio processing
        return audioData;
    }

    public async Task<CoreModels.AudioFormat> GetAudioFormatAsync()
    {
        return new CoreModels.AudioFormat
        {
            SampleRate = 44100,
            Channels = 1,
            BitsPerSample = 16
        };
    }

    public async Task PlayTestToneAsync(float frequency, TimeSpan duration)
    {
        // Implement test tone playback
    }

    // No conversion needed since both use VoiceInputAssistant.Core.Models.AudioDevice
}
