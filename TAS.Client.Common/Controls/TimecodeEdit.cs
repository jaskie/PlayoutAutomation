using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TAS.Server.Common;
using Xceed.Wpf.Toolkit;

namespace TAS.Client.Common.Controls
{
    public class TimecodeEdit: MaskedTextBox
    {
        const string mask = "00:00:00:00";
        public TimecodeEdit()
        {
            Mask = mask;
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
            (d as TimecodeEdit)?._updateText();
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
            Text = Timecode.ToSMPTETimecodeString(VideoFormat);
        }

        public TimeSpan Timecode
        {
           get { return (TimeSpan)GetValue(TimecodeProperty); }
            set
            {
                if (value != Timecode)
                {
                    SetValue(TimecodeProperty, value);
                    _updateText();
                }
            }
        }

        public TVideoFormat VideoFormat
        {
            private get
            {
                return (TVideoFormat)GetValue(VideoFormatProperty);
            }
            set
            {
                if (value != VideoFormat)
                {
                    SetValue(VideoFormatProperty, value);
                    _updateText();
                }
            }
        }
    }
}
