using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace TAS.Common
{
    public class UICommand : ICommand
    {
        private bool _handleExceptions = true;
        public Predicate<object> CanExecuteDelegate { get; set; }
        public Action<object> ExecuteDelegate { get; set; }
        public bool HandleExceptions { get { return _handleExceptions; } set { _handleExceptions = value; } }

        #region ICommand Members

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
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (ExecuteDelegate != null)
            {
                if (HandleExceptions)
                    try
                    {
                        ExecuteDelegate(parameter);
                    }
                catch (Exception e)
                    {
                        Debug.WriteLine(e);
                        System.Windows.MessageBox.Show(string.Format(TAS.Client.Properties.Resources._message_CommandFailed, e.Message), TAS.Client.Properties.Resources._caption_Error, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                else
                ExecuteDelegate(parameter);
            }
        }

        #endregion
    }
}
