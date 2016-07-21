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
    public abstract class OkCancelViewmodelBase<M> : EditViewmodelBase<M>
    {
        private bool? _showResult;
        private OkCancelView _currentWindow;

        public OkCancelViewmodelBase(M model, UserControl editor, string windowTitle):base(model, editor)
        {
            CommandClose = new UICommand() { CanExecuteDelegate = CanClose, ExecuteDelegate = Close };
            CommandApply = new UICommand() { CanExecuteDelegate = CanApply, ExecuteDelegate = o => Save() };
            CommandOK = new UICommand() { CanExecuteDelegate = CanOK, ExecuteDelegate = Ok };
            _title = windowTitle;
        }

        public Window Window { get { return _currentWindow; } }
        
        private string _title;
        public string Title { get { return _title; } set { SetField(ref _title, value, "Title"); } }

        protected virtual void Ok(object o)
        {
            Save();
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
                ShowInTaskbar = false
            };
            _showResult = _currentWindow.ShowDialog();
            return _showResult;
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
            return IsModified == true && (OKCallback == null || OKCallback(this));
        }

        protected virtual bool CanApply(object parameter)
        {
            return IsModified == true;
        }

        protected override void OnModified()
        {
            InvalidateRequerySuggested();
        }


        public ICommand CommandClose { get; protected set; }
        public ICommand CommandApply { get; protected set; }
        public ICommand CommandOK { get; protected set; }

        public Func<object, bool> OKCallback;
    }
}
