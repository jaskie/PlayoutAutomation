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

namespace TAS.Database.MySqlRedundant.Configurator
{
    /// <summary>
    /// Interaction logic for ConfiguratorView.xaml
    /// </summary>
    public partial class ConfiguratorView : UserControl
    {
        public ConfiguratorView()
        {
            InitializeComponent();

        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property.Name == nameof(IsVisible)
                && DataContext is ConfiguratorViewModel configuratorViewModel)
                configuratorViewModel.Window = Window.GetWindow(this);
        }
    }

}
