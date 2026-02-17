using Infralution.Localization.Wpf;
using System;
using System.Configuration;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using TAS.Client.Views;
using TAS.Server;


namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Mutex Mutex = new Mutex(false, "TASClientApplication");
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        internal bool IsShutdown { get; private set; }

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

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (!IsShutdown)
                Mutex.ReleaseMutex();
            EngineController.Current.ShutDown();
            NLog.LogManager.Shutdown();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var window = Current?.MainWindow;
            Logger.Error(e.Exception, "DispatcherUnhandledException");
            if (window == null)
                MessageBox.Show(e.Exception.Message, Common.Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(window, e.Exception.Message, Common.Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        protected override async void OnStartup(StartupEventArgs eventArgs)
        {
            try
            {
                bool.TryParse(ConfigurationManager.AppSettings["IsBackupInstance"], out var isBackupInstance);
                if ((!Mutex.WaitOne(5000) &&
                     (MessageBox.Show(Common.Properties.Resources._query_StartAnotherInstance, Common.Properties.Resources._caption_Confirmation,
                          MessageBoxButton.OKCancel) == MessageBoxResult.Cancel))
                     || (isBackupInstance && MessageBox.Show(Common.Properties.Resources._query_StartBackupInstance,
                             Common.Properties.Resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes))
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
            TAS.Common.LoggerConfig.AddDebuggerTarget();
            var splash = MainWindow as SplashScreenView;
            if (!IsShutdown)
            {
                AppDomain.CurrentDomain.SetThreadPrincipal(new GenericPrincipal(new LocalUser(), Array.Empty<string>()));
                if (await AppLoader.LoadApp())
                {
                    MainWindow = new MainWindow();
                    MainWindow.Show();
                }
            }
            splash?.Close();
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            if (MessageBox.Show(Common.Properties.Resources._query_ExitApplication, Common.Properties.Resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                IsShutdown = true;
            else
                e.Cancel = true;
            base.OnSessionEnding(e);
        }

    }
}
