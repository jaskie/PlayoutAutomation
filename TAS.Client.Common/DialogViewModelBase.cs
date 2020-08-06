using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.AvalonDock;

namespace TAS.Client.Common
{
    /// <summary>
    /// DialogViewModelBase class provides basic support for Window structures that are handled by WindowManager in UiServices.
    /// </summary>
    public abstract class DialogViewModelBase : ModifyableViewModelBase, IWindow
    {
        private ViewModelBase _content;

        public DialogViewModelBase()
        {
            _content = this;
        }

        public event EventHandler Closing;
        
        /// <summary>
        /// This value will be returned on DialogClosed in UiServices
        /// </summary>
        public bool DialogResult { get; private set; }

        /// <summary>
        /// Sets dialogresult as true and calls for dialog close
        /// </summary>       
        protected void DialogConfirm()
        {
            DialogResult = true;
            Closing?.Invoke(this, EventArgs.Empty);
            UiServices.WindowManager.CloseWindow(this);            
        }

        /// <summary>
        /// Sets dialogresult as false and calls for dialog close
        /// </summary>  
        protected void DialogClose()
        {
            DialogResult = false;
            Closing?.Invoke(this, EventArgs.Empty);
            UiServices.WindowManager.CloseWindow(this);
        }

        public ViewModelBase Content { get => _content; private set => SetField(ref _content, value); }

        protected override void OnDispose()
        {
            //
        }
    }
}
