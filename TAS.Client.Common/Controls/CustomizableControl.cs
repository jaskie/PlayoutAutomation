using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using TAS.Common;

namespace TAS.Client.Common.Controls
{
    public class CustomizableControl : ContentControl
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            CustomizableControlPropertyStore.Current.SetControlCustomizedProperties(this);
        }

        public static DependencyProperty ViewNameProperty = DependencyProperty.Register(
            nameof(ViewName), typeof(string), typeof(CustomizableControl), new PropertyMetadata());

        public string ViewName
        {
            get => (string) GetValue(ViewNameProperty);
            set => SetValue(ViewNameProperty, value);
        }
    }

    internal class CustomizableControlPropertyStore
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger(); 
        private CustomizableControlPropertyStore() { }

        private Dictionary<string, IList<CustomizedPropertySetter>> _customizedProperties = new Dictionary<string, IList<CustomizedPropertySetter>>();

        public static CustomizableControlPropertyStore Current { get; } = new CustomizableControlPropertyStore();
        public void SetControlCustomizedProperties(CustomizableControl view)
        {
            if (string.IsNullOrWhiteSpace(view.ViewName))
                return;
            if (!_customizedProperties.TryGetValue(view.ViewName, out var properties))
            {
                properties = LoadConfiguration(view);
                _customizedProperties.Add(view.ViewName, properties);
            }
            ApplyProperties(view, properties);
        }

        private void ApplyProperties(CustomizableControl control, IEnumerable<CustomizedPropertySetter> properties)
        {
            foreach (var property in properties)
                property.Update(control.ViewName, control);
        }

        private IList<CustomizedPropertySetter> LoadConfiguration(CustomizableControl view)
        {

            var fileName = Path.Combine(FileUtils.ConfigurationPath, "UI", $"{view.ViewName}.xml");
            if (File.Exists(fileName))
            {
                try
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
                            if (CustomizedPropertySetter.TryAdd(view, controlName.ToString(), propertyName.ToString(), propertyValue, out var setter))
                                setters.Add(setter);
                        }
                    }
                    return setters;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to parse UI configuration file for view {0}", view.ViewName);
                }
            }
            return Array.Empty<CustomizedPropertySetter>();
        }

        private class CustomizedPropertySetter
        {
            private readonly string _controlName;
            private readonly PropertyInfo _property;
            private readonly object _value;

            public static bool TryAdd(CustomizableControl view, string controlName, string propertyName, string value, out CustomizedPropertySetter propertySetter)
            {
                propertySetter = null;
                var control = view.FindName(controlName);
                if (control is null)
                {
                    Logger.Error("Unable to add customization for view {0} - control with name {1} not found", view.ViewName, controlName);
                    return false;
                }
                var pi = control.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (pi is null)
                {
                    Logger.Error("Unable to add customization for control {0} on view {1}, property {2} - property does not exists", controlName, view.ViewName, propertyName);
                    return false;
                }
                if (!pi.CanWrite)
                {
                    Logger.Error("Unable to add customization for control {0} on view {1}, property {2} - property is not writable", controlName, view.ViewName, propertyName);
                    return false;
                }
                try
                {
                    var convertedValue = ConvertFromString(value, pi.PropertyType);
                    propertySetter = new CustomizedPropertySetter(controlName, pi, convertedValue);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to add customization for control {0} on view {1}, property {2} - unable to convert value {3} to expected type {4}", controlName, view.ViewName, propertyName, value, pi.PropertyType);
                    return false;
                }
            }

            private CustomizedPropertySetter(string controlName, PropertyInfo property, object value)
            {
                _controlName = controlName;
                _property = property;
                _value = value;
            }

            public void Update(string viewName, ContentControl contentControl)
            {
                var control = contentControl.FindName(_controlName);
                if (control is null)
                {
                    Logger.Error("Unable to find control of name {0} of view {1} to set its property {2}", _controlName, viewName, _property.Name);
                    return;
                }
                try
                {
                    _property.SetValue(control, _value);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to set property of control {0} on view {1} to {2}", _controlName, viewName, _value);
                }
            }

            private static object ConvertFromString(string value, Type targetType)
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                    return converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);

                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
        }

    }
}
