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

        public OkCancelViewmodelBase(M model, UserControl editor, string windowTitle):base(model, editor)
        {
            CommandClose = new UICommand() { CanExecuteDelegate = CanClose, ExecuteDelegate = Close };
            CommandApply = new UICommand() { CanExecuteDelegate = o => Modified == true, ExecuteDelegate = o => Save() };
            CommandOK = new UICommand() { CanExecuteDelegate = o => Modified == true, ExecuteDelegate = Ok };
            View = new OkCancelView() { 
                DataContext = this, 
                Title = windowTitle, 
                Owner = System.Windows.Application.Current.MainWindow, 
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner, 
                ShowInTaskbar = false };
        }

        protected virtual void Ok(object o)
        {
            Save();
            View.DialogResult = true;
        }

        public virtual bool? ShowDialog()
        {
            return View.ShowDialog();
        }

        protected virtual void Close(object parameter)
        {
            View.DialogResult = false;
        }
        
        protected virtual bool CanClose(object parameter)
        {
            return true;
        }

        protected override void OnModified()
        {
            NotifyPropertyChanged("CommandApply");
            NotifyPropertyChanged("CommandOK");
            NotifyPropertyChanged("CommandClose");
        }


        public ICommand CommandClose { get; protected set; }
        public ICommand CommandApply { get; protected set; }
        public ICommand CommandOK { get; protected set; }

       
    }
}
