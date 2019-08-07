using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using TAS.Server;
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

        protected override void OnClosing(CancelEventArgs e)
        {
#if DEBUG == false
            int connectedClientCount = EngineController.GetConnectedClientCount();
            e.Cancel = !((App)Application.Current).IsShutdown
                       && (MessageBox.Show(resources._query_ExitApplication, resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes
                           || (connectedClientCount > 0 && MessageBox.Show(string.Format(resources._query_ClientsConnectedOnExit, connectedClientCount), resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes));
#endif // DEBUG
            base.OnClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
#if DEBUG
            if (e.Key == Key.G && e.KeyboardDevice.Modifiers == (ModifierKeys.Alt | ModifierKeys.Control))
            {
                GC.Collect(GC.MaxGeneration);
                Debug.WriteLine("CG enforced");
                e.Handled = true;
            }
#endif
        }

    }
}
