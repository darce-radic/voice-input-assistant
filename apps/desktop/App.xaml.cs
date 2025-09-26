using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Unity;
using Serilog;
using System.IO;
using System.Reflection;
using System.Windows;
using Unity;
using VoiceInputAssistant.Interfaces;
using VoiceInputAssistant.Services;
using VoiceInputAssistant.Services.Interfaces;
using VoiceInputAssistant.ViewModels;
using VoiceInputAssistant.Views;
// Register Core service aliases to avoid ambiguity
using IHotkeySvc = VoiceInputAssistant.Core.Services.Interfaces.IHotkeyService;
using ISettingsSvc = VoiceInputAssistant.Core.Services.Interfaces.ISettingsService;
using IAudioSvc = VoiceInputAssistant.Core.Services.Interfaces.IAudioDeviceService;

namespace VoiceInputAssistant;

/// <summary>
/// Application entry point with Prism bootstrapping and dependency injection
/// </summary>
public partial class App : PrismApplication
{
    private IHost? _host;
    
    /// <summary>
    /// Provides access to the application's service provider for dependency injection
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Configure Serilog
        ConfigureSerilog();

        // Build host with configuration and services
        _host = CreateHostBuilder(e.Args).Build();
        
        // Set static service provider reference
        ServiceProvider = _host.Services;
        
        // Set up global exception handling
        SetupGlobalErrorHandling();

        base.OnStartup(e);

        Log.Information("Voice Input Assistant started");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Voice Input Assistant shutting down");
        Log.CloseAndFlush();
        
        _host?.Dispose();
        base.OnExit(e);
    }

    protected override Window CreateShell()
    {
        // Start minimized to system tray
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register views
        containerRegistry.RegisterForNavigation<MainWindow, MainWindowViewModel>();
        containerRegistry.RegisterForNavigation<SettingsWindow, SettingsWindowViewModel>();
        containerRegistry.RegisterForNavigation<HistoryWindow, HistoryViewModel>();

        // Register services
        containerRegistry.RegisterSingleton<ISpeechRecognitionService, SpeechRecognitionService>();
        containerRegistry.RegisterSingleton<ITextInsertionService, TextInsertionService>();
        containerRegistry.RegisterSingleton<IErrorHandlingService, ErrorHandlingService>();
        // Create Desktop service instances
        var hotkeyService = new HotkeyService(Container.Resolve<ILogger<HotkeyService>>());
        var systemTrayService = new SystemTrayService(Container.Resolve<ILogger<SystemTrayService>>());
        var dataStorageService = new DataStorageService(Container.Resolve<ILogger<DataStorageService>>());
        var settingsService = new SettingsService(Container.Resolve<ILogger<SettingsService>>(), dataStorageService);
        var audioDeviceService = new AudioDeviceService(Container.Resolve<ILogger<AudioDeviceService>>());
        
        // Register Desktop interfaces
        containerRegistry.RegisterInstance<IHotkeyService>(hotkeyService);
        containerRegistry.RegisterInstance<ISystemTrayService>(systemTrayService);
        containerRegistry.RegisterInstance<IDataStorageService>(dataStorageService);
        containerRegistry.RegisterInstance<ISettingsService>(settingsService);
        containerRegistry.RegisterInstance<IAudioDeviceService>(audioDeviceService);
        
        // Register Core adapters
        containerRegistry.RegisterInstance<IHotkeySvc>(new HotkeyServiceAdapter(hotkeyService));
        containerRegistry.RegisterInstance<ISettingsSvc>(new SettingsServiceAdapter(settingsService));
        containerRegistry.RegisterInstance<IAudioSvc>(new AudioDeviceServiceAdapter(audioDeviceService));
        containerRegistry.RegisterSingleton<IAudioProcessingService, AudioProcessingService>();
        containerRegistry.RegisterSingleton<IPostProcessingService, PostProcessingService>();
        containerRegistry.RegisterSingleton<IUsageAnalyticsService, UsageAnalyticsService>();
        containerRegistry.RegisterSingleton<IApplicationProfileService, ApplicationProfileService>();

        // Register host services
        if (_host != null)
        {
            var serviceProvider = _host.Services;
            containerRegistry.RegisterInstance<IServiceProvider>(serviceProvider);
            containerRegistry.RegisterInstance(serviceProvider.GetRequiredService<IConfiguration>());
            containerRegistry.RegisterInstance(serviceProvider.GetRequiredService<ILogger<App>>());
        }
    }

    private static void ConfigureSerilog()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoiceInputAssistant",
            "logs",
            "app-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024)
            .CreateLogger();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                config.SetBasePath(basePath)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure application settings
                services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));
                services.Configure<SpeechSettings>(context.Configuration.GetSection("SpeechSettings"));
                services.Configure<PrivacySettings>(context.Configuration.GetSection("PrivacySettings"));
            });

    /// <summary>
    /// Sets up global exception handling for unhandled exceptions
    /// </summary>
    private void SetupGlobalErrorHandling()
    {
        // Handle unhandled exceptions in the current AppDomain
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        // Handle unhandled exceptions in WPF dispatcher threads
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // Handle unhandled exceptions in Task threads
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }
    
    /// <summary>
    /// Handles unhandled exceptions from the AppDomain
    /// </summary>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("Unknown critical error occurred");
            Log.Fatal(exception, "Unhandled exception in AppDomain - Application terminating: {IsTerminating}", e.IsTerminating);
            
            if (ServiceProvider != null)
            {
                var errorService = ServiceProvider.GetService(typeof(IErrorHandlingService)) as IErrorHandlingService;
                if (errorService != null)
                {
                    // Handle synchronously since the app may be terminating
                    var task = errorService.HandleExceptionAsync(exception, "Application Domain", showErrorDialog: e.IsTerminating);
                    task.Wait(TimeSpan.FromSeconds(5)); // Don't wait too long if app is terminating
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to handle unhandled exception");
        }
    }
    
    /// <summary>
    /// Handles unhandled exceptions from WPF dispatcher
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Log.Error(e.Exception, "Unhandled exception in WPF Dispatcher");
            
            if (ServiceProvider != null)
            {
                var errorService = ServiceProvider.GetService(typeof(IErrorHandlingService)) as IErrorHandlingService;
                if (errorService != null)
                {
                    var task = errorService.HandleExceptionAsync(e.Exception, "WPF Dispatcher", showErrorDialog: true);
                    var shouldContinue = task.GetAwaiter().GetResult();
                    
                    if (shouldContinue)
                    {
                        e.Handled = true; // Prevent application shutdown
                    }
                }
                else
                {
                    // Fallback without error service
                    e.Handled = !(e.Exception is OutOfMemoryException || e.Exception is StackOverflowException);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to handle dispatcher unhandled exception");
        }
    }
    
    /// <summary>
    /// Handles unobserved task exceptions
    /// </summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            Log.Error(e.Exception, "Unobserved task exception");
            
            if (ServiceProvider != null)
            {
                var errorService = ServiceProvider.GetService(typeof(IErrorHandlingService)) as IErrorHandlingService;
                if (errorService != null)
                {
                    // Handle the flattened exception
                    var task = errorService.HandleExceptionAsync(e.Exception.GetBaseException(), "Background Task", showErrorDialog: false);
                    task.ConfigureAwait(false);
                }
            }
            
            e.SetObserved(); // Mark as observed to prevent app termination
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to handle unobserved task exception");
        }
    }
}

// Configuration models
public class AppSettings
{
    public string ApplicationName { get; set; } = "Voice Input Assistant";
    public string Version { get; set; } = "0.1.0";
    public bool StartMinimized { get; set; } = true;
    public bool AutoStartWithWindows { get; set; } = false;
    public bool CheckForUpdates { get; set; } = true;
    public string UpdateChannel { get; set; } = "Stable";
    public bool EnableTelemetry { get; set; } = true;
    public int DataRetentionDays { get; set; } = 30;
    public int MaxHistoryItems { get; set; } = 1000;
}

public class SpeechSettings
{
    public string DefaultEngine { get; set; } = "WhisperLocal";
    public string DefaultLanguage { get; set; } = "en-US";
    public float ConfidenceThreshold { get; set; } = 0.7f;
    public bool EnableVoiceActivityDetection { get; set; } = true;
    public bool EnableInterimResults { get; set; } = true;
    public int AudioSampleRate { get; set; } = 16000;
    public int AudioChannels { get; set; } = 1;
    public int MaxRecordingLengthSeconds { get; set; } = 300;
    public int SilenceTimeoutSeconds { get; set; } = 2;
}

public class PrivacySettings
{
    public bool EnableCloudSync { get; set; } = false;
    public bool EnableTelemetry { get; set; } = true;
    public bool EnableCrashReporting { get; set; } = true;
    public string DataRetentionPolicy { get; set; } = "Local";
    public int DeleteDataAfterDays { get; set; } = 30;
    public bool EnableAudioLogging { get; set; } = false;
    public bool EnableTranscriptLogging { get; set; } = true;
}