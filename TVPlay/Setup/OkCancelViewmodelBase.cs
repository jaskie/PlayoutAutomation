using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using TAS.Common;

namespace TAS.Client.Setup
{
    public abstract class OkCancelViewmodelBase<M, V> : ViewModels.ViewmodelBase where V : System.Windows.FrameworkElement, new()
    {
        public readonly M Model;
        public readonly V View;

        public OkCancelViewmodelBase(M model)
        {
            Model = model;
            PropertyInfo[] copiedProperties = this.GetType().GetProperties();
            foreach (PropertyInfo copyPi in copiedProperties)
            {
                PropertyInfo sourcePi = Model.GetType().GetProperty(copyPi.Name);
                if (sourcePi != null)
                    copyPi.SetValue(this, sourcePi.GetValue(Model, null), null);
            }
            _modified = false;
            CommandClose = new SimpleCommand() { CanExecuteDelegate = CanClose, ExecuteDelegate = Close };
            CommandApply = new SimpleCommand() { CanExecuteDelegate = o => Modified == true, ExecuteDelegate = Apply };
            CommandOK = new SimpleCommand() { CanExecuteDelegate = o => Modified == true, ExecuteDelegate = o => { Apply(o); Close(o); } };
            View = new V() { DataContext = this };
        }

        public virtual void Ok()
        {
            Apply(null);
            Close(null);
        }

        protected virtual void Apply(object parameter)
        {
            if (Modified && Model != null)
            {
                PropertyInfo[] copiedProperties = this.GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo destPi = Model.GetType().GetProperty(copyPi.Name);
                    if (destPi != null)
                    {
                        if (destPi.GetValue(Model, null) != copyPi.GetValue(this, null)
                            && destPi.CanWrite)
                            destPi.SetValue(Model, copyPi.GetValue(this, null), null);
                    }
                }
                Modified = false;
            }
        }

        protected abstract void Close(object parameter);
        
        protected virtual bool CanClose(object parameter)
        {
            return true;
        }

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            bool modified = base.SetField<T>(ref field, value, propertyName);
            if (modified) Modified = true;
            return modified;
        }

        protected bool _modified;

        public bool Modified
        {
            get { return _modified; }
            protected set
            {
                if (base.SetField(ref _modified, value, "Modified"))
                {
                    NotifyPropertyChanged("CommandApply");
                    NotifyPropertyChanged("CommandOk");
                    NotifyPropertyChanged("CommandClose");
                }
            }
        }

        public ICommand CommandClose { get; protected set; }
        public ICommand CommandApply { get; protected set; }
        public ICommand CommandOK { get; protected set; }

    }
}
