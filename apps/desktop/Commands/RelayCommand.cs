using System;
using System.Windows.Input;

namespace VoiceInputAssistant.Commands
{
    /// <summary>
    /// Optimized relay command implementation for MVVM pattern with better performance and error handling
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Fields

        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        private readonly string _name;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of RelayCommand
        /// </summary>
        /// <param name="execute">Action to execute when the command is invoked</param>
        /// <param name="canExecute">Function that determines if the command can execute</param>
        /// <param name="name">Optional name for debugging purposes</param>
        /// <exception cref="ArgumentNullException">Thrown when execute is null</exception>
        public RelayCommand(Action execute, Func<bool>? canExecute = null, string name = "")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of this command for debugging purposes
        /// </summary>
        public string Name => _name;

        #endregion

        #region ICommand Implementation

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return _canExecute?.Invoke() ?? true;
            }
            catch
            {
                // If CanExecute throws an exception, assume command cannot execute
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                _execute();
            }
            catch
            {
                // Command execution errors should be handled by the execute action itself
                // This catch prevents unhandled exceptions from crashing the application
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually raises the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Returns a string representation of the command
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(_name) ? base.ToString() ?? "RelayCommand" : $"RelayCommand: {_name}";
        }

        #endregion
    }

    /// <summary>
    /// Generic relay command implementation with parameter support
    /// </summary>
    /// <typeparam name="T">Type of the command parameter</typeparam>
    public class RelayCommand<T> : ICommand
    {
        #region Fields

        private readonly Action<T> _execute;
        private readonly Predicate<T>? _canExecute;
        private readonly string _name;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of RelayCommand with parameter support
        /// </summary>
        /// <param name="execute">Action to execute when the command is invoked</param>
        /// <param name="canExecute">Function that determines if the command can execute</param>
        /// <param name="name">Optional name for debugging purposes</param>
        /// <exception cref="ArgumentNullException">Thrown when execute is null</exception>
        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null, string name = "")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _name = name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of this command for debugging purposes
        /// </summary>
        public string Name => _name;

        #endregion

        #region ICommand Implementation

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                if (_canExecute == null) return true;

                // Handle parameter conversion
                if (parameter is T typedParameter)
                {
                    return _canExecute(typedParameter);
                }

                // If parameter is null and T is nullable, allow it
                if (parameter == null && !typeof(T).IsValueType)
                {
                    return _canExecute(default(T)!);
                }

                // Parameter type mismatch
                return false;
            }
            catch
            {
                // If CanExecute throws an exception, assume command cannot execute
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                T typedParameter;
                
                if (parameter is T directCast)
                {
                    typedParameter = directCast;
                }
                else if (parameter == null && !typeof(T).IsValueType)
                {
                    typedParameter = default(T)!;
                }
                else
                {
                    // Try to convert the parameter
                    try
                    {
                        typedParameter = (T)parameter!;
                    }
                    catch (InvalidCastException)
                    {
                        return; // Cannot convert parameter, don't execute
                    }
                }

                _execute(typedParameter);
            }
            catch
            {
                // Command execution errors should be handled by the execute action itself
                // This catch prevents unhandled exceptions from crashing the application
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually raises the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Returns a string representation of the command
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(_name) ? base.ToString() ?? $"RelayCommand<{typeof(T).Name}>" : $"RelayCommand<{typeof(T).Name}>: {_name}";
        }

        #endregion
    }
}