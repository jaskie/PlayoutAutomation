using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using TAS.Common;
using TAS.Client.Common;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for PreviewPlayer.xaml
    /// </summary>
    public partial class PreviewView : UserControl
    {
        public PreviewView(RationalNumber frameRate)
        {
            InitializeComponent();
            ((TimeSpanToSMPTEConverter)Resources["TimeSpanToSMPTE"]).FrameRate = frameRate;
        }

        //private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        //{
        //    if (((PropertyDescriptor)e.PropertyDescriptor).IsBrowsable == false)
        //        e.Cancel = true;
        //    e.Column.Header = ((PropertyDescriptor)e.PropertyDescriptor).DisplayName;
        //}
    }
}
