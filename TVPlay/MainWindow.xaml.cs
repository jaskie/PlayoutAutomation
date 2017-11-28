using System;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            (TryFindResource("MainWindowVM") as MainWindowViewmodel)?.Dispose();
            base.OnClosed(e);
        }

        private void AppMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
#if DEBUG == false
            e.Cancel = !((App)Application.Current).IsIsSystemShutdown && MessageBox.Show(resources._query_ExitApplication, resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.No;
#endif // DEBUG
        }

        private void AppMainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.G && e.KeyboardDevice.Modifiers == (ModifierKeys.Alt | ModifierKeys.Control))
            {
                GC.Collect(GC.MaxGeneration);
                Debug.WriteLine("CG enforced");
                e.Handled = true;
            }
        }
    }
}
