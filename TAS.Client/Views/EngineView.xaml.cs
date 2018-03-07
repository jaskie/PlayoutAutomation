//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TAS.Client.Common;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for Channel.xaml
    /// </summary>
    /// 
    public partial class EngineView : UserControl
    {
        public EngineView()
        {
            InitializeComponent();
        }

        private void SidePanelResizer_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var xadjust = SidePanel.Width - e.HorizontalChange;
            if (xadjust >= 0)
                SidePanel.Width = xadjust;
            e.Handled = true;
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            ClearCombo.IsDropDownOpen = false;
        }
    }
}
