using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.AvalonDock;

namespace TAS.Client.Common
{
    public abstract class DialogViewModel : ViewModelBase, IWindow
    {
        public event EventHandler Closing;
        public bool DialogResult { get; private set; }
        /// <summary>
        /// Sets dialogresult as true and calls for dialog close
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected void DialogConfirm()
        {
            DialogResult = true;
            Closing?.Invoke(this, EventArgs.Empty);
            UiServices.WindowManager.CloseWindow(this);            
        }
        protected void DialogClose()
        {
            DialogResult = false;
            Closing?.Invoke(this, EventArgs.Empty);
            UiServices.WindowManager.CloseWindow(this);
        }

        protected override void OnDispose()
        {
            //
        }
    }
}
