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
            get => (string)GetValue(ViewNameProperty);
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
                    var configuration = XDocument.Load(fileName);
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
            private static readonly IDictionary<string, Type> AttachedOwnerAliases = new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                { "Grid", typeof(Grid) },
                { "DockPanel", typeof(DockPanel) },
                { "Canvas", typeof(Canvas) },
                { "Panel", typeof(Panel) }
            };

            private readonly string _controlName;
            private readonly DependencyProperty _dependencyProperty;
            private readonly PropertyInfo _propertyInfo;
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

                // Regular CLR property
                if (TryCreateClrPropertySetter(control, view.ViewName, controlName, propertyName, value, out propertySetter))
                    return true;

                // Regular DP on the target type (e.g. WidthProperty)
                if (TryCreateDependencyPropertySetter(control, view.ViewName, controlName, propertyName, value, out propertySetter))
                    return true;

                // Attached property: "Grid.Row", "DockPanel.Dock", etc.
                if (TryCreateAttachedPropertySetter(view.ViewName, controlName, propertyName, value, out propertySetter))
                    return true;

                Logger.Error("Unable to add customization for control {0} on view {1}, property {2} - property not identified", controlName, view.ViewName, propertyName);
                return false;
            }

            private static bool TryCreateClrPropertySetter(object control, string viewName, string controlName, string propertyName, string value, out CustomizedPropertySetter setter)
            {
                var pi = control.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                setter = null;
                if (pi != null)
                {
                    if (!pi.CanWrite)
                    {
                        Logger.Error("Unable to add customization for control {0} on view {1}, property {2} - property is not writable", controlName, viewName, propertyName);
                        return false;
                    }
                    try
                    {
                        var convertedValue = ConvertFromString(value, pi.PropertyType);
                        setter = new CustomizedPropertySetter(controlName, pi, convertedValue);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Unable to add customization for control {0} on view {1}, property {2} - unable to convert value {3} to expected type {4}",
                            controlName, viewName, propertyName, value, pi.PropertyType);
                        return false;
                    }
                }
                return false;
            }

            private static bool TryCreateDependencyPropertySetter(object control, string viewName, string controlName, string propertyName, string value, out CustomizedPropertySetter setter)
            {
                setter = null;

                var dp = FindDependencyProperty(control.GetType(), propertyName);
                if (dp == null)
                    return false;

                if (dp.ReadOnly)
                {
                    Logger.Error("Unable to add customization for control {0} on view {1}, dependency property {2} - dependency property is read-only",
                        controlName, viewName, propertyName);
                    return false;
                }

                try
                {
                    var convertedValue = ConvertFromString(value, dp.PropertyType);
                    setter = new CustomizedPropertySetter(controlName, dp, convertedValue);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to add customization for control {0} on view {1}, dependency property {2} - unable to convert value {3} to expected type {4}",
                        controlName, viewName, propertyName, value, dp.PropertyType);
                    return false;
                }
            }

            private static bool TryCreateAttachedPropertySetter(string viewName, string controlName, string propertyName, string value, out CustomizedPropertySetter setter)
            {
                setter = null;

                var dot = propertyName.IndexOf('.');
                if (dot <= 0 || dot >= propertyName.Length - 1)
                    return false;

                var ownerName = propertyName.Substring(0, dot);
                var attachedName = propertyName.Substring(dot + 1);

                if (!TryResolveOwnerType(ownerName, out var ownerType))
                {
                    Logger.Error("Unable to add customization for control {0} on view {1}, attached property {2} - owner type not resolved", controlName, viewName, propertyName);
                    return false;
                }

                var dp = FindDependencyProperty(ownerType, attachedName);
                if (dp == null)
                {
                    Logger.Error("Unable to add customization for control {0} on view {1}, attached property {2} - property not found", controlName, viewName, propertyName);
                    return false;
                }

                if (dp.ReadOnly)
                {
                    Logger.Error("Unable to add customization for control {0} on view {1}, attached property {2} - dependency property is read-only", controlName, viewName, propertyName);
                    return false;
                }

                try
                {
                    var convertedValue = ConvertFromString(value, dp.PropertyType);
                    setter = new CustomizedPropertySetter(controlName, dp, convertedValue);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Unable to add customization for control {0} on view {1}, attached property {2} - unable to convert value {3} to expected type {4}",
                        controlName, viewName, propertyName, value, dp.PropertyType);
                }
                return false;
            }

            private static bool TryResolveOwnerType(string ownerName, out Type ownerType)
            {
                ownerType = null;

                if (AttachedOwnerAliases.TryGetValue(ownerName, out ownerType))
                    return true;

                ownerType = Type.GetType(ownerName, throwOnError: false);
                return ownerType != null;
            }

            private static DependencyProperty FindDependencyProperty(Type ownerType, string propertyName)
            {
                var dpField = ownerType.GetField(propertyName + "Property",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                return dpField?.GetValue(null) as DependencyProperty;
            }

            private CustomizedPropertySetter(string controlName, PropertyInfo property, object value)
            {
                if (property == null)
                    throw new ArgumentNullException(nameof(property));

                _controlName = controlName;
                _propertyInfo = property;
                _value = value;
            }

            private CustomizedPropertySetter(string controlName, DependencyProperty property, object value)
            {
                if (property == null)
                    throw new ArgumentNullException(nameof(property));

                _controlName = controlName;
                _dependencyProperty = property;
                _value = value;
            }

            public void Update(string viewName, ContentControl contentControl)
            {
                var control = contentControl.FindName(_controlName);
                if (control is null)
                {
                    Logger.Error("Unable to find control of name {0} of view {1} to set its property", _controlName, viewName);
                    return;
                }

                try
                {
                    if (_propertyInfo != null)
                    {
                        _propertyInfo.SetValue(control, _value);
                        return;
                    }

                    if (_dependencyProperty != null && control is DependencyObject dependencyObject)
                        dependencyObject.SetValue(_dependencyProperty, _value);
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
