using System;
using System.Diagnostics;
using System.Windows.Input;

namespace TAS.Client.Common
{
    public class UiCommand : ICommand
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string _name;
        private readonly Action<object> _executeDelegate;
        private Predicate<object> _canExecuteDelegate;

        public UiCommand(string name, Action<object> executeDelegate) : this(name, executeDelegate, null) { }

        public UiCommand(string name, Action<object> executeDelegate, Predicate<object> canExecuteDelegate)
        {
            _name = name;
            _executeDelegate = executeDelegate;
            _canExecuteDelegate = canExecuteDelegate;
        }


        public bool HandleExceptions { get; set; } = true;
        public bool CheckBeforeExecute { get; set; } = true;

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            if (_canExecuteDelegate != null)
            {
                if (HandleExceptions)
                {
                    try
                    {
                        return _canExecuteDelegate(parameter);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"{_name}: CanExecute thrown an exception");
                    }
                }
                else
                    return _canExecuteDelegate(parameter);
            }
            return true;// if there is no can execute default to true
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object parameter)
        {
            UiServices.SetBusyState();
            if (CheckBeforeExecute && !CanExecute(parameter))
                return;
            if (_executeDelegate != null)
            {
                if (HandleExceptions)
                    try
                    {
                        _executeDelegate(parameter);
                    }
                    catch (Exception e)
                    {
                        HandleException(e);
                    }
                else
                    _executeDelegate(parameter);
            }
        }
        #endregion

        private void HandleException(Exception e)
        {
            Logger.Error(e, $"{_name}: Execute thrown exception");
            System.Windows.MessageBox.Show(string.Format(Properties.Resources._message_CommandFailed,
#if DEBUG
                            e
#else
                            e.Message
#endif
                            ), Properties.Resources._caption_Error, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
