using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TAS.Client.ViewModels;
using TAS.Server;
using System.Globalization;
using System.ComponentModel;
using TAS.Client.Common;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for EventPanel.xaml
    /// </summary>
    /// 

    public partial class EventPanelView : UserControl
    {
        public EventPanelView()
        {
            InitializeComponent();
        }

#if DEBUG
        ~EventPanelView()
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.BeginInvoke((Action)(() => System.Diagnostics.Debug.WriteLine(this.DataContext, "View finalized")));
        }
#endif // DEBUG

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EventPanelViewmodel vm = e.NewValue as EventPanelViewmodel;
            if (vm != null)
            {
                vm.View = (EventPanelView)sender;
                if (vm.IsSelected)
                    this.BringIntoView();
            }
        }


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
                (parent as ScrollViewer).ScrollToVerticalOffset(offset.Y + (parent as ScrollViewer).ContentVerticalOffset);
            }
        }
    }

}
