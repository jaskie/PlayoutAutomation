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
using System.ComponentModel;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for PreviewPlayer.xaml
    /// </summary>
    public partial class PreviewView : UserControl
    {
        public PreviewView()
        {
            InitializeComponent();
        }

        //private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        //{
        //    if (((PropertyDescriptor)e.PropertyDescriptor).IsBrowsable == false)
        //        e.Cancel = true;
        //    e.Column.Header = ((PropertyDescriptor)e.PropertyDescriptor).DisplayName;
        //}
    }
}
