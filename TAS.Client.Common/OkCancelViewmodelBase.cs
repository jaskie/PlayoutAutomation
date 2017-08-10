using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TAS.Client.Common
{
    public abstract class OkCancelViewmodelBase<TM> : EditViewmodelBase<TM>
    {
        public delegate bool OnOkDelegate(object parameter);
        private bool? _showResult;
        private OkCancelView _currentWindow;
        private string _title;

        protected OkCancelViewmodelBase(TM model, Type editor, string windowTitle) : base(model)
        {
            CommandCancel = new UICommand { CanExecuteDelegate = CanClose, ExecuteDelegate = Close };
            CommandOk = new UICommand { CanExecuteDelegate = CanOk, ExecuteDelegate = Ok };
            Title = windowTitle;
            Editor = (UserControl)Activator.CreateInstance(editor);
        }

        public string Title { get { return _title; } set { SetField(ref _title, value, setIsModified: false); } }

        public UserControl Editor { get; }

        public bool OkCancelButtonsActivateViaKeyboard { get; set; } = true;

        protected virtual void Ok(object o)
        {
            Update();
            _currentWindow.DialogResult = true;
        }

        public virtual bool? ShowDialog()
        {
            _currentWindow = new OkCancelView
            {
                DataContext = this,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                MaxHeight = SystemParameters.PrimaryScreenHeight,
                MaxWidth = SystemParameters.PrimaryScreenWidth,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };
            _showResult = _currentWindow.ShowDialog();
            _currentWindow = null;
            if (_showResult == false)
                Load();
            return _showResult;
        }

        public virtual MessageBoxResult ShowMessage(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            if (_currentWindow == null)
                return MessageBox.Show(messageBoxText, caption, button, icon);
            return MessageBox.Show(_currentWindow, messageBoxText, caption, button, icon);
        }

        public bool? ShowResult => _showResult;

        protected virtual void Close(object parameter)
        {
            _currentWindow.DialogResult = false;
        }
        
        protected virtual bool CanClose(object parameter)
        {
            return true;
        }

        protected virtual bool CanOk(object parameter)
        {
            return IsModified && OnOk?.Invoke(this) != false;
        }

        protected virtual bool CanApply(object parameter)
        {
            return IsModified;
        }

        public ICommand CommandCancel { get; protected set; }
        public ICommand CommandOk { get; protected set; }

        public event OnOkDelegate OnOk;
    }

    public abstract class OkCancelViewmodelBase : ViewmodelBase
    {
        public delegate bool OnOkDelegate(object parameter);
        private bool? _showResult;
        private OkCancelView _currentWindow;
        private string _title;

        protected OkCancelViewmodelBase(Type editor, string windowTitle)
        {
            CommandCancel = new UICommand { CanExecuteDelegate = CanClose, ExecuteDelegate = Close };
            CommandOk = new UICommand { CanExecuteDelegate = CanOk, ExecuteDelegate = Ok };
            Title = windowTitle;
            Editor = (UserControl)Activator.CreateInstance(editor);
        }

        public string Title { get { return _title; } set { SetField(ref _title, value, setIsModified: false); } }

        public UserControl Editor { get; }

        public bool OkCancelButtonsActivateViaKeyboard { get; set; } = true;

        protected virtual void Ok(object o)
        {
            _currentWindow.DialogResult = true;
        }

        public virtual bool? ShowDialog()
        {
            _currentWindow = new OkCancelView
            {
                DataContext = this,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                MaxHeight = SystemParameters.PrimaryScreenHeight,
                MaxWidth = SystemParameters.PrimaryScreenWidth,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };
            _showResult = _currentWindow.ShowDialog();
            _currentWindow = null;
            return _showResult;
        }

        public virtual MessageBoxResult ShowMessage(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            if (_currentWindow == null)
                return MessageBox.Show(messageBoxText, caption, button, icon);
            return MessageBox.Show(_currentWindow, messageBoxText, caption, button, icon);
        }

        public bool? ShowResult => _showResult;

        protected virtual void Close(object parameter)
        {
            _currentWindow.DialogResult = false;
        }

        protected virtual bool CanClose(object parameter)
        {
            return true;
        }

        protected virtual bool CanOk(object parameter)
        {
            return IsModified;
        }

        protected virtual bool CanApply(object parameter)
        {
            return IsModified;
        }

        public ICommand CommandCancel { get; protected set; }
        public ICommand CommandOk { get; protected set; }
    }
}
