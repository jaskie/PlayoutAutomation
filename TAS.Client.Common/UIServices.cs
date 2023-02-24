using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace TAS.Client.Common
{
    /// <summary>
    ///   Contains helper methods for UI
    /// </summary>
    public class UiServices 
    {
        private UiServices() { }

        public static UiServices Current { get; } = new UiServices();

        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        /// 
        private static bool _isBusy;

       
        /// <summary>
        /// Sets the busystate to busy or not busy.
        /// </summary>
        /// <param name="busy">if set to <c>true</c> the application is now busy.</param>
        public void SetBusyState(bool busy = true)
        {
            if (busy == _isBusy)
                return;
            _isBusy = busy;
            RootDispatcher.Dispatcher.BeginInvoke((Action)(() => Mouse.OverrideCursor = busy ? Cursors.Wait : null ));
            if (_isBusy)
                new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.ContextIdle, dispatcherTimer_Tick, RootDispatcher.Dispatcher);
        }

        /// <summary>
        /// Handles the Tick event of the dispatcherTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (!(sender is DispatcherTimer dispatcherTimer))
                return;
            SetBusyState(false);
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= dispatcherTimer_Tick;
        }

    }
}
