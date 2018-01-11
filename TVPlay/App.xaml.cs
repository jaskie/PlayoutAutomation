using System;
using System.Configuration;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using TAS.Server;
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
        bool _isShutdown;

        public App()
        {
            new SplashScreenView().Show();

            #region hacks
            Common.WpfHacks.ApplyGridViewRowPresenter_CellMargin();
            #endregion

            string uiCulture = ConfigurationManager.AppSettings["UiLanguage"];
            if (string.IsNullOrWhiteSpace(uiCulture))
                CultureManager.UICulture = System.Globalization.CultureInfo.CurrentUICulture;
            else
                CultureManager.UICulture = new System.Globalization.CultureInfo(uiCulture);
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        public bool IsShutdown => _isShutdown;

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            EngineController.ShutDown();
            if (!_isShutdown)
                Mutex.ReleaseMutex();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var window = Current?.MainWindow;
            if (window == null)
                MessageBox.Show(e.Exception.Message, resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(window, e.Exception.Message, resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs eventArgs)
        {
            try
            {
                bool isBackupInstance;
                bool.TryParse(ConfigurationManager.AppSettings["IsBackupInstance"], out isBackupInstance);
                if ((!Mutex.WaitOne(5000) &&
                     (MessageBox.Show(resources._query_StartAnotherInstance, resources._caption_Confirmation,
                          MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                     || (isBackupInstance && MessageBox.Show(resources._query_StartBackupInstance,
                             resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes)))
                {
                    _isShutdown = true;
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

            try
            {
                SplashScreenView.Current?.Notify("Initializing engines...");
                EngineController.Initialize();
            }
            catch (TypeInitializationException e)
            {
                MessageBox.Show(string.Format(resources._message_CantInitializeEngines, e.InnerException),
                    resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                _isShutdown = true;
                Shutdown(1);
            }
            catch (Exception e)
            {
                var message =
#if DEBUG
                $"{e}";
#else
                $"{e.Source}:{e.GetType().Name} {e.Message}";
#endif
                MessageBox.Show(string.Format(resources._message_CantInitializeEngines, message), resources._caption_Error,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _isShutdown = true;
                Shutdown(1);
            }

            var splash = MainWindow as SplashScreenView;
            if (!_isShutdown)
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
                _isShutdown = true;
            else
                e.Cancel = true;
            base.OnSessionEnding(e);
        }
    }
}
