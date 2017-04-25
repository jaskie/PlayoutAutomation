using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TAS.Client.Common
{
    public delegate bool OnOkDelegate(object parameter);
    public abstract class OkCancelViewmodelBase<M> : EditViewmodelBase<M>
    {
        private bool? _showResult;
        private OkCancelView _currentWindow;

        public OkCancelViewmodelBase(M model, UserControl editor, string windowTitle):base(model, editor)
        {
            CommandClose = new UICommand() { CanExecuteDelegate = CanClose, ExecuteDelegate = Close };
            CommandApply = new UICommand() { CanExecuteDelegate = CanApply, ExecuteDelegate = o => ModelUpdate() };
            CommandOK = new UICommand() { CanExecuteDelegate = CanOK, ExecuteDelegate = Ok };
            _title = windowTitle;
        }

        private string _title;
        public string Title { get { return _title; } set { SetField(ref _title, value); } }

        public bool OkCancelButtonsActivateViaKeyboard { get; set; } = true;

        protected virtual void Ok(object o)
        {
            ModelUpdate();
            _currentWindow.DialogResult = true;
        }

        public virtual bool? ShowDialog()
        {
            _currentWindow = new OkCancelView()
            {
                DataContext = this,
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                MaxHeight = System.Windows.SystemParameters.PrimaryScreenHeight,
                MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };
            _showResult = _currentWindow.ShowDialog();
            _currentWindow = null;
            if (_showResult == false)
                ModelLoad();
            return _showResult;
        }

        public virtual MessageBoxResult ShowMessage(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            if (_currentWindow == null)
                return MessageBox.Show(messageBoxText, caption, button, icon);
            else
                return MessageBox.Show(_currentWindow, messageBoxText, caption, button, icon);
        }

        public bool? ShowResult { get { return _showResult; } }

        protected virtual void Close(object parameter)
        {
            _currentWindow.DialogResult = false;
        }
        
        protected virtual bool CanClose(object parameter)
        {
            return true;
        }

        protected virtual bool CanOK(object parameter)
        {
            return IsModified && OnOk?.Invoke(this) != false;
        }

        protected virtual bool CanApply(object parameter)
        {
            return IsModified;
        }

        public ICommand CommandClose { get; protected set; }
        public ICommand CommandApply { get; protected set; }
        public ICommand CommandOK { get; protected set; }

        public event OnOkDelegate OnOk;
    }
}
