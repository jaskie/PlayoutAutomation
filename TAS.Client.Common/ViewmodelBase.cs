//#undef DEBUG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace TAS.Client.Common
{
    /// <summary>
    /// Base class for all ViewModel classes in the application.
    /// It provides support for property change notifications 
    /// and has a DisplayName property.  This class is abstract.
    /// </summary>
    public abstract class ViewmodelBase : INotifyPropertyChanged, IDisposable
    {

        private bool _disposed;
        private bool _isModified;


        #region Constructor / Destructor

#if DEBUG
        /// <summary>
        /// Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~ViewmodelBase()
        {
            Debug.WriteLine($"{GetType().Name} ({this}) ({GetHashCode()}) Finalized");
        }
#endif

        #endregion // Constructor / Destructor

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (!string.IsNullOrEmpty(propertyName))
                VerifyPropertyName(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                OnDispose();
            }
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected abstract void OnDispose();

        #endregion // IDisposable Members

        protected virtual void OnModified()
        {
            Modified?.Invoke(this, EventArgs.Empty);
            InvalidateRequerySuggested();
            NotifyPropertyChanged(nameof(IsModified));
        }

        public virtual bool IsModified
        {
            get { return _isModified; }
            protected set
            {
                if (_isModified == value)
                    return;
                _isModified = value;
                if (value)
                    OnModified();
            }
        }

        public event EventHandler Modified;

        protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null, bool setIsModified = true)
        {
            VerifyPropertyName(propertyName);
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            if (setIsModified)
                IsModified = true;
            return true;
        }

        protected virtual void InvalidateRequerySuggested()
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)CommandManager.InvalidateRequerySuggested);
        }
    }
}