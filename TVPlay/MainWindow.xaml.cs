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
using System.Windows.Threading;
using System.Diagnostics;
using TAS.Server;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using System.Threading;
using resources = TAS.Client.Common.Properties.Resources;
using System.Configuration;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Mutex mutex = new Mutex(false, "TASClientApplication");
        bool _systemShutdown;
        public MainWindow()
        {
            Application.Current.LoadCompleted += _loadCompleted;
            try
            {
                bool isBackupInstance;
                bool.TryParse(ConfigurationManager.AppSettings["IsBackupInstance"], out isBackupInstance);
                if ((!mutex.WaitOne(5000) && (MessageBox.Show(resources._query_StartAnotherInstance, resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel))
                    || (isBackupInstance && MessageBox.Show(resources._query_StartBackupInstance, resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes))
                {
                    _systemShutdown = true;
                    Application.Current.Shutdown(0);
                }
                else
                {
                    InitializeComponent();
                }
            }
            catch (AbandonedMutexException)
            {
                mutex.ReleaseMutex();
                mutex.WaitOne();
            }
        }

        private void _loadCompleted(object sender, NavigationEventArgs e)
        {
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.SessionEnding += _sessionEnding;
            Closing += AppMainWindow_Closing;
        }

        private void _sessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            _systemShutdown = true;
        }

        private void AppMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
#if DEBUG == false
            e.Cancel = !_systemShutdown && MessageBox.Show(resources._query_ExitApplication, resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.No;
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
