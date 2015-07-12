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

        private static string volumeReferenceLoudnessKey = "VolumeReferenceLoudness";
        
        public App()
        {
            decimal referenceLoudness = -23;
            if (ConfigurationManager.AppSettings.AllKeys.Contains(volumeReferenceLoudnessKey))
                decimal.TryParse(ConfigurationManager.AppSettings[volumeReferenceLoudnessKey], out referenceLoudness);
            App.Current.Properties["VolumeReferenceLoudness"] = referenceLoudness;
            CultureManager.UICulture = System.Globalization.CultureInfo.CurrentUICulture;
            EngineController = new EngineController();
        //CultureManager.UICulture = new System.Globalization.CultureInfo("en");
        }
        protected override void OnExit(ExitEventArgs e)
        {
            EngineController.Dispose();
            base.OnExit(e);
        }
    }
}
