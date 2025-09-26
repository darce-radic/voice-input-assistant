using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace VoiceInputAssistant.ViewModels
{
    /// <summary>
    /// Base class for ViewModels providing common functionality for MVVM pattern
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Fields

        protected readonly ILogger Logger;
        protected readonly Dispatcher Dispatcher;
        protected readonly CancellationTokenSource CancellationTokenSource;
        
        private bool _disposed;

        #endregion

        #region Constructor

        protected BaseViewModel(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Dispatcher = Dispatcher.CurrentDispatcher;
            CancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance has been disposed
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Gets the cancellation token for this view model
        /// </summary>
        protected CancellationToken CancellationToken => CancellationTokenSource.Token;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the property value and raises PropertyChanged if the value changed
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="backingStore">Reference to the backing field</param>
        /// <param name="value">New value for the property</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if the property value changed, false otherwise</returns>
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region UI Thread Helpers

        /// <summary>
        /// Invokes an action on the UI thread asynchronously with proper error handling
        /// </summary>
        /// <param name="action">Action to execute on the UI thread</param>
        /// <returns>Task representing the async operation</returns>
        protected async Task InvokeOnUIThreadAsync(Action action)
        {
            if (_disposed) return;

            try
            {
                if (Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    await Dispatcher.InvokeAsync(action, DispatcherPriority.Normal, CancellationToken);
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("UI thread invocation was cancelled");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error invoking action on UI thread");
            }
        }

        /// <summary>
        /// Invokes an action on the UI thread synchronously
        /// </summary>
        /// <param name="action">Action to execute on the UI thread</param>
        protected void InvokeOnUIThread(Action action)
        {
            if (_disposed) return;

            try
            {
                if (Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error invoking action on UI thread");
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Handles exceptions with logging and optional UI feedback
        /// </summary>
        /// <param name="ex">Exception to handle</param>
        /// <param name="operation">Description of the operation that failed</param>
        /// <param name="showToUser">Whether to show the error to the user</param>
        protected virtual void HandleException(Exception ex, string operation, bool showToUser = false)
        {
            Logger.LogError(ex, "Error during {Operation}", operation);
            
            if (showToUser)
            {
                _ = InvokeOnUIThreadAsync(() => OnErrorOccurred(ex, operation));
            }
        }

        /// <summary>
        /// Called when an error occurs that should be shown to the user
        /// Override in derived classes to provide specific error handling
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="operation">Description of the operation</param>
        protected virtual void OnErrorOccurred(Exception ex, string operation)
        {
            // Default implementation - derived classes should override
            Logger.LogWarning("Error occurred in {Operation} but no user notification implemented", operation);
        }

        #endregion

        #region IDisposable

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
                    CancellationTokenSource?.Cancel();

                    // Allow derived classes to clean up
                    OnDisposing();

                    // Dispose managed resources
                    CancellationTokenSource?.Dispose();

                    Logger?.LogDebug("{ViewModelType} disposed successfully", GetType().Name);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Error during {ViewModelType} disposal", GetType().Name);
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Called during disposal to allow derived classes to clean up resources
        /// </summary>
        protected virtual void OnDisposing()
        {
            // Override in derived classes
        }

        #endregion
    }
}