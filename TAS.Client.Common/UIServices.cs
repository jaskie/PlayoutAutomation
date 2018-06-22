using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TAS.Client.Common
{
    /// <summary>
    ///   Contains helper methods for UI, so far just one for showing a waitcursor
    /// </summary>
    public static class UiServices
    {

        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        private static bool _isBusy;

        /// <summary>
        /// Sets the busystate as busy.
        /// </summary>
        public static void SetBusyState()
        {
            SetBusyState(true);
        }

        /// <summary>
        /// Shows window with content
        /// </summary>
        /// <typeparam name="TView">type of UserControl class to show content</typeparam>
        /// <param name="viewmodel">DataContext of the view</param>
        public static TView ShowWindow<TView>(ViewModelBase viewmodel) where TView: Window, new()
        {
            var newWindow = new TView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewmodel 
            };
            newWindow.Show();
            return newWindow;
        }

        /// <summary>
        /// Shows modal dialog with content 
        /// </summary>
        /// <typeparam name="TView">type of UserControl class to show content</typeparam>
        /// <param name="viewmodel">DataContext of the view</param>
        public static bool? ShowDialog<TView>(ViewModelBase viewmodel)
            where TView : Window, new()
        {
            var newWindow = new TView
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = viewmodel
            };
            return newWindow.ShowDialog();
        }

        /// <summary>
        /// Sets the busystate to busy or not busy.
        /// </summary>
        /// <param name="busy">if set to <c>true</c> the application is now busy.</param>
        private static void SetBusyState(bool busy)
        {
            if (busy == _isBusy)
                return;
            _isBusy = busy;
            Mouse.OverrideCursor = busy ? Cursors.Wait : null;
            if (_isBusy)
                new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.ContextIdle, dispatcherTimer_Tick, Application.Current.Dispatcher);
        }

        /// <summary>
        /// Handles the Tick event of the dispatcherTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (!(sender is DispatcherTimer dispatcherTimer))
                return;
            SetBusyState(false);
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= dispatcherTimer_Tick;
        }
    }
}
