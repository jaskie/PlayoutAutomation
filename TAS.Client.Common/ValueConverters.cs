using System;
using System.Linq;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using TAS.Common;
using System.Windows;

namespace TAS.Client.Common
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToBrushButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (bool)value)
                return Brushes.LawnGreen;
            return new Button().Background;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (bool)value && !string.IsNullOrEmpty((string)parameter))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)parameter));
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }


    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanToSignedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            return (TimeSpan)value < TimeSpan.Zero ? ((TimeSpan)value).ToString("'-'h\\:mm\\:ss") : ((TimeSpan)value).ToString("'+'h\\:mm\\:ss");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan ts) || ts == TimeSpan.Zero) return 
                    string.Empty;
            if (ts.Days > 0)
                return ts < TimeSpan.Zero
                    ? ts.ToString("'-'d\\.hh\\:mm\\:ss")
                    : ts.ToString("d\\.hh\\:mm\\:ss");
            return ts < TimeSpan.Zero
                ? ts.ToString("'-'hh\\:mm\\:ss")
                : ts.ToString("hh\\:mm\\:ss");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(TimeSpan?), typeof(string))]
    public class TimeOfDayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan timeSpan)) return string.Empty;
            var result = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now) + timeSpan;
            if (result > TimeSpan.FromDays(1))
                result -= TimeSpan.FromDays(1);
            if (result < TimeSpan.Zero)
                result += TimeSpan.FromDays(1);
            return result.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string stringValue)) return null;
            var replaced = stringValue.Replace('_', '0');
            if (!TimeSpan.TryParse(replaced, out var result))
                return null;
            if (result.Days > 0)
                return null;
            result -= TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            if (result > TimeSpan.FromDays(1))
                result -= TimeSpan.FromDays(1);
            if (result < TimeSpan.Zero)
                result += TimeSpan.FromDays(1);
            return result;
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(Brush))]
    public class TimeSpanToRedGreenBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan span && span >= TimeSpan.Zero)
                return Brushes.Red;
            else
                return Brushes.Green;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateTimeToSMPTEConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime dateTime))
                return string.Empty;
            if (dateTime == default(DateTime))
                return string.Empty;
            if ((string)parameter == "TC")
                return dateTime.ToLocalTime().TimeOfDay.ToSmpteTimecodeString(FrameRate);
            return dateTime.ToLocalTime().Date.ToString("d") + " " + ((DateTime)value).ToLocalTime().TimeOfDay.ToSmpteTimecodeString(FrameRate);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string strValue))
                return Binding.DoNothing;
            var v = strValue.Split(' ');
            {
                if (v.Length != 2 || !v[1].IsValidSmpteTimecode(FrameRate))
                    return null;
                try
                {
                    return (DateTime.Parse(v[0]) + v[1].SmpteTimecodeToTimeSpan(FrameRate)).ToUniversalTime();
                }
                catch (FormatException) {}
                return Binding.DoNothing;
            }
        }
        public RationalNumber FrameRate { get; set; } = new RationalNumber(25, 1);
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime dateTimeValue) || dateTimeValue == default(DateTime))
                return string.Empty;
            if (parameter is string s)
                return dateTimeValue.ToLocalTime().ToString(s);
            return dateTimeValue.ToLocalTime().ToString();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(bool), typeof(double))]
    public class EnabledBoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolValue) || boolValue)
                return 1.0;
            return 0.5;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(int), typeof(string))]
    public class IntToStringNoZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int intValue) || intValue == 0)
                return string.Empty;
            return intValue.ToString();

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.TryParse((string)value, out var val) ? val : 0;
        }
    }

    [ValueConversion(typeof(Color), typeof(Brush))]
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is Color)) return null;
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(int?), typeof(Brush))]
    public class AgeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //TODO: write correct logic for conversion
            if (!(value is int intValue)) 
                return DependencyProperty.UnsetValue;
            byte b = (byte)(intValue > 365 ? 0 : byte.MaxValue - (intValue * byte.MaxValue / 365));
            var color = Color.FromRgb(byte.MaxValue, 128 , 128);
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(TMediaEmphasis), typeof(Brush))]
    public class MediaEmphasisToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is TMediaEmphasis)) 
                return Brushes.Transparent;
            if (value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(ColorAttribute), false).FirstOrDefault() is ColorAttribute attribute)
                return new SolidColorBrush(attribute.Color);
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToStringConverter : IValueConverter
    {
        public string FalseValue { get; set; }
        public string TrueValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return FalseValue;
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(TrueValue);
        }
    }

    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (bool)value ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(object), typeof(System.Windows.Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(object), typeof(System.Windows.Visibility))]
    public class InvertedNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(long), typeof(System.Windows.Visibility))]
    public class NonZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt64(value) == 0 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }


    [ValueConversion(typeof(string[]), typeof(string))]
    public class StringArrayToDelimitedStringConverter: IValueConverter
    {
        private static readonly string[] Separators = { ";" };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string[])
                return string.Join(Separators[0], (string[])value);
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().Split(Separators, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InvertedBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value == true ? false : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    [ValueConversion(typeof(object[]), typeof(System.Windows.Visibility))]
    public class MultiBooleanOrNotNullToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = true;
            foreach (object value in values)
            {
                if (value == null)
                    visible = false;
                if (value is bool b)
                    visible = visible && b;
            }
            return visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object[] ConvertBack(object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }

}
