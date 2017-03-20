using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TAS.Common;
using Xceed.Wpf.Toolkit;

namespace TAS.Client.Common.Controls
{
    public class TimecodeEdit: MaskedTextBox
    {
        const string mask = "00:00:00:00";
        public TimecodeEdit(): base()
        {
            Mask = mask;
        }

        public static readonly DependencyProperty TimecodeProperty =
            DependencyProperty.Register(
            "Timecode",
            typeof(TimeSpan),
            typeof(TimecodeEdit),
            new PropertyMetadata(TimeSpan.Zero, OnTimecodeChanged));

        public static readonly DependencyProperty FrameRateProperty =
            DependencyProperty.Register(
                "FrameRate",
                typeof(RationalNumber),
                typeof(TimecodeEdit),
                new PropertyMetadata(new RationalNumber(25, 1), OnTimecodeChanged));

        private static void OnTimecodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TimecodeEdit)._updateText();
        }
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
        }

        private void _updateText()
        {
            Text = Timecode.ToSMPTETimecodeString(FrameRate);
        }

        protected override void ValidateValue(object value)
        {
            Debug.WriteLine(value, "ValidateValue");
            base.ValidateValue(value);
        }

        public TimeSpan Timecode
        {
           get { return (TimeSpan)GetValue(TimecodeProperty); }
            set
            {
                if (value != Timecode)
                {
                    Debug.WriteLine(value, "Timecode");
                    SetValue(TimecodeProperty, value);
                    _updateText();
                }
            }
        }

        public RationalNumber FrameRate
        {
            private get
            {
                return (RationalNumber)GetValue(FrameRateProperty);
            }
            set
            {
                if (value != FrameRate)
                {
                    SetValue(FrameRateProperty, value);
                    _updateText();
                }
            }
        }
    }
}
