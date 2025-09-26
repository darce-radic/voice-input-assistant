using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Management;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.Services;

public interface IHealthCheckService
{
    Task<HealthStatus> GetHealthStatusAsync();
    Task<bool> IsHealthyAsync();
    event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
}

public class HealthCheckService : BackgroundService, IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private HealthStatus _currentStatus;
    private readonly object _statusLock = new();
    
    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    public HealthCheckService(ILogger<HealthCheckService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _currentStatus = new HealthStatus();
    }

    public Task<HealthStatus> GetHealthStatusAsync()
    {
        lock (_statusLock)
        {
            return Task.FromResult(_currentStatus);
        }
    }

    public Task<bool> IsHealthyAsync()
    {
        lock (_statusLock)
        {
            return Task.FromResult(_currentStatus.OverallStatus == ServiceStatus.Healthy);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var newStatus = await PerformHealthCheckAsync();
                
                lock (_statusLock)
                {
                    var previousStatus = _currentStatus.OverallStatus;
                    _currentStatus = newStatus;
                    
                    if (previousStatus != newStatus.OverallStatus)
                    {
                        _logger.LogInformation("Health status changed from {PreviousStatus} to {NewStatus}", 
                            previousStatus, newStatus.OverallStatus);
                        
                        HealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs
                        {
                            PreviousStatus = previousStatus,
                            CurrentStatus = newStatus.OverallStatus,
                            HealthStatus = newStatus
                        });
                    }
                }

                // Log metrics
                LoggingService.LogSystemResource("CPU Usage", newStatus.SystemMetrics.CpuUsagePercent, "%");
                LoggingService.LogSystemResource("Memory Usage", newStatus.SystemMetrics.MemoryUsageMB, "MB");
                LoggingService.LogSystemResource("Available Memory", newStatus.SystemMetrics.AvailableMemoryMB, "MB");
                LoggingService.LogSystemResource("Disk Free Space", newStatus.SystemMetrics.DiskFreeSpaceGB, "GB");

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Health check service stopping");
    }

    private async Task<HealthStatus> PerformHealthCheckAsync()
    {
        var status = new HealthStatus
        {
            Timestamp = DateTime.UtcNow,
            SystemMetrics = await GetSystemMetricsAsync(),
            ServiceChecks = new Dictionary<string, ServiceStatus>()
        };

        // Check system resources
        status.ServiceChecks["SystemResources"] = CheckSystemResources(status.SystemMetrics);
        
        // Check audio system
        status.ServiceChecks["AudioSystem"] = await CheckAudioSystemAsync();
        
        // Check file system access
        status.ServiceChecks["FileSystem"] = await CheckFileSystemAccessAsync();
        
        // Check network connectivity (if using cloud services)
        status.ServiceChecks["NetworkConnectivity"] = await CheckNetworkConnectivityAsync();
        
        // Check external API availability
        status.ServiceChecks["ExternalAPIs"] = await CheckExternalAPIsAsync();

        // Determine overall status
        status.OverallStatus = DetermineOverallStatus(status.ServiceChecks);

        return status;
    }

    private async Task<SystemMetrics> GetSystemMetricsAsync()
    {
        var metrics = new SystemMetrics();

        try
        {
            // Get process metrics
            using var process = Process.GetCurrentProcess();
            metrics.MemoryUsageMB = process.WorkingSet64 / 1024 / 1024;
            metrics.CpuUsagePercent = await GetCpuUsageAsync(process);

            // Get system metrics using WMI
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject mo in searcher.Get())
                {
                    var totalMemoryBytes = Convert.ToDouble(mo["TotalVisibleMemorySize"]) * 1024;
                    var availableMemoryBytes = Convert.ToDouble(mo["AvailablePhysicalMemory"]) * 1024;
                    
                    metrics.TotalMemoryMB = totalMemoryBytes / 1024 / 1024;
                    metrics.AvailableMemoryMB = availableMemoryBytes / 1024 / 1024;
                    break;
                }

                // Get disk space
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var drive = new DriveInfo(Path.GetPathRoot(appDataPath) ?? "C:");
                metrics.DiskFreeSpaceGB = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                metrics.DiskTotalSpaceGB = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get system metrics via WMI, using fallback values");
                metrics.TotalMemoryMB = 8192; // Assume 8GB
                metrics.AvailableMemoryMB = 4096; // Assume 4GB available
                metrics.DiskFreeSpaceGB = 50; // Assume 50GB free
            }

            metrics.ThreadCount = process.Threads.Count;
            metrics.HandleCount = process.HandleCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system metrics");
        }

        return metrics;
    }

    private async Task<double> GetCpuUsageAsync(Process process)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            await Task.Delay(500);
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return cpuUsageTotal * 100;
        }
        catch
        {
            return 0;
        }
    }

    private ServiceStatus CheckSystemResources(SystemMetrics metrics)
    {
        var issues = new List<string>();

        // Check memory usage
        if (metrics.MemoryUsageMB > 1024) // More than 1GB
        {
            issues.Add($"High memory usage: {metrics.MemoryUsageMB:F1} MB");
        }

        if (metrics.AvailableMemoryMB < 512) // Less than 512MB available
        {
            issues.Add($"Low available memory: {metrics.AvailableMemoryMB:F1} MB");
        }

        // Check CPU usage
        if (metrics.CpuUsagePercent > 80)
        {
            issues.Add($"High CPU usage: {metrics.CpuUsagePercent:F1}%");
        }

        // Check disk space
        if (metrics.DiskFreeSpaceGB < 1) // Less than 1GB free
        {
            issues.Add($"Low disk space: {metrics.DiskFreeSpaceGB:F1} GB free");
        }

        return issues.Count == 0 ? ServiceStatus.Healthy : 
               issues.Count <= 2 ? ServiceStatus.Degraded : ServiceStatus.Unhealthy;
    }

    private async Task<ServiceStatus> CheckAudioSystemAsync()
    {
        try
        {
            // Check if audio devices are available
            var audioDevices = NAudio.Wave.WaveIn.DeviceCount;
            if (audioDevices == 0)
            {
                return ServiceStatus.Unhealthy;
            }

            // Try to create a WaveIn instance to test audio system
            using var waveIn = new NAudio.Wave.WaveIn();
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            
            return ServiceStatus.Healthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio system check failed");
            return ServiceStatus.Degraded;
        }
    }

    private async Task<ServiceStatus> CheckFileSystemAccessAsync()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var testPath = Path.Combine(appDataPath, "VoiceInputAssistant", "health-check.tmp");
            
            Directory.CreateDirectory(Path.GetDirectoryName(testPath)!);
            
            // Test write access
            await File.WriteAllTextAsync(testPath, "health check");
            
            // Test read access
            var content = await File.ReadAllTextAsync(testPath);
            
            // Cleanup
            File.Delete(testPath);
            
            return content == "health check" ? ServiceStatus.Healthy : ServiceStatus.Degraded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "File system access check failed");
            return ServiceStatus.Unhealthy;
        }
    }

    private async Task<ServiceStatus> CheckNetworkConnectivityAsync()
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await client.GetAsync("https://www.google.com");
            return response.IsSuccessStatusCode ? ServiceStatus.Healthy : ServiceStatus.Degraded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Network connectivity check failed");
            return ServiceStatus.Unhealthy;
        }
    }

    private async Task<ServiceStatus> CheckExternalAPIsAsync()
    {
        var healthyCount = 0;
        var totalCount = 0;

        // Check OpenAI API (if configured)
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            totalCount++;
            if (await CheckOpenAIApiAsync())
            {
                healthyCount++;
            }
        }

        // Check Azure Speech API (if configured)
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY")))
        {
            totalCount++;
            if (await CheckAzureSpeechApiAsync())
            {
                healthyCount++;
            }
        }

        if (totalCount == 0)
        {
            return ServiceStatus.Healthy; // No external APIs configured
        }

        var ratio = (double)healthyCount / totalCount;
        return ratio >= 1.0 ? ServiceStatus.Healthy :
               ratio >= 0.5 ? ServiceStatus.Degraded : ServiceStatus.Unhealthy;
    }

    private async Task<bool> CheckOpenAIApiAsync()
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("Authorization", 
                $"Bearer {Environment.GetEnvironmentVariable("OPENAI_API_KEY")}");
            
            var response = await client.GetAsync("https://api.openai.com/v1/models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckAzureSpeechApiAsync()
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION") ?? "eastus";
            var response = await client.GetAsync($"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issuetoken");
            
            // We expect 401 without proper auth, but 200 means the service is reachable
            return response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                   response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private ServiceStatus DetermineOverallStatus(Dictionary<string, ServiceStatus> checks)
    {
        var unhealthyCount = 0;
        var degradedCount = 0;
        var totalCount = checks.Count;

        foreach (var status in checks.Values)
        {
            switch (status)
            {
                case ServiceStatus.Unhealthy:
                    unhealthyCount++;
                    break;
                case ServiceStatus.Degraded:
                    degradedCount++;
                    break;
            }
        }

        // If any critical service is unhealthy, overall is unhealthy
        if (checks.ContainsKey("SystemResources") && checks["SystemResources"] == ServiceStatus.Unhealthy)
        {
            return ServiceStatus.Unhealthy;
        }

        if (checks.ContainsKey("FileSystem") && checks["FileSystem"] == ServiceStatus.Unhealthy)
        {
            return ServiceStatus.Unhealthy;
        }

        // Otherwise, determine by ratio
        var unhealthyRatio = (double)unhealthyCount / totalCount;
        var degradedRatio = (double)(unhealthyCount + degradedCount) / totalCount;

        return unhealthyRatio > 0.3 ? ServiceStatus.Unhealthy :
               degradedRatio > 0.3 ? ServiceStatus.Degraded : ServiceStatus.Healthy;
    }
}

public class HealthStatusChangedEventArgs : EventArgs
{
    public ServiceStatus PreviousStatus { get; set; }
    public ServiceStatus CurrentStatus { get; set; }
    public HealthStatus HealthStatus { get; set; } = null!;
}