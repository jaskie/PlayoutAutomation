using Infralution.Localization.Wpf;
using System.Configuration;
using System.Windows;
using TAS.Client.Views;
using TVPlayClient;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Common.WpfHacks.ApplyGridViewRowPresenter_CellMargin();
            new SplashScreenView().Show();
            string uiCulture = ConfigurationManager.AppSettings["UiLanguage"];
            if (string.IsNullOrWhiteSpace(uiCulture))
                CultureManager.UICulture = System.Globalization.CultureInfo.CurrentUICulture;
            else
                CultureManager.UICulture = new System.Globalization.CultureInfo(uiCulture);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            TAS.Common.LoggerConfig.AddDebuggerTarget();
            var splash = MainWindow as SplashScreenView;
            MainWindow = new MainWindow();
            MainWindow.Show();
            splash?.Close();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            NLog.LogManager.Shutdown();
            base.OnExit(e);
        }
    }
}
