using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Threading;

namespace TAS.Client.Common
{
    /// <summary>
    /// Represents a TextBox whose Text Binding will get updated after a specified interval when the user stops entering text
    /// </summary>
    public class DelayedBindingTextBox : TextBox
    {


        private Timer timer;
        private delegate void Method();

        /// <summary>
        /// Gets and Sets the amount of time to wait after the text has changed before updating the binding
        /// </summary>
        public int DelayTime
        {
            get { return (int)GetValue(DelayTimeProperty); }
            set { SetValue(DelayTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DelayTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DelayTimeProperty =
            DependencyProperty.Register("DelayTime", typeof(int), typeof(DelayedBindingTextBox), new UIPropertyMetadata(1500));


        //override this to update the source if we get an enter or return
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {

            //we dont update the source if we accept enter
            if (this.AcceptsReturn == true) { }
            //update the binding if enter or return is pressed
            else if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                //get the binding
                BindingExpression bindingExpression = this.GetBindingExpression(TextBox.TextProperty);

                //if the binding is valid update it
                if (BindingCanProceed(bindingExpression))
                {
                    //update the source
                    bindingExpression.UpdateSource();
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {

            //get the binding
            BindingExpression bindingExpression = this.GetBindingExpression(TextBox.TextProperty);

            if (BindingCanProceed(bindingExpression))
            {
                //get rid of the timer if it exists
                if (timer != null)
                {
                    //dispose of the timer so that it wont get called again
                    timer.Dispose();
                }

                //recreate the timer everytime the text changes
                timer = new Timer(new TimerCallback((o) =>
                {

                    //create a delegate method to do the binding update on the main thread
                    Method x = (Method)delegate
                    {
                        //update the binding
                        bindingExpression.UpdateSource();
                    };

                    //need to check if the binding is still valid, as this is a threaded timer the text box may have been unloaded etc.
                    if (BindingCanProceed(bindingExpression))
                    {
                        //invoke the delegate to update the binding source on the main (ui) thread
                        Dispatcher.BeginInvoke(x, new object[] { });
                    }
                    //dispose of the timer so that it wont get called again
                    timer.Dispose();

                }), null, this.DelayTime, Timeout.Infinite);
            }

            base.OnTextChanged(e);
        }

        //makes sure a binding can proceed
        private bool BindingCanProceed(BindingExpression bindingExpression)
        {
            bool canProceed = false;

            //cant update if there is no BindingExpression
            if (bindingExpression == null) { }
            //cant update if we have no data item
            else if (bindingExpression.DataItem == null) { }
            //cant update if the binding is not active
            else if (bindingExpression.Status != BindingStatus.Active) { }
            //cant update if the parent binding is null
            else if (bindingExpression.ParentBinding == null) { }
            //dont need to update if the UpdateSourceTrigger is set to update every time the property changes
            else if (bindingExpression.ParentBinding.UpdateSourceTrigger == UpdateSourceTrigger.PropertyChanged) { }
            //we can proceed
            else
            {
                canProceed = true;
            }

            return canProceed;
        }
    }

}
