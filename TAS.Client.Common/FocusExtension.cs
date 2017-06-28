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
            if (AssociatedObject == null)
                return;
            AssociatedObject.GotKeyboardFocus += AssociatedObject_GotKeyboardFocus;
            AssociatedObject.LostKeyboardFocus += AssociatedObject_LostKeyboardFocus;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject == null)
                return;
            AssociatedObject.GotKeyboardFocus -= AssociatedObject_GotKeyboardFocus;
            AssociatedObject.LostKeyboardFocus -= AssociatedObject_LostKeyboardFocus;
        }

        private void AssociatedObject_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            IsFocused = false;
        }

        private void AssociatedObject_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            IsFocused = true;
            if (!SelectAllOnFocus)
                return;
            var textBox = sender as System.Windows.Controls.Primitives.TextBoxBase;
            textBox?.SelectAll();
        }

        private static void OnIsFocusedPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var fe = d as FocusExtension;
            var ao = fe?.AssociatedObject;
            if (ao == null || !Equals(e.NewValue, true))
                return;
            ao.Focus();
            var tb = ao as System.Windows.Controls.TextBox;
            if (tb != null && fe.SelectAllOnFocus)
                tb.SelectAll();
        }
    }

}
