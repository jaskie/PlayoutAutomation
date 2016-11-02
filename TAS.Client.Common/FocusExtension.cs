using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;

namespace TAS.Client.Common
{
    public class FocusExtension: Behavior<UIElement>
    {

        public bool IsFocused
        {
            get { return (bool)GetValue(IsFocusedProperty); }
            set { SetValue(IsFocusedProperty, value); }
        }
        public bool SelectAllOnFocus
        {
            get { return (bool)GetValue(SelectAllOnFocusProperty); }
            set { SetValue(SelectAllOnFocusProperty, value); }
        }


        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register("IsFocused", typeof(bool), typeof(FocusExtension),
             new FrameworkPropertyMetadata(false, OnIsFocusedPropertyChanged) { BindsTwoWayByDefault = true });

        public static readonly DependencyProperty SelectAllOnFocusProperty = DependencyProperty.Register("SelectAllOnFocus", typeof(bool), typeof(FocusExtension), new PropertyMetadata(false));

        protected override void OnAttached()
        {
            base.OnAttached();
            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.GotKeyboardFocus += AssociatedObject_GotKeyboardFocus;
                this.AssociatedObject.LostKeyboardFocus += AssociatedObject_LostKeyboardFocus;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.GotKeyboardFocus -= AssociatedObject_GotKeyboardFocus;
                this.AssociatedObject.LostKeyboardFocus -= AssociatedObject_LostKeyboardFocus;
            }
        }

        private void AssociatedObject_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            IsFocused = false;
        }

        private void AssociatedObject_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            IsFocused = true;
            if (SelectAllOnFocus)
            {
                var textBox = sender as System.Windows.Controls.Primitives.TextBoxBase;
                if (textBox != null)
                    textBox.SelectAll();
            }
        }

        private static void OnIsFocusedPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var fe = d as FocusExtension;
            var ao = fe.AssociatedObject;
            if (ao != null && fe != null && Equals(e.NewValue, true))
            {
                ao.Focus();
                var tb = ao as System.Windows.Controls.TextBox;
                if (tb != null && fe.SelectAllOnFocus)
                    tb.SelectAll();
            }
        }
    }

}
