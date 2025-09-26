using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Commands;
using VoiceInputAssistant.Models;

namespace VoiceInputAssistant.ViewModels;

/// <summary>
/// ViewModel for the History window
/// </summary>
public class HistoryViewModel : INotifyPropertyChanged
{
    private readonly ILogger<HistoryViewModel> _logger;
    private ObservableCollection<SpeechRecognitionResult> _historyItems;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string _statusMessage = "Ready";

    public HistoryViewModel(ILogger<HistoryViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _historyItems = new ObservableCollection<SpeechRecognitionResult>();
        
        // Initialize commands
        LoadHistoryCommand = new RelayCommand(async () => await LoadHistoryAsync());
        ClearHistoryCommand = new RelayCommand(ClearHistory, () => HistoryItems.Any());
        SearchCommand = new RelayCommand(Search);
        ExportHistoryCommand = new RelayCommand(async () => await ExportHistoryAsync());
        
        _logger.LogDebug("HistoryViewModel initialized");
    }

    #region Properties

    public ObservableCollection<SpeechRecognitionResult> HistoryItems
    {
        get => _historyItems;
        set => SetProperty(ref _historyItems, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    #endregion

    #region Commands

    public ICommand LoadHistoryCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ExportHistoryCommand { get; }

    #endregion

    #region Methods

    private Task LoadHistoryAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading history...";

            // TODO: Load history from service
            // For now, create some sample data
            var sampleHistory = new[]
            {
                new SpeechRecognitionResult 
                { 
                    Text = "Hello world", 
                    Confidence = 0.95f, 
                    Timestamp = DateTime.Now.AddHours(-1) 
                },
                new SpeechRecognitionResult 
                { 
                    Text = "This is a test", 
                    Confidence = 0.87f, 
                    Timestamp = DateTime.Now.AddHours(-2) 
                }
            };

            HistoryItems.Clear();
            foreach (var item in sampleHistory)
            {
                HistoryItems.Add(item);
            }

            StatusMessage = $"Loaded {HistoryItems.Count} items";
            _logger.LogInformation("History loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history");
            StatusMessage = "Failed to load history";
        }
        finally
        {
            IsLoading = false;
        }
        
        return Task.CompletedTask;
    }

    private void ClearHistory()
    {
        try
        {
            HistoryItems.Clear();
            StatusMessage = "History cleared";
            _logger.LogInformation("History cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear history");
        }
    }

    private void Search()
    {
        try
        {
            // TODO: Implement search functionality
            StatusMessage = $"Search for '{SearchText}' not yet implemented";
            _logger.LogDebug("Search requested for: {SearchText}", SearchText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
        }
    }

    private Task ExportHistoryAsync()
    {
        try
        {
            // TODO: Implement history export
            StatusMessage = "Export not yet implemented";
            _logger.LogDebug("History export requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            StatusMessage = "Export failed";
        }
        
        return Task.CompletedTask;
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
        if (Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}