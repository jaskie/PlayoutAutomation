using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;

namespace TAS.Client.Common
{
    public abstract class EditViewmodelBase<M> : ViewModels.ViewmodelBase 
    {
        public readonly M Model;
        public readonly UserControl _editor;
        //public readonly List<string> ModifiedFields = new List<string>();
        public EditViewmodelBase(M model, UserControl editor)
        {
            Model = model;
            _editor = editor;
            Load();
            _isModified = false;
            editor.DataContext = this;
        }

        protected bool _isModified;
        protected virtual void OnModified(){}

        [XmlIgnore]
        public virtual bool IsModified
        {
            get { return _isModified; }
            protected set
            {
                if (base.SetField(ref _isModified, value, nameof(IsModified))
                    && value)
                {
                    OnModified();
                    InvalidateRequerySuggested();
                }
            }
        }

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            bool isModified = base.SetField(ref field, value, propertyName);
            if (isModified)
            {
                IsModified = true;
                //ModifiedFields.Add(propertyName);
            }
            return isModified;
        }

        protected virtual void Load(object source = null)
        {
            IEnumerable<PropertyInfo> copiedProperties = this.GetType().GetProperties().Where(p => p.CanWrite);
            foreach (PropertyInfo copyPi in copiedProperties)
            {
                PropertyInfo sourcePi = (source ?? Model).GetType().GetProperty(copyPi.Name);
                if (sourcePi != null)
                    copyPi.SetValue(this, sourcePi.GetValue((source ?? Model), null), null);
            }
            IsModified = false;
        }

        public virtual void Save(object destObject = null)
        {
            if (IsModified && Model != null
                || destObject != null)
            {
                PropertyInfo[] copiedProperties = this.GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo destPi = (destObject ?? Model).GetType().GetProperty(copyPi.Name);
                    if (destPi != null)
                    {
                        if (destPi.GetValue(destObject ?? Model, null) != copyPi.GetValue(this, null)
                            && destPi.CanWrite)
                            destPi.SetValue(destObject ?? Model, copyPi.GetValue(this, null), null);
                    }
                }
                IsModified = false;
            }
        }

        [XmlIgnore]
        public UserControl Editor { get { return _editor; } }

        public override string ToString()
        {
            return Model.ToString();
        }

    }
}
