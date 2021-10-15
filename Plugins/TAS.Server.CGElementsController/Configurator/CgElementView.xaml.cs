using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TAS.Server.CgElementsController.Configurator
{
    /// <summary>
    /// Interaction logic for CgElementView.xaml
    /// </summary>
    public partial class CgElementView : UserControl
    {
        public CgElementView()
        {
            InitializeComponent();
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (DataContext is CgElementViewModel viewModel)
                viewModel.Window = Window.GetWindow(this);
        }

    }
}
