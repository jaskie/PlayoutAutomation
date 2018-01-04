using System;
using System.Linq;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using TAS.Common;

namespace TAS.Client.Common
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToBrushButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
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
            if ((bool)value
                && !string.IsNullOrEmpty((string)parameter))
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
            if (value is TimeSpan ts)
            {
                if (ts.Equals(TimeSpan.Zero))
                    return string.Empty;
                return (TimeSpan) value < TimeSpan.Zero
                    ? ((TimeSpan) value).ToString("'-'hh\\:mm\\:ss")
                    : ((TimeSpan) value).ToString("hh\\:mm\\:ss");
            }
            return string.Empty;
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
            if (value is TimeSpan? && ((TimeSpan?)value).HasValue)
            {
                TimeSpan result = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now) + ((TimeSpan?)value).Value;
                if (result > TimeSpan.FromDays(1))
                    result -= TimeSpan.FromDays(1);
                if (result < TimeSpan.Zero)
                    result += TimeSpan.FromDays(1);
                return result.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                TimeSpan result;
                string replaced = ((string)value).Replace('_', '0');
                if (TimeSpan.TryParse(replaced, out result))
                {
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
            return null;
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(Brush))]
    public class TimeSpanToRedGreenBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan && (TimeSpan)value >= TimeSpan.Zero)
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
        private RationalNumber _frameRate = new RationalNumber(25, 1);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((DateTime)value == default(DateTime))
                return string.Empty;
            if ((string)parameter == "TC")
                return ((DateTime)value).ToLocalTime().TimeOfDay.ToSMPTETimecodeString(_frameRate);
            return ((DateTime)value).ToLocalTime().Date.ToString("d") + " " + ((DateTime)value).ToLocalTime().TimeOfDay.ToSMPTETimecodeString(_frameRate);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] v = (value as string).Split(' ');
            {
                if (v.Length == 2 && v[1].IsValidSMPTETimecode(_frameRate))
                {
                    try
                    {
                        return (DateTime.Parse(v[0]) + v[1].SMPTETimecodeToTimeSpan(_frameRate)).ToUniversalTime();
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
                }
                return null;
            }
        }
        public RationalNumber FrameRate { get { return _frameRate; } set { _frameRate = value; } }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((DateTime)value == default(DateTime))
                return string.Empty;
            if (parameter is string)
                return ((DateTime)value).ToLocalTime().ToString(parameter as string);
            return ((DateTime)value).ToLocalTime().ToString();
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
            if ((bool)value)
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
            if ((int)value == 0)
                return string.Empty;
            return ((int)value).ToString();

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int val;
            if (int.TryParse((string)value, out val))
                return val;
            return 0;
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

    [ValueConversion(typeof(TMediaEmphasis), typeof(Brush))]
    public class MediaEmphasisToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is TMediaEmphasis)) 
                return Brushes.Transparent;
            var nAttributes = value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(ColorAttribute), false);
            if (!nAttributes.Any())
                return Brushes.Transparent;
            return new SolidColorBrush((nAttributes.First() as ColorAttribute).Color);
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
            else
                return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? value.Equals(TrueValue) : false;
        }
    }

    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
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

    [ValueConversion(typeof(long), typeof(System.Windows.Visibility))]
    public class ZeroToVisibilityConverter : IValueConverter
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
        private static string[] separators = new string[] { ";" };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string[])
                return string.Join(separators[0], (string[])value);
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }
    }

}
