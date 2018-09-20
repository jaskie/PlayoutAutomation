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
using System.Collections;
using System.Windows.Controls.Primitives;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for ServerManager.xaml
    /// </summary>
    public partial class MediaManagerView : UserControl
    {
        public MediaManagerView()
        {
            InitializeComponent();
        }
        
        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
            {
                if (mainMenu.Visibility == Visibility.Collapsed)
                    mainMenu.Visibility = Visibility.Visible;
                else
                    mainMenu.Visibility = Visibility.Collapsed;
            }
        }

        private void TreeViewEx_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            FolderCombo.IsOpen = false;
        }

        private void SidePanelResizer_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var xadjust = SidePanel.Width - e.HorizontalChange;
            if (xadjust >= 0)
                SidePanel.Width = xadjust;
            e.Handled = true;
        }

        private void Expander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            MediaListGrid.Focus();
        }
    }
}
