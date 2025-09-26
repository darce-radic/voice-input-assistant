using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services;

public interface IUpdateService
{
    Task<bool> CheckForUpdatesAsync();
    Task<UpdateInfo?> GetUpdateInfoAsync();
    Task<bool> DownloadAndInstallUpdateAsync(Action<int>? progressCallback = null);
    Task RestartApplicationAsync();
    Task<string> GetCurrentVersionAsync();
    Task<ReleaseEntry[]> GetAvailableReleasesAsync();
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    event EventHandler<UpdateDownloadedEventArgs>? UpdateDownloaded;
    event EventHandler<UpdateErrorEventArgs>? UpdateError;
}

public class UpdateService : BackgroundService, IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMetricsService _metricsService;
    
    private UpdateManager? _updateManager;
    private Timer? _updateCheckTimer;
    private readonly object _updateLock = new();
    private UpdateInfo? _lastUpdateInfo;
    private bool _updateInProgress;

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<UpdateDownloadedEventArgs>? UpdateDownloaded;
    public event EventHandler<UpdateErrorEventArgs>? UpdateError;

    public UpdateService(
        ILogger<UpdateService> logger, 
        IConfiguration configuration,
        IMetricsService metricsService)
    {
        _logger = logger;
        _configuration = configuration;
        _metricsService = metricsService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Update service starting");

        try
        {
            await InitializeUpdateManagerAsync();

            // Check for updates on startup
            await CheckForUpdatesAsync();

            // Set up periodic update checks (every 6 hours by default)
            var checkInterval = TimeSpan.FromHours(
                _configuration.GetValue("Updates:CheckIntervalHours", 6));

            _updateCheckTimer = new Timer(
                async _ => await CheckForUpdatesAsync(),
                null,
                checkInterval,
                checkInterval);

            _logger.LogInformation("Update service initialized with check interval: {Interval}", checkInterval);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in update service");
            _metricsService.RecordError("UpdateService", ex.Message);
        }
        finally
        {
            _updateCheckTimer?.Dispose();
            _updateManager?.Dispose();
            _logger.LogInformation("Update service stopped");
        }
    }

    private async Task InitializeUpdateManagerAsync()
    {
        try
        {
            var updateUrl = _configuration.GetValue<string>("Updates:UpdateUrl") ?? 
                           "https://github.com/yourusername/voice-input-assistant/releases";
            
            _updateManager = new UpdateManager(updateUrl);
            _logger.LogInformation("Update manager initialized with URL: {UpdateUrl}", updateUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize update manager");
            throw;
        }
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        if (_updateManager == null)
        {
            _logger.LogWarning("Update manager not initialized");
            return false;
        }

        lock (_updateLock)
        {
            if (_updateInProgress)
            {
                _logger.LogDebug("Update check skipped - update already in progress");
                return false;
            }
        }

        try
        {
            _logger.LogDebug("Checking for updates...");
            var stopwatch = Stopwatch.StartNew();
            
            var updateInfo = await _updateManager.CheckForUpdate();
            
            stopwatch.Stop();
            _metricsService.RecordPerformanceMetric("UpdateCheck", stopwatch.Elapsed);

            if (updateInfo == null)
            {
                _logger.LogDebug("No update information available");
                return false;
            }

            var hasUpdates = updateInfo.ReleasesToApply?.Length > 0;
            
            if (hasUpdates)
            {
                _lastUpdateInfo = updateInfo;
                var latestVersion = updateInfo.ReleasesToApply![^1].Version;
                
                _logger.LogInformation("Update available: {Version} (Current: {CurrentVersion})", 
                    latestVersion, GetCurrentVersionSync());

                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                {
                    UpdateInfo = updateInfo,
                    LatestVersion = latestVersion.ToString(),
                    CurrentVersion = GetCurrentVersionSync(),
                    ReleasesToApply = updateInfo.ReleasesToApply?.Length ?? 0
                });

                _metricsService.RecordUserAction("UpdateAvailable");
            }
            else
            {
                _logger.LogDebug("No updates available");
            }

            return hasUpdates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            _metricsService.RecordError("UpdateService", $"CheckForUpdates: {ex.Message}");
            
            UpdateError?.Invoke(this, new UpdateErrorEventArgs
            {
                Operation = "CheckForUpdates",
                Exception = ex,
                Message = ex.Message
            });
            
            return false;
        }
    }

    public async Task<UpdateInfo?> GetUpdateInfoAsync()
    {
        if (_updateManager == null)
        {
            return null;
        }

        try
        {
            return await _updateManager.CheckForUpdate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting update info");
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(Action<int>? progressCallback = null)
    {
        if (_updateManager == null)
        {
            _logger.LogWarning("Update manager not initialized");
            return false;
        }

        lock (_updateLock)
        {
            if (_updateInProgress)
            {
                _logger.LogWarning("Update already in progress");
                return false;
            }
            _updateInProgress = true;
        }

        try
        {
            _logger.LogInformation("Starting update download and installation...");
            var stopwatch = Stopwatch.StartNew();

            // Get the latest update info
            var updateInfo = _lastUpdateInfo ?? await _updateManager.CheckForUpdate();
            if (updateInfo?.ReleasesToApply == null) 
            {
                _logger.LogWarning("No valid update info available");
                return false;
            }
            if (updateInfo == null || updateInfo.ReleasesToApply?.Length == 0)
            {
                _logger.LogWarning("No updates available to install");
                return false;
            }

            var latestVersion = updateInfo.ReleasesToApply![^1].Version;
            _logger.LogInformation("Downloading update version {Version}...", latestVersion);

            // Download updates with progress reporting
            int lastReportedProgress = 0;
            await _updateManager.DownloadReleases(updateInfo.ReleasesToApply!, progress =>
            {
                var progressPercent = (int)(progress * 100);
                if (progressPercent >= lastReportedProgress + 5) // Report every 5%
                {
                    lastReportedProgress = progressPercent;
                    _logger.LogDebug("Download progress: {Progress}%", progressPercent);
                    progressCallback?.Invoke(progressPercent);
                }
            });

            _logger.LogInformation("Download completed. Installing updates...");
            
            // Apply updates
            var appPath = await _updateManager.ApplyReleases(updateInfo);
            
            stopwatch.Stop();
            _metricsService.RecordPerformanceMetric("UpdateDownloadAndInstall", stopwatch.Elapsed);
            _metricsService.RecordUserAction("UpdateInstalled");

            _logger.LogInformation("Update installed successfully. New app path: {AppPath}", appPath);

            UpdateDownloaded?.Invoke(this, new UpdateDownloadedEventArgs
            {
                UpdateInfo = updateInfo,
                NewVersion = latestVersion.ToString(),
                AppPath = appPath,
                RequiresRestart = true
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading and installing update");
            _metricsService.RecordError("UpdateService", $"DownloadAndInstall: {ex.Message}");
            
            UpdateError?.Invoke(this, new UpdateErrorEventArgs
            {
                Operation = "DownloadAndInstall",
                Exception = ex,
                Message = ex.Message
            });
            
            return false;
        }
        finally
        {
            lock (_updateLock)
            {
                _updateInProgress = false;
            }
        }
    }

    public async Task RestartApplicationAsync()
    {
        try
        {
            _logger.LogInformation("Restarting application for update...");
            _metricsService.RecordUserAction("UpdateRestart");

            if (_updateManager != null)
            {
                UpdateManager.RestartApp();
            }
            else
            {
                // Fallback restart mechanism
                var currentProcess = Process.GetCurrentProcess();
                var startInfo = new ProcessStartInfo
                {
                    FileName = currentProcess.MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Arguments = "--updated"
                };

                Process.Start(startInfo);
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting application");
            _metricsService.RecordError("UpdateService", $"RestartApplication: {ex.Message}");
            throw;
        }
    }

    public Task<string> GetCurrentVersionAsync()
    {
        return Task.FromResult(GetCurrentVersionSync());
    }

    private string GetCurrentVersionSync()
    {
        try
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0";
        }
        catch
        {
            return "0.1.0";
        }
    }

    public async Task<ReleaseEntry[]> GetAvailableReleasesAsync()
    {
        if (_updateManager == null)
        {
            return Array.Empty<ReleaseEntry>();
        }

        try
        {
            var updateInfo = await _updateManager.CheckForUpdate();
            return updateInfo?.ReleasesToApply ?? Array.Empty<ReleaseEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available releases");
            return Array.Empty<ReleaseEntry>();
        }
    }

    public override void Dispose()
    {
        _updateCheckTimer?.Dispose();
        _updateManager?.Dispose();
        base.Dispose();
    }
}

// Event arguments
public class UpdateAvailableEventArgs : EventArgs
{
    public UpdateInfo UpdateInfo { get; set; } = null!;
    public string LatestVersion { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public int ReleasesToApply { get; set; }
}

public class UpdateDownloadedEventArgs : EventArgs
{
    public UpdateInfo UpdateInfo { get; set; } = null!;
    public string NewVersion { get; set; } = string.Empty;
    public string AppPath { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
}

public class UpdateErrorEventArgs : EventArgs
{
    public string Operation { get; set; } = string.Empty;
    public Exception Exception { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}

// Configuration class for update options
public class UpdateOptions
{
    public string UpdateUrl { get; set; } = string.Empty;
    public bool AllowVersionDowngrade { get; set; } = false;
    public bool CheckSignature { get; set; } = true;
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(6);
    public bool AutoInstallUpdates { get; set; } = false;
    public bool NotifyUserOfUpdates { get; set; } = true;
}