using System;
using System.Runtime.CompilerServices;

namespace TAS.Client.Common
{
    public abstract class ModifyableViewModelBase: ViewModelBase
    {
        private bool _isModified;

        protected bool IsLoading;

        public virtual bool IsModified
        {
            get => _isModified;
            set
            {
                if (IsLoading || _isModified == value)
                    return;
                _isModified = value;
                if (value)
                    OnModifiedChanged();
            }
        }

        public event EventHandler ModifiedChanged;

        protected virtual void OnModifiedChanged()
        {
            ModifiedChanged?.Invoke(this, EventArgs.Empty);
            InvalidateRequerySuggested();
            NotifyPropertyChanged(nameof(IsModified));
        }

        protected override bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!base.SetField(ref field, value, propertyName))
                return false;
            IsModified = true;
            return true;
        }
    }
}
