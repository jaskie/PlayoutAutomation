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
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Common;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for ConvertOperationView.xaml
    /// </summary>
    public partial class ConvertOperationView : UserControl
    {

        public ConvertOperationView()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ConvertOperationViewModel vm = e.NewValue as ConvertOperationViewModel;
            if (vm != null)
                ((TimeSpanToSMPTEConverter)Resources["TimeSpanToSMPTE"]).FrameRate = vm.SourceMediaFrameRate;
        }


#if DEBUG
        ~ConvertOperationView()
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.BeginInvoke((Action)(() => System.Diagnostics.Debug.WriteLine(this.DataContext, "View finalized")));
        }

#endif // DEBUG

    }
}
