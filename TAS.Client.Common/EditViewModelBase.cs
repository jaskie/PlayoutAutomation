using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace TAS.Client.Common
{
    public abstract class EditViewmodelBase<M> : ViewmodelBase 
    {
        public readonly M Model;
        public readonly UserControl _editor;

        protected EditViewmodelBase(M model, UserControl editor)
        {
            Model = model;
            _editor = editor;
            ModelLoad();
            if (editor != null)
                editor.DataContext = this;
        }

        protected virtual void ModelLoad(object source = null)
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

        public virtual void ModelUpdate(object destObject = null)
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
        public UserControl Editor => _editor;

        public override string ToString()
        {
            return Model.ToString();
        }

        protected override void OnDispose()
        {
            if (_editor != null)
                _editor.DataContext = null;
        }

    }
}
