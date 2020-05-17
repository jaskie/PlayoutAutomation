using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;

namespace TAS.Client.Common
{
    /// <summary>
    ///   Contains helper methods for UI
    /// </summary>
    public class UiServices : IWindowManager, IUiStateManager, ICommonDialogManager
    {
        public static IWindowManager WindowManager { get; private set; } = new UiServices();
        public static IUiStateManager UiStateManager { get; private set; } = new UiServices();
        public static ICommonDialogManager CommonDialogManager { get; private set; } = new UiServices();
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
        /// <param name="title">Title of window</param>                         
        public void ShowWindow(ViewModelBase content, string title)
        {
            if (!FindView(content))            
                throw new ViewNotFoundException(content);

            var window = _windows.FirstOrDefault(w => w.Content == content);
            if (window != null)
            {
                window.Activate();
                return;
            }
                
            window = CreateWindow(title, content);
            window.Show();            
        }

        /// <summary>
        /// Create window dialog
        /// </summary>
        /// <param name="content">ViewModel which will be assigned as content</param>
        /// <param name="title">Title of window</param>  
        /// <returns>Window's DialogResult</returns>
        public bool? ShowDialog(ViewModelBase content, string title)
        {
            if (!FindView(content))
                throw new ViewNotFoundException(content);

            var window = CreateWindow(title, content);
            window.ShowDialog();

            if (window.Content is DialogViewModel dialogVm)
                return dialogVm.DialogResult;

            return window.DialogResult;
        }

        /// <summary>
        /// Create or find existing window
        /// </summary>
        /// <param name="content">ViewModel which will be assigned as content</param>
        /// <param name="windowInfo">Window parameters</param>      
        public void ShowWindow(ViewModelBase content, WindowInfo windowInfo = null)
        {
            if (!FindView(content))
                throw new ViewNotFoundException(content);

            var window = _windows.FirstOrDefault(w => w.Content == content);
            if (window != null)
            {
                window.Activate();
                return;
            }

            window = CreateWindow(windowInfo, content);
            window.Show();
        }

        /// <summary>
        /// Create window dialog
        /// </summary>
        /// <param name="content">ViewModel which will be assigned as content</param>
        /// <param name="windowInfo">Window parameters</param>
        /// <returns>Window's DialogResult</returns>
        public bool? ShowDialog(ViewModelBase content, WindowInfo windowInfo = null)
        {
            if (!FindView(content))
                throw new ViewNotFoundException(content);

            var window = CreateWindow(windowInfo, content);
            window.ShowDialog();

            if (window.Content is DialogViewModel dialogVm)
                return dialogVm.DialogResult;

            return window.DialogResult;
        }
        
        private Window CreateWindow(string title, ViewModelBase content)
        {
            var window = new Window();
            window.Title = title;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.Content = content;
            window.ResizeMode = ResizeMode.NoResize;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Owner = Application.Current.MainWindow;

            window.Closed += Window_Closed;
            _windows.Add(window);
            return window;
        }
        private Window CreateWindow(WindowInfo windowInfo, ViewModelBase content)
        {
            var window = new Window();
            window.Title = windowInfo?.Title ?? content.GetType().Name.Replace("ViewModel","");
            window.SizeToContent = windowInfo?.SizeToContent ?? SizeToContent.WidthAndHeight;
            window.Content = content;
            window.ResizeMode = windowInfo?.ResizeMode ?? ResizeMode.NoResize;
            window.ShowInTaskbar = windowInfo?.ShowInTaskbar ?? false;
            window.WindowStartupLocation = windowInfo?.WindowStartupLocation ?? WindowStartupLocation.CenterOwner;
            window.Owner = windowInfo?.Owner ?? Application.Current.MainWindow;

            window.Closed += Window_Closed;
            _windows.Add(window);
            return window;
        }

        private Type FindAccurateType(Type targetType, IEnumerable<Type> types)
        {
            string[] targetNamespaces = targetType.Namespace.Split(',');

            Type accurateType = types.FirstOrDefault();            
            byte accuracy = 0;

            foreach (var type in types)
            {
                var typeNamespaces = type.Namespace.Split(',');
                byte localAccuracy = 0;

                for (int i = targetNamespaces.Count(), j = typeNamespaces.Count(); i <= 0 && j <= 0; --i, --j)
                {
                    if (targetNamespaces[i] == typeNamespaces[j])
                        ++localAccuracy;
                    else
                        break;
                }

                if (localAccuracy > accuracy)
                {
                    accurateType = type;                   
                    accuracy = localAccuracy;
                }
            }
            return accurateType;
        }
        private bool FindView(ViewModelBase viewModel)
        {
            if (Application.Current.Resources.Contains(new DataTemplateKey(viewModel.GetType())))
                return true;

            var vmType = viewModel.GetType();
            var viewTypeName = vmType.Name.Replace("ViewModel", "View");
            var viewTypes = vmType.Assembly.GetTypes().Where(t => (t.IsClass && t.Name == viewTypeName) 
                                                            || t.GetCustomAttribute<DataContextAttribute>(false) != null);

            if (viewTypes.Count() == 0)
                return false;

            if (!Application.Current.Resources.Contains(new DataTemplateKey(viewModel.GetType())))
            {
                if (viewTypes.Count() == 1)
                {
                    AddDataTemplate(viewModel.GetType(), viewTypes.FirstOrDefault());
                }
                else
                {
                    
                    AddDataTemplate(viewModel.GetType(), FindAccurateType(vmType, viewTypes));
                }                
            }
            return true;                        
        }
        
        public string OpenFileDialog()
        {
            var dialog = new OpenFileDialog();                     
            var result = dialog.ShowDialog();
            
            if (result == true)
                return dialog.FileName;
            return null;
        }

        //Fires when window is closed not matter how (only for windows created by this class)
        private void Window_Closed(object sender, EventArgs e)
        {
            if (!(sender is Window window))
                return;
            
            _windows.Remove(window);                                        
        }                

        /// <summary>
        /// Close Window
        /// </summary>
        /// <param name="content">ViewModel which is assigned as content of opened window</param>
        /// <param name="dialogResult">Dialogresult if closed from code</param>
        public void CloseWindow(ViewModelBase content)
        {
            var window = _windows.FirstOrDefault(p => p.Content == content);     
            
            if (window != null)
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

        /// <summary>
        /// Used to create DataTemplate from code
        /// </summary>
        /// <param name="viewModelType">DataType of content</param>
        /// <param name="viewType">Content</param>
        /// <returns></returns>
        public static void AddDataTemplate(Type viewModelType, Type viewType)
        {
            const string xamlTemplate = "<DataTemplate DataType=\"{{x:Type vm:{0}}}\"><v:{1} /></DataTemplate>";
            var xaml = String.Format(xamlTemplate, viewModelType.Name, viewType.Name, viewModelType.Namespace, viewType.Namespace);

            var context = new ParserContext();

            context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
            context.XamlTypeMapper.AddMappingProcessingInstruction("vm", viewModelType.Namespace, viewModelType.Assembly.FullName);
            context.XamlTypeMapper.AddMappingProcessingInstruction("v", viewType.Namespace, viewType.Assembly.FullName);

            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            context.XmlnsDictionary.Add("vm", "vm");
            context.XmlnsDictionary.Add("v", "v");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);

            if (!Application.Current.Resources.Contains(template.DataTemplateKey))
                Application.Current.Resources.Add(template.DataTemplateKey, template);
        }       
    }
}
