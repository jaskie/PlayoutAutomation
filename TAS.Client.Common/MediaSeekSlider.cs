using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;

namespace TAS.Client.Common
{
    public class MediaSeekSlider: Slider
    {
        #region Position dependency property

        public static readonly DependencyProperty PositionProperty =
        DependencyProperty.RegisterAttached(
            "Position",
            typeof(double),
            typeof(MediaSeekSlider),
            new FrameworkPropertyMetadata(0D, new PropertyChangedCallback(OnPositionChanged), new CoerceValueCallback(CoercePosition))
            {
                BindsTwoWayByDefault = true,
            });

        private static object CoercePosition(DependencyObject d, object value)
        {
            MediaSeekSlider c = (MediaSeekSlider)d;
            double v = (double)value;
            if (v < c.Minimum) v = c.Minimum;
            if (v > c.Maximum) v = c.Maximum;
            return v;
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(PositionProperty);
            (d as MediaSeekSlider).Value = (d as MediaSeekSlider).Position;
        }

        public double Position
        {
            get { return (double)this.GetValue(PositionProperty); }
            set
            {
                if (value != Position)
                    this.SetValue(PositionProperty, value);
                if (value != Value)
                    this.SetValue(ValueProperty, value);
            }
        }

        #endregion //Position dependency property

        public MediaSeekSlider(): base()
        {
            MouseWheel += _mouseWheel;
        }

        private void _mouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double delta = ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None) ? TickFrequency :
                            (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None ? SmallChange : LargeChange;
            if (e.Delta > 0)
                Value = Value + delta;
            else
                Value = Value - delta;
            e.Handled = true;
        }

        private bool _dragging = false;

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            _dragging = true;
            base.OnThumbDragStarted(e);
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            _dragging = false;
            Position = Value;
            base.OnThumbDragCompleted(e);
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            if (!_dragging)
                Position = Value;
            base.OnValueChanged(oldValue, newValue);
        }

    }
}
