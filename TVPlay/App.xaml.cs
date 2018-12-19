using System;
using System.Configuration;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using Infralution.Localization.Wpf;
using TAS.Client.Views;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Mutex Mutex = new Mutex(false, "TASClientApplication");
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(Application));

        public App()
        {
            new SplashScreenView().Show();

            #region hacks
            Common.WpfHacks.ApplyGridViewRowPresenter_CellMargin();
            #endregion

            var uiCulture = ConfigurationManager.AppSettings["UiLanguage"];
            CultureManager.UICulture = string.IsNullOrWhiteSpace(uiCulture) ? System.Globalization.CultureInfo.CurrentUICulture : new System.Globalization.CultureInfo(uiCulture);
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        public bool IsShutdown { get; private set; }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (!IsShutdown)
                Mutex.ReleaseMutex();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var window = Current?.MainWindow;
            if (window == null)
                MessageBox.Show(e.Exception.Message, resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(window, e.Exception.Message, resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Error(e);
            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs eventArgs)
        {
            try
            {
                bool.TryParse(ConfigurationManager.AppSettings["IsBackupInstance"], out var isBackupInstance);
                if ((!Mutex.WaitOne(5000) &&
                     (MessageBox.Show(resources._query_StartAnotherInstance, resources._caption_Confirmation,
                          MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                     || (isBackupInstance && MessageBox.Show(resources._query_StartBackupInstance,
                             resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes)))
                {
                    IsShutdown = true;
                    Shutdown(0);
                    return;
                }
            }
            catch (AbandonedMutexException)
            {
                Mutex.ReleaseMutex();
                Mutex.WaitOne();
            }
            base.OnStartup(eventArgs);

            var splash = MainWindow as SplashScreenView;
            if (!IsShutdown)
            {
                AppDomain.CurrentDomain.SetThreadPrincipal(new GenericPrincipal(new LocalUser(), new string[0]));
                SplashScreenView.Current?.Notify("Creating views...");
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            splash?.Close();
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            if (MessageBox.Show(resources._query_ExitApplication, resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                IsShutdown = true;
            else
                e.Cancel = true;
            base.OnSessionEnding(e);
        }
    }
}
