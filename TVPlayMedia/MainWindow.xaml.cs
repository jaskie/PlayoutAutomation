using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TAS.Client.ViewModels;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            InitializeComponent();
            Model.MediaManager model = new Model.MediaManager(ConfigurationManager.AppSettings["Host"]);
            model.Initialize();
            MediaManagerViewmodel vm = new MediaManagerViewmodel(model, null);
            _windowContent.Content = vm.View;
            var format = model.getFormatDescription();
            MessageBox.Show(string.Join(Environment.NewLine, format.Format, format.FrameRate));
            
        }
    }
}
