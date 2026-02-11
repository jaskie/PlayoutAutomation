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
            SetFileNamesVisibility();
            cbShowFileNames.Checked += (s, e) => SetFileNamesVisibility();
            cbShowFileNames.Unchecked += (s, e) => SetFileNamesVisibility();
            MediaListGrid.RowStyle = new Style(typeof(DataGridRow))
            {
                Setters = { new Setter(Control.FontSizeProperty, UISettings.GetFontSize(MediaListGrid.FontSize)) }
            };
        }

        void SetFileNamesVisibility()
        {
            clFileName.Visibility = cbShowFileNames.IsChecked ? Visibility.Visible : Visibility.Hidden;
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey != Key.LeftAlt)
                return;
            mainMenu.Visibility = mainMenu.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TreeViewEx_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DirectoryCombo.IsOpen = false;
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
