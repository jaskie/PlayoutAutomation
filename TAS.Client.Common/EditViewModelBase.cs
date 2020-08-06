using System.Linq;
using System.Reflection;

namespace TAS.Client.Common
{
    public abstract class EditViewModelBase<TM> : ModifyableViewModelBase 
    {
        protected EditViewModelBase(TM model)
        {
            Model = model;
            Load(model);
        }

        public TM Model { get; }

        protected void Load(object source = null)
        {
            IsLoading = true;
            try
            {
                var copiedProperties = GetType().GetProperties().Where(p => p.CanWrite);
                foreach (var copyPi in copiedProperties)
                {
                    var sourcePi = (source ?? Model).GetType().GetProperty(copyPi.Name);
                    if (sourcePi != null)
                        copyPi.SetValue(this, sourcePi.GetValue((source ?? Model), null), null);
                }
            }
            finally
            {
                IsLoading = false;
            }
            IsModified = false;
        }

        protected virtual void Update(object destObject = null)
        {
            if ((!IsModified || Model == null) && destObject == null)
                return;
            var copiedProperties = GetType().GetProperties().Where(p => p.CanRead && p.GetCustomAttribute(typeof(IgnoreOnUpdateAttribute)) == null);
            foreach (var copyPi in copiedProperties)
            {
                var destPi = (destObject ?? Model).GetType().GetProperty(copyPi.Name);
                if (destPi == null)
                    continue;
                var newValue = copyPi.GetValue(this, null);
                var oldValue = destPi.GetValue(destObject ?? Model, null);
                if (((oldValue == null && newValue != null) || (oldValue != null && !oldValue.Equals(newValue)))
                    && destPi.CanWrite)
                    destPi.SetValue(destObject ?? Model, copyPi.GetValue(this, null), null);
            }
            IsModified = false;
        }

        public override string ToString()
        {
            return Model.ToString();
        }

    }
}
