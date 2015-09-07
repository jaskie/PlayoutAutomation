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

namespace TAS.Client
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

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is EventViewmodel)
                (e.NewValue as EventViewmodel).View = (EventPanelView)sender;
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
