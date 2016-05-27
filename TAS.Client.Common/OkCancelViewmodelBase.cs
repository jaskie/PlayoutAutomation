using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace TAS.Client.Common
{
    public abstract class OkCancelViewmodelBase<M> : EditViewmodelBase<M>
    {
        public readonly OkCancelView View;
        private bool? _showResult;

        public OkCancelViewmodelBase(M model, UserControl editor, string windowTitle):base(model, editor)
        {
            CommandClose = new UICommand() { CanExecuteDelegate = CanClose, ExecuteDelegate = Close };
            CommandApply = new UICommand() { CanExecuteDelegate = CanApply, ExecuteDelegate = o => Save() };
            CommandOK = new UICommand() { CanExecuteDelegate = CanOK, ExecuteDelegate = Ok };
            View = new OkCancelView() { 
                DataContext = this, 
                Title = windowTitle, 
                Owner = System.Windows.Application.Current.MainWindow, 
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner, 
                MaxHeight = System.Windows.Application.Current.MainWindow.Height,
                MaxWidth = System.Windows.Application.Current.MainWindow.Width,
                ShowInTaskbar = false };
        }


        protected virtual void Ok(object o)
        {
            Save();
            View.DialogResult = true;
        }

        public virtual bool? ShowDialog()
        {
            _showResult = View.ShowDialog();
            return _showResult;
        }

        public bool? ShowResult { get { return _showResult; } }

        protected virtual void Close(object parameter)
        {
            View.DialogResult = false;
        }
        
        protected virtual bool CanClose(object parameter)
        {
            return true;
        }

        protected virtual bool CanOK(object parameter)
        {
            return Modified == true;
        }

        protected virtual bool CanApply(object parameter)
        {
            return Modified == true;
        }

        protected override void OnModified()
        {
            InvalidateRequerySuggested();
        }


        public ICommand CommandClose { get; protected set; }
        public ICommand CommandApply { get; protected set; }
        public ICommand CommandOK { get; protected set; }

       
    }
}
