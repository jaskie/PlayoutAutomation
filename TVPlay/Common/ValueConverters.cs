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

namespace TAS.Client
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan)
            {
                if (parameter is string
                    && (string)parameter == "HIDE_ZERO_VALUE"
                    && (TimeSpan)value == TimeSpan.Zero)
                    return string.Empty;
                return ((TimeSpan)value).ToSMPTETimecodeString();
            }
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string && ((string)value).IsValidSMPTETimecode())
                return ((string)value).SMPTETimecodeToTimeSpan();
            else
                return null;
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(long))]
    public class TimeSpanToFramesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((TimeSpan)value).ToSMPTEFrames();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long val;
            if (long.TryParse((string)value, out val))
                return val.SMPTEFramesToTimeSpan();
            else
                return null;
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
                return null;
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateTimeToSMPTEConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((DateTime)value == default(DateTime))
                return string.Empty;
            if ((string)parameter == "TC")
                return ((DateTime)value).ToLocalTime().TimeOfDay.ToSMPTETimecodeString();
            return ((DateTime)value).ToLocalTime().Date.ToString("d") + " " + ((DateTime)value).ToLocalTime().TimeOfDay.ToSMPTETimecodeString();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] v = (value as string).Split(' ');
            {
                if (v.Length == 2 && v[1].IsValidSMPTETimecode())
                {
                    try
                    {
                        return (DateTime.Parse(v[0]) + v[1].SMPTETimecodeToTimeSpan()).ToUniversalTime();
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

    
    [ValueConversion(typeof(TPlayState), typeof(Uri))]
    public class PlayStateToUriConverter : IValueConverter
    {
        private static Uri _iconEmpty = new Uri("../Glyphs/Empty.png", UriKind.Relative);
        private static Uri _iconCheck = new Uri("../Glyphs/Check.png", UriKind.Relative);
        private static Uri _iconPlay = new Uri("../Glyphs/Play.png", UriKind.Relative);
        private static Uri _iconPause = new Uri("../Glyphs/Pause.png", UriKind.Relative);
        private static Uri _iconStop = new Uri("../Glyphs/Stop.png", UriKind.Relative);
        private static Uri _iconFade = new Uri("../Glyphs/Down.png", UriKind.Relative);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((TPlayState)value)
            {
                case TPlayState.Played:
                    return _iconCheck;
                case TPlayState.Playing:
                    return _iconPlay;
                case TPlayState.Aborted:
                    return _iconStop;
                case TPlayState.Paused:
                    return _iconPause;
                case TPlayState.Fading:
                    return _iconFade;
                default:
                    return _iconEmpty;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(TMediaErrorInfo), typeof(Uri))]
    public class MediaErrorInfoToUriConverter : IValueConverter
    {
        private static Uri _iconWarningRed = new Uri("../Glyphs/WarningRed.png", UriKind.Relative);
        private static Uri _iconWarningYellow = new Uri("../Glyphs/WarningYellow.png", UriKind.Relative);
        private static Uri _iconEmpty = new Uri("../Glyphs/Empty.png", UriKind.Relative);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((TMediaErrorInfo)value)
            {
                case TMediaErrorInfo.Missing:
                    return _iconWarningRed;
                case TMediaErrorInfo.TooShort:
                    return _iconWarningYellow;
                default:
                    return _iconEmpty;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
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
    public class NegativeBooleanToVisibilityConverter : IValueConverter
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

}
