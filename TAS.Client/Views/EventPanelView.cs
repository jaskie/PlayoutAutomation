using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TAS.Client.ViewModels;

namespace TAS.Client.Views
{
    public class EventPanelView: UserControl
    {

        public EventPanelView()
        {
            DataContextChanged += UserControl_DataContextChanged;
        }


        protected void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is EventPanelViewModelBase vm))
                return;
            vm.View = (EventPanelView)sender;
            if (vm.IsSelected)
                BringIntoView();
            DataContextChanged -= UserControl_DataContextChanged;
        }

        internal void SetOnTop()
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is ScrollViewer))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent == null)
                return;
            // Scroll to selected Item
            Common.DispatcherHelper.WaitForPriority();
            // TODO: find conditions for InvalidOperationException in TransformToAncestor below
            Point offset = TransformToAncestor(parent as ScrollViewer).Transform(new Point(0, 0));
            (parent as ScrollViewer).ScrollToVerticalOffset(offset.Y + (parent as ScrollViewer).ContentVerticalOffset - ActualHeight);
        }
    }
}
