using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using TAS.Server;
using System.Reflection;
using Infralution.Localization.Wpf;
using System.Threading;

namespace TAS.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {


        public App()
        {
            
            #region hacks
            Common.WpfHacks.ApplyGridViewRowPresenter_CellMargin();
            #endregion
            string uiCulture = ConfigurationManager.AppSettings["UiLanguage"];
            if (string.IsNullOrWhiteSpace(uiCulture))
                CultureManager.UICulture = System.Globalization.CultureInfo.CurrentUICulture;
            else
                CultureManager.UICulture = new System.Globalization.CultureInfo(uiCulture);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            EngineController.ShutDown();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(App.Current.MainWindow, e.Exception.Message, TAS.Client.Common.Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
