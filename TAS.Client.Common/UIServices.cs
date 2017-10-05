using System;
using System.Windows;
using System.Windows.Controls;
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
        public static void ShowWindow<TView>(ViewmodelBase viewmodel, string windowTitle, bool disposeVm) where TView: UserControl, new()
        {
            var newWindow = new Window
            {
                Title = windowTitle,
                Owner = Application.Current.MainWindow,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = new TView { DataContext = viewmodel }
            };
            if (disposeVm)
                newWindow.Closed += (sender, args) => viewmodel.Dispose();
            newWindow.Show();
        }

        /// <summary>
        /// Shows modal dialog with content 
        /// </summary>
        /// <typeparam name="TView">type of UserControl class to show content</typeparam>
        /// <param name="viewmodel">DataContext of the view</param>
        public static bool? ShowDialog<TView>(ViewmodelBase viewmodel, string windowTitle, double width, double height)
            where TView : UserControl, new()
        {
            var newWindow = new Window
            {
                Title = windowTitle,
                Width = width,
                Height = height,
                Owner = Application.Current.MainWindow,
                Content = new TView {DataContext = viewmodel}
            };
            if (viewmodel is ICloseable)
                ((ICloseable) viewmodel).ClosedOk += (o, e) => { newWindow.DialogResult = true; };
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
            var dispatcherTimer = sender as DispatcherTimer;
            if (dispatcherTimer == null)
                return;
            SetBusyState(false);
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= dispatcherTimer_Tick;
        }
    }
}
