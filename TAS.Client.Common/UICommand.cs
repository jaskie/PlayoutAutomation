using System;
using System.Diagnostics;
using System.Windows.Input;

namespace TAS.Client.Common
{
    public class UiCommand : ICommand
    {
        public UiCommand(Action<object> executeDelegate): this(executeDelegate, null) { }

        public UiCommand(Action<object> executeDelegate, Predicate<object> canExecuteDelegate)
        {
            ExecuteDelegate = executeDelegate;
            CanExecuteDelegate = canExecuteDelegate;
        }

        public Predicate<object> CanExecuteDelegate { get;  }
        public Action<object> ExecuteDelegate { get; }
        public bool HandleExceptions { get; set; } = true;
        public bool CheckBeforeExecute { get; set; } = true;

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            if (CanExecuteDelegate != null)
            {
                if (HandleExceptions)
                {
                    try
                    {
                        return CanExecuteDelegate(parameter);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                else
                    return CanExecuteDelegate(parameter);
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
            if (CheckBeforeExecute && !CanExecute(parameter))
                return;
            if (ExecuteDelegate != null)
            {
                UiServices.Current.SetBusyState();
                if (HandleExceptions)
                    try
                    {
                        ExecuteDelegate(parameter);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        System.Windows.MessageBox.Show(string.Format(Properties.Resources._message_CommandFailed,
#if DEBUG
                            e
#else
                            e.Message
#endif
                            ), Properties.Resources._caption_Error, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                else
                    ExecuteDelegate(parameter);
            }
        }

#endregion
    }
}
