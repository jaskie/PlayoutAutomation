using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using resources = TAS.Client.Common.Properties.Resources;
using TAS.Server;

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

        private void ShutdownApplication()
        {
            var splashScreen = new Views.SplashScreenView() { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            splashScreen.Notify(resources._splash_ClosingApplication);
            splashScreen.Show();
            MainWindowViewmodel.Current?.Dispose();
            splashScreen.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
#if DEBUG == false
            int connectedClientCount = EngineController.Current.GetConnectedClientCount();
            e.Cancel = !((App)Application.Current).IsShutdown
                       && (MessageBox.Show(this, resources._query_ExitApplication, resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes
                           || (connectedClientCount > 0 && MessageBox.Show(this, string.Format(resources._query_ClientsConnectedOnExit, connectedClientCount), resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes));
#endif // DEBUG
            if (!e.Cancel)
                ShutdownApplication();
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
