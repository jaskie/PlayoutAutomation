using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
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
        private static readonly HashSet<Window> _windows = new HashSet<Window>();        


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
            if (!FindView(content))
                throw new ViewNotFoundException(content);

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
            if (!FindView(content))
                throw new ViewNotFoundException(content);

            var window = CreateWindow(content, windowInfo);
            window.ShowDialog();

            if (window.DataContext is DialogViewModelBase dialogVm)
                return dialogVm.DialogResult;

            return window.DialogResult;
        }
        
        private object GetDialogView(ViewModelBase viewModel)
        {
            DataTemplate dataTemplate = null;
            if ((dataTemplate = (DataTemplate)Application.Current.TryFindResource(new DataTemplateKey(viewModel.GetType().BaseType))) != null)
            {
                return dataTemplate.LoadContent();
            }

            var dialogVmType = viewModel.GetType().BaseType;            
            var viewTypeName = dialogVmType.Name.Replace("ViewModelBase", "View");
            var viewTypes = dialogVmType.Assembly.GetTypes().Where(t => (t.IsClass && t.Name == viewTypeName));

            if (viewTypes.Count() == 0)
                return null;

            if (viewTypes.Count() == 1)
            {
                AddDataTemplate(dialogVmType, viewTypes.FirstOrDefault());                
            }
            else
            {
                AddDataTemplate(viewModel.GetType().BaseType, FindAccurateType(viewModel.GetType().BaseType, viewTypes));                
            }

            dataTemplate = (DataTemplate)Application.Current.TryFindResource(new DataTemplateKey(dialogVmType));
            return dataTemplate.LoadContent();
        }

        private Window CreateWindow(ViewModelBase viewModel, WindowInfo windowInfo)
        {
            var window = new Window();
            window.Title = windowInfo?.Title ?? viewModel.GetType().Name.Replace("ViewModel","");
            window.SizeToContent = windowInfo?.SizeToContent ?? SizeToContent.WidthAndHeight;
            window.Content = GetDialogView(viewModel) ?? viewModel;
            window.DataContext = viewModel;
            window.ResizeMode = windowInfo?.ResizeMode ?? ResizeMode.NoResize;
            window.ShowInTaskbar = windowInfo?.ShowInTaskbar ?? false;
            window.WindowStartupLocation = windowInfo?.WindowStartupLocation ?? WindowStartupLocation.CenterOwner;
            window.Owner = windowInfo?.Owner ?? Application.Current.MainWindow;            
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


            if (viewTypes.Count() == 1)
            {
                AddDataTemplate(viewModel.GetType(), viewTypes.FirstOrDefault());
            }
            else
            {
                AddDataTemplate(viewModel.GetType(), FindAccurateType(vmType, viewTypes));
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
            const string xamlTemplate = "<DataTemplate DataType=\"{{x:Type vm:{0}}}\"><v:{1} /></DataTemplate>";
            var xaml = string.Format(xamlTemplate, viewModelType.Name, viewType.Name, viewModelType.Namespace, viewType.Namespace);

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
