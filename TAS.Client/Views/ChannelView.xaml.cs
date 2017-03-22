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

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for ChannelView.xaml
    /// </summary>
    public partial class ChannelView : UserControl
    {
        public ChannelView()
        {
            InitializeComponent();
        }
    }

    public class TabSelector : DataTemplateSelector
    {
        public DataTemplate EngineTemplate { get; set; }
        public DataTemplate MediaManagerTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ViewModels.EngineViewmodel)
                return EngineTemplate;
            if (item is ViewModels.MediaManagerViewmodel)
                return MediaManagerTemplate;
            return null;
        }
    }
}
