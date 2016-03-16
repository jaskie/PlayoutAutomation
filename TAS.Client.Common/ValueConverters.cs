using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using TAS.Common;
using TAS.Server;
using System.ComponentModel;
using System.Windows.Media;

namespace TAS.Client.Common
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToBrushButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value == true)
                return Brushes.LawnGreen;
            else
                return new Button { }.Background;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value == true
                && !string.IsNullOrEmpty((string)parameter))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString((string)parameter));
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanToSMPTEConverter : IValueConverter
    {
        private RationalNumber _frameRate = new RationalNumber(25, 1);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan)
            {
                if (parameter is string
                    && (string)parameter == "HIDE_ZERO_VALUE"
                    && (TimeSpan)value == TimeSpan.Zero)
                    return string.Empty;
                return ((TimeSpan)value).ToSMPTETimecodeString(_frameRate);
            }
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string && ((string)value).IsValidSMPTETimecode(_frameRate))
                return ((string)value).SMPTETimecodeToTimeSpan(_frameRate);
            else
                return null;
        }
        public RationalNumber FrameRate { get { return _frameRate; } set { _frameRate = value; } }
    }

    [ValueConversion(typeof(TimeSpan), typeof(long))]
    public class TimeSpanToFramesConverter : IValueConverter
    {
        private RationalNumber _frameRate = new RationalNumber(25, 1);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((TimeSpan)value).ToSMPTEFrames(_frameRate);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long val;
            if (long.TryParse((string)value, out val))
                return val.SMPTEFramesToTimeSpan(_frameRate);
            else
                return null;
        }
        public RationalNumber FrameRate { get { return _frameRate; } set { _frameRate = value; } }
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
                return null;
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
                else
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
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(double))]
    public class EnabledBoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return 1.0;
            else
                return 0.5;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(TStartType), typeof(int))]
    public class StartTypeToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (TStartType)value;
        }
    }

    [ValueConversion(typeof(TEventType), typeof(int))]
    public class EventTypeToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (TEventType)value;
        }
    }

    [ValueConversion(typeof(TEngineState), typeof(Brush))]
    public class EngineStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((TEngineState)value)
            {
                case TEngineState.Running:
                    return Brushes.LightPink;
                case TEngineState.Hold:
                    return Brushes.PaleGreen;
                default:
                    return null;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
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
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is Color)) return null;
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(TMediaEmphasis), typeof(Brush))]
    public class MediaEmphasisToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is TMediaEmphasis)) 
                return null;
            var nAttributes = value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(ColorAttribute), false);
            if (!nAttributes.Any())
                return null;
            return new SolidColorBrush((nAttributes.First() as ColorAttribute).Color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToStringConverter : IValueConverter
    {
        public string FalseValue { get; set; }
        public string TrueValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return FalseValue;
            else
                return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null ? value.Equals(TrueValue) : false;
        }
    }

    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(object), typeof(System.Windows.Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string[]), typeof(string))]
    public class StringArrayToDelimitedStringConverter: IValueConverter
    {
        private static string[] separators = new string[] { ";" };
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string[])
                return string.Join(separators[0], (string[])value);
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }
    }

}
