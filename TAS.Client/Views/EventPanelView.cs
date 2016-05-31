using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TAS.Client.ViewModels;

namespace TAS.Client.Views
{
    public class EventPanelView: UserControl
    {

        private string _viewName;
        public EventPanelView()
        {
            this.DataContextChanged += UserControl_DataContextChanged;
            
        }

        protected void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EventPanelViewmodelBase vm = e.NewValue as EventPanelViewmodelBase;
            if (vm != null)
            {
                vm.View = (EventPanelView)sender;
                _viewName = vm.EventName;
                if (vm.IsSelected)
                    this.BringIntoView();
                this.DataContextChanged -= UserControl_DataContextChanged;
            }
        }
        

#if DEBUG
        ~EventPanelView()
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.BeginInvoke((Action)(() => System.Diagnostics.Debug.WriteLine(_viewName, "View finalized")));
        }
#endif // DEBUG

        internal void SetOnTop()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is ScrollViewer))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
            {
                // Scroll to selected Item
                DispatcherHelper.WaitForPriority();
                Point offset = TransformToAncestor(parent as ScrollViewer).Transform(new Point(0, 0));
                (parent as ScrollViewer).ScrollToVerticalOffset(offset.Y + (parent as ScrollViewer).ContentVerticalOffset - ActualHeight);
            }
        }

    }
}
