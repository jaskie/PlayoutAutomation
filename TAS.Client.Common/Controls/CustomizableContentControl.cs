using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using TAS.Common;

namespace TAS.Client.Common.Controls
{
    public class CustomizableContentControl : ContentControl
    {
        protected override void OnInitialized(EventArgs e)
        {
            CustomizableContentControlPropertyStore.Current.SetControlCustomizedProperties(this);
            base.OnInitialized(e);
        }

        public static DependencyProperty ViewNameProperty = DependencyProperty.Register(
            nameof(ViewName), typeof(string), typeof(CustomizableContentControl), new PropertyMetadata());

        public string ViewName
        {
            get => (string) GetValue(ViewNameProperty);
            set => SetValue(ViewNameProperty, value);
        }
    }

    internal class CustomizableContentControlPropertyStore
    {
        private CustomizableContentControlPropertyStore() { }

        private Dictionary<string, IList<CustomizedPropertySetter>> _customizedProperties = new Dictionary<string, IList<CustomizedPropertySetter>>();

        public static CustomizableContentControlPropertyStore Current { get; } = new CustomizableContentControlPropertyStore();
        public void SetControlCustomizedProperties(CustomizableContentControl view)
        {
            if (!_customizedProperties.TryGetValue(view.ViewName, out var properties))
            {
                properties = LoadConfiguration(view);
                _customizedProperties.Add(view.ViewName, properties);
            }
            ApplyProperties(view, properties);
        }

        private void ApplyProperties(ContentControl control, IEnumerable<CustomizedPropertySetter> properties)
        {
            foreach (var property in properties)
                property.Update(control);
        }

        private IList<CustomizedPropertySetter> LoadConfiguration(CustomizableContentControl view)
        {
            if (view.ViewName != null)
            {
                var fileName = Path.Combine(FileUtils.ConfigurationPath, "UI", $"{view.ViewName}.xml");
                if (File.Exists(fileName))
                {
                    XDocument configuration = XDocument.Load(fileName);
                    var setters = new List<CustomizedPropertySetter>();
                    foreach (var element in configuration.Root.Elements())
                    {
                        var controlName = element.Name;
                        foreach (var attribute in element.Attributes())
                        {
                            var propertyName = attribute.Name;
                            var propertyValue = attribute.Value;
                            var setter = CustomizedPropertySetter.TryAdd(view, controlName.ToString(), propertyName.ToString(), propertyValue);
                            if (setter != null)
                                setters.Add(setter);
                        }
                    }
                    return setters;
                }
            }
            return Array.Empty<CustomizedPropertySetter>();
        }

        private class CustomizedPropertySetter
        {
            private readonly string _controlName;
            private readonly PropertyInfo _property;
            private readonly object _value;

            public static CustomizedPropertySetter TryAdd(ContentControl contentControl, string controlName, string propertyName, string value)
            {
                var control = contentControl.FindName(controlName);
                if (!(control is FrameworkElement frameworkElement))
                    return null;
                var pi = frameworkElement.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (pi == null)
                    return null;
                var convertedValue = Convert.ChangeType(value, pi.PropertyType);
                return new CustomizedPropertySetter(controlName, pi, convertedValue);
            }

            private CustomizedPropertySetter(string controlName, PropertyInfo property, object value)
            {
                _controlName = controlName;
                _property = property;
                _value = value;
            }

            public void Update(ContentControl contentControl)
            {
                var control = contentControl.FindName(_controlName);
                if (!(control is FrameworkElement frameworkElement))
                    return;
                _property.SetValue(control, _value);
            }
        }

    }
}
