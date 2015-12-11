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

        public static EngineController EngineController;

        public App()
        {
            CultureManager.UICulture = System.Globalization.CultureInfo.CurrentUICulture;
            #region hacks
            Common.WpfHacks.ApplyGridViewRowPresenter_CellMargin();
            #endregion
            //CultureManager.UICulture = new System.Globalization.CultureInfo("en");
            EngineController = new EngineController();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            EngineController.Dispose();
            base.OnExit(e);
        }
    }
}
