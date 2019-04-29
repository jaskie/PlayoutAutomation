using System;
using System.Windows;
using System.Windows.Controls;
using TAS.Common;

namespace TAS.Client.Common.Controls
{
    public class TimecodeTextBlock: TextBlock
    {
        public static readonly DependencyProperty TimecodeProperty =
            DependencyProperty.Register(
            "Timecode",
            typeof(TimeSpan),
            typeof(TimecodeTextBlock),
            new PropertyMetadata(TimeSpan.Zero, OnTimecodeChanged));

        public static readonly DependencyProperty VideoFormatProperty =
            DependencyProperty.Register(
                "VideoFormat",
                typeof(TVideoFormat),
                typeof(TimecodeTextBlock),
                new PropertyMetadata(TVideoFormat.Other, OnTimecodeChanged));

        public static readonly DependencyProperty HideZeroProperty =
            DependencyProperty.Register(
                "HideZero",
                typeof(bool),
                typeof(TimecodeTextBlock),
                new PropertyMetadata(false, OnTimecodeChanged));

        private static void OnTimecodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimecodeTextBlock)d)._updateText();
        }

        private void _updateText()
        {
            if (HideZero && Timecode == TimeSpan.Zero)
                Text = string.Empty;
            else
                Text = Timecode.ToSmpteTimecodeString(VideoFormat);
        }

        public TimeSpan Timecode
        {
            get { return (TimeSpan)GetValue(TimecodeProperty); }
            set
            {
                if (value == Timecode)
                    return;
                SetValue(TimecodeProperty, value);
                _updateText();
            }
        }

        public TVideoFormat VideoFormat
        {
            get
            {
                return (TVideoFormat)GetValue(VideoFormatProperty);
            }
            set
            {
                if (value == VideoFormat)
                    return;
                SetValue(VideoFormatProperty, value);
                _updateText();
            }
        }

        public bool HideZero
        {
            get { return (bool)GetValue(HideZeroProperty); }
            set
            {
                if (value == HideZero)
                    return;
                SetValue(HideZeroProperty, value);
                _updateText();
            }
        }

    }
}
