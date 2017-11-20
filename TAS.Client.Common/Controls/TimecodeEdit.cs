using System;
using System.Windows;
using System.Windows.Controls;
using TAS.Common;
using Xceed.Wpf.Toolkit;

namespace TAS.Client.Common.Controls
{
    public class TimecodeEdit: MaskedTextBox
    {
        const string TimecodeMask = "00:00:00:00";
        public TimecodeEdit()
        {
            Mask = TimecodeMask;
        }

        public static readonly DependencyProperty TimecodeProperty =
            DependencyProperty.Register(
            "Timecode",
            typeof(TimeSpan),
            typeof(TimecodeEdit),
            new FrameworkPropertyMetadata(TimeSpan.Zero, OnTimecodeChanged) { BindsTwoWayByDefault = true });

        public static readonly DependencyProperty VideoFormatProperty =
            DependencyProperty.Register(
                "VideoFormat",
                typeof(TVideoFormat),
                typeof(TimecodeEdit),
                new PropertyMetadata(TVideoFormat.Other, OnTimecodeChanged));

        private static void OnTimecodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimecodeEdit)d)._updateText();
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            var text = (string)GetValue(TextProperty);
            if (text.IsValidSMPTETimecode(VideoFormat))
                SetValue(TimecodeProperty, text.SMPTETimecodeToTimeSpan(VideoFormat));
            base.OnTextChanged(e);
        }

        private void _updateText()
        {
            SetValue(TextProperty, Timecode.ToSMPTETimecodeString(VideoFormat));
        }

        public TimeSpan Timecode
        {
           get => (TimeSpan)GetValue(TimecodeProperty);
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
            get => (TVideoFormat)GetValue(VideoFormatProperty);
            set
            {
                if (value == VideoFormat)
                    return;
                SetValue(VideoFormatProperty, value);
                _updateText();
            }
        }
    }
}
