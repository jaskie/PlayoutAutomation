using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TAS.Client.Common
{
    public abstract class EditViewmodelBase<TM> : ViewmodelBase 
    {
        protected EditViewmodelBase(TM model)
        {
            Model = model;
        }

        public  TM Model { get; }

        public virtual void Load(object source = null)
        {
            IEnumerable<PropertyInfo> copiedProperties = GetType().GetProperties().Where(p => p.CanWrite);
            foreach (PropertyInfo copyPi in copiedProperties)
            {
                PropertyInfo sourcePi = (source ?? Model).GetType().GetProperty(copyPi.Name);
                if (sourcePi != null)
                    copyPi.SetValue(this, sourcePi.GetValue((source ?? Model), null), null);
            }
            IsModified = false;
        }

        public virtual void Update(object destObject = null)
        {
            if (IsModified && Model != null
                || destObject != null)
            {
                PropertyInfo[] copiedProperties = GetType().GetProperties();
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

        public override string ToString()
        {
            return Model.ToString();
        }

    }
}
