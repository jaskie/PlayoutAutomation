using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace TAS.Client.Common
{
    /// <summary>
    ///   Contains helper methods for UI
    /// </summary>
    public class UiServices : IWindowManager, IUiStateManager
    {
        public static IWindowManager WindowManager { get; private set; } = new UiServices();
        public static IUiStateManager UiStateManager { get; private set; } = new UiServices();
        /// <summary>
        /// Collection containing existing windows
        /// </summary>
        private static readonly HashSet<Window> _windows = new HashSet<Window>();
        
        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        private static bool _isBusy;        

        /// <summary>
        /// Create or find existing window
        /// </summary>
        /// <param name="content">ViewModel which will be assigned as content</param>
        /// <param name="isDialog">Show window as dialog</param>        
        /// <returns>True if window was created or already found, false if window was not created correctly</returns>
        public void ShowWindow(object content, string title)
        {
            if (!(content is ViewModelBase))
                return;

            var _window = _windows.FirstOrDefault(p => p.Content.GetType() == content.GetType());
            if (_window != null)
            {
                _window.Activate();
                return;
            }

            _window = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = content is IOkCancelViewModel ? new OkCancelViewModel(content as IOkCancelViewModel) : content,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };
            
            _window.Closed += Window_Closed;
            _windows.Add(_window);
            
            _window.Show();            
        }
        public bool? ShowDialog(object content, string title)
        {
            if (!(content is ViewModelBase))
                return false;           
            
            var _window = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = content is IOkCancelViewModel ? new OkCancelViewModel(content as IOkCancelViewModel) : content,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            _window.Closed += Window_Closed;
            _windows.Add(_window);

            _window.ShowDialog();

            if (content is OkCancelViewModel okCancelVM)
                return okCancelVM.DialogResult;

            return _window.DialogResult;
        }        

        //public void ShowOkCancelWindow(object content, string title = null)
        //{
        //    if (!content.GetType().IsGenericType || (content.GetType().GetGenericTypeDefinition() != typeof(OkCancelViewmodelBase<>)))
        //        return;

        //    var _window = _windows.FirstOrDefault(p => p.Content.GetType() == content.GetType());
        //    if (_window != null)
        //    {
        //        _window.Activate();
        //        return;
        //    }

        //    _window = new OkCancelView()
        //    {
        //        Title = title,
        //        SizeToContent = SizeToContent.WidthAndHeight,
        //        Content = content,
        //        ResizeMode = ResizeMode.NoResize,
        //        ShowInTaskbar = false,
        //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //        Owner = Application.Current.MainWindow
        //    };

        //    _window.Closed += Window_Closed;
        //    _windows.Add(_window);

        //    _window.Show();
        //}
        //public bool? ShowOkCancelDialog(object content, string title = null)
        //{
        //    if (!content.GetType().IsGenericType || (content.GetType().GetGenericTypeDefinition() != typeof(OkCancelViewmodelBase<>)))
        //        return false;

        //    var _window = new OkCancelView()
        //    {
        //        Title = title,
        //        SizeToContent = SizeToContent.WidthAndHeight,
        //        Content = content,
        //        ResizeMode = ResizeMode.NoResize,
        //        ShowInTaskbar = false,
        //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //        Owner = Application.Current.MainWindow
        //    };

        //    _window.Closed += Window_Closed;
        //    _windows.Add(_window);

        //    _window.ShowDialog();
        //    return _window.DialogResult;
        //}

        //Fires when window is closed not matter how (only for windows created by this class)
        private void Window_Closed(object sender, EventArgs e)
        {
            if (!(sender is Window window))
                return;

            //if (window.Content is ViewModelBase vm)
            //    vm.Dispose();

            _windows.Remove(window);                                        
        }                

        /// <summary>
        /// Close Window
        /// </summary>
        /// <param name="content">ViewModel which is assigned as content of opened window</param>
        /// <param name="dialogResult">Dialogresult if closed from code</param>
        public void CloseWindow(object content)
        {
            var window = _windows.FirstOrDefault(p => p.Content == content);            
            window.Close();           
        }
        /// <summary>
        /// Close Dialog
        /// </summary>
        /// <param name="content">ViewModel which is assigned as content of opened window</param>
        /// <param name="dialogResult">Dialogresult</param>        

        /// <summary>
        /// Use to change default WindowManager
        /// </summary>
        /// <param name="windowManager"></param>
        public void InitWindowManager(IWindowManager windowManager)
        {
            WindowManager = windowManager;
        }

        /// <summary>
        /// Use to change default UiStateManager
        /// </summary>
        /// <param name="uiStateManager"></param>
        public void InitUiStateManager(IUiStateManager uiStateManager)
        {
            UiStateManager = uiStateManager;
        }

        /// <summary>
        /// Shows window with content
        /// </summary>
        /// <typeparam name="TView">type of UserControl class to show content</typeparam>
        /// <param name="viewmodel">DataContext of the view</param>
        [Obsolete]
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
        [Obsolete]
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
        public void SetBusyState(bool busy = true)
        {
            if (busy == _isBusy)
                return;
            _isBusy = busy;
            Application.Current.Dispatcher.BeginInvoke((Action)(() => Mouse.OverrideCursor = busy ? Cursors.Wait : null ));                                
            if (_isBusy)
                new DispatcherTimer(TimeSpan.Zero, DispatcherPriority.ContextIdle, dispatcherTimer_Tick, Application.Current.Dispatcher);
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
