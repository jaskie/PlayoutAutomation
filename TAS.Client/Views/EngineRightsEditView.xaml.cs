using System.Windows;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for EventRightsEditView.xaml
    /// </summary>
    public partial class EngineRightsEditView : Window
    {
        public EngineRightsEditView()
        {
            InitializeComponent();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
