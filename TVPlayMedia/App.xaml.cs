using Infralution.Localization.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

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

            string uiCulture = ConfigurationManager.AppSettings["UiLanguage"];
            if (string.IsNullOrWhiteSpace(uiCulture))
                CultureManager.UICulture = System.Globalization.CultureInfo.CurrentUICulture;
            else
                CultureManager.UICulture = new System.Globalization.CultureInfo(uiCulture);
        }
    }
}
