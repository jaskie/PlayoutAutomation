using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace TAS.Client.Common
{
    /// <summary>
    ///   Contains UI window helpers
    /// </summary>
    public class WindowManager 
    {
        private WindowManager() { }

        public static WindowManager Current { get; } = new WindowManager();

        /// <summary>
        /// Collection containing existing windows
        /// </summary>
        private readonly HashSet<Window> _windows = new HashSet<Window>();

        private readonly Dictionary<Type, Type> _viewTypesCache = new Dictionary<Type, Type>();

        /// <summary>
        /// Create window dialog
        /// </summary>
        /// <param name="content">ViewModel which will be assigned as content</param>
        /// <param name="title">Title of window</param>  
        /// <returns>Window's DialogResult</returns>
        public bool? ShowDialog(ViewModelBase content, string title)
        {
            var window = CreateWindow(content, new WindowInfo { Title = title });
            window.ShowDialog();

            if (window.DataContext is DialogViewModelBase dialogVm)
                return dialogVm.DialogResult;

            return window.DialogResult;
        }

        /// <summary>
        /// Create or find existing window
        /// </summary>
        /// <param name="content">ViewModel which will be assigned as content</param>
        /// <param name="windowInfo">Window parameters</param>      
        public Window ShowWindow(ViewModelBase content, Action onClose = null, WindowInfo windowInfo = null)
        {
            var window = _windows.FirstOrDefault(w => w.Content == content);
            if (window == null)
            {
                EventHandler closeAction = null;
                window = CreateWindow(content, windowInfo);
                closeAction = new EventHandler((o, e) =>
                {
                    window.Closed -= closeAction;
                    onClose?.Invoke();
                });
                window.Closed += closeAction;
                window.Show();
            }
            else
                window.Activate();
            return window;
        }

        /// <summary>
        /// Create window dialog
        /// </summary>
        /// <param name="content">ViewModel which will be assigned as content</param>
        /// <param name="windowInfo">Window parameters</param>
        /// <returns>Window's DialogResult</returns>
        public bool? ShowDialog(ViewModelBase content, WindowInfo windowInfo = null)
        {
            var window = CreateWindow(content, windowInfo);
            window.ShowDialog();

            if (window.DataContext is DialogViewModelBase dialogVm)
                return dialogVm.DialogResult;

            return window.DialogResult;
        }

        private Type GetViewType(Type viewModelType)
        {
            if (_viewTypesCache.TryGetValue(viewModelType, out var viewType))
                return viewType;
            var namespaceParts = viewModelType.FullName.Split(new[] { '.' });
            for (int i = 0; i < namespaceParts.Length; i++)
                if (string.Equals(namespaceParts[i], "ViewModels"))
                    namespaceParts[i] = "Views";
            namespaceParts[namespaceParts.Length - 1] = namespaceParts[namespaceParts.Length - 1].Replace("ViewModel", "View");
            viewType = viewModelType.Assembly.GetType(string.Join(".", namespaceParts), true, false);
            _viewTypesCache[viewModelType] = viewType;
            if (typeof(OkCancelViewModelBase).IsAssignableFrom(viewModelType))
                AddDataTemplate(viewModelType, viewType);
            return viewType;
        }
        
        private Window CreateWindow(ViewModelBase viewModel, WindowInfo windowInfo)
        {
            var viewType = GetViewType(viewModel.GetType());
            Window window = null;
            if (typeof(OkCancelViewModelBase).IsAssignableFrom(viewModel.GetType()))
                window = new Window { Content = new OkCancelView(), SizeToContent = SizeToContent.WidthAndHeight };
            else
            if (typeof(Window).IsAssignableFrom(viewType))
                window = (Window)Activator.CreateInstance(viewType);
            else
            if (typeof(UserControl).IsAssignableFrom(viewType))
                window = new Window { Content = Activator.CreateInstance(viewType), SizeToContent = SizeToContent.WidthAndHeight };
            if (window == null)
                throw new ApplicationException($"Type of view for {viewModel.GetType().FullName} is not Window nor UserControl");
            window.DataContext = viewModel;
            if (windowInfo?.Title != null)
                window.Title = windowInfo.Title;
            if (windowInfo?.SizeToContent != null)
                window.SizeToContent = windowInfo.SizeToContent.Value;
            if (windowInfo?.ResizeMode != null)
                window.ResizeMode = windowInfo.ResizeMode.Value;
            if (windowInfo?.ShowInTaskbar != null)
                window.ShowInTaskbar = windowInfo.ShowInTaskbar.Value;
            if (windowInfo?.WindowStartupLocation != null)
                window.WindowStartupLocation = windowInfo.WindowStartupLocation.Value;
            if (windowInfo?.Owner != null)
                window.Owner = windowInfo.Owner;  
            _windows.Add(window);
            return window;
        }

        /// <summary>
        /// Close Window
        /// </summary>
        /// <param name="content">ViewModel which is assigned as content of opened window</param>
        /// <param name="dialogResult">Dialogresult if closed from code</param>
        public void CloseWindow(ViewModelBase content)
        {
            var window = _windows.FirstOrDefault(p => p.DataContext == content);     
            
            if (window != null)
                window.Close();           
        }
       
        /// <summary>
        /// Used to create DataTemplate from code
        /// </summary>
        /// <param name="viewModelType">DataType of content</param>
        /// <param name="viewType">Content</param>
        /// <returns></returns>
        public void AddDataTemplate(Type viewModelType, Type viewType)
        {
            var xaml = $"<DataTemplate DataType=\"{{x:Type vm:{viewModelType.Name}}}\"><v:{viewType.Name} /></DataTemplate>";
            var context = new ParserContext();
            context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
            context.XamlTypeMapper.AddMappingProcessingInstruction("vm", viewModelType.Namespace, viewModelType.Assembly.FullName);
            context.XamlTypeMapper.AddMappingProcessingInstruction("v", viewType.Namespace, viewType.Assembly.FullName);

            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            context.XmlnsDictionary.Add("vm", "vm");
            context.XmlnsDictionary.Add("v", "v");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);
            if (!Application.Current?.Resources.Contains(template.DataTemplateKey) == true)
                Application.Current?.Resources.Add(template.DataTemplateKey, template);
        }       
    }
}
