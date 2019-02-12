using System;
using System.Configuration;

namespace TAS.Client.Config.Model
{
    public class ConfigFile
    {
        readonly Configuration _configuration;
        public ConfigFile(string fileName)
        {
            _configuration = ConfigurationManager.OpenExeConfiguration(fileName);
            ConnectionStrings = new ConnectionStrings();
            var csl = ConnectionStrings.GetType().GetProperties();
            foreach (var cs in csl)
            {
                var css = _configuration.ConnectionStrings.ConnectionStrings[cs.Name];
                cs.SetValue(ConnectionStrings, css == null ? string.Empty : css.ConnectionString, null);
            }
            AppSettings = new AppSettings();
            var asl = AppSettings.GetType().GetProperties();
            foreach (var setting in asl)
            {
                var aps = _configuration.AppSettings.Settings[setting.Name];
                if (aps != null)
                    setting.SetValue(AppSettings, Convert.ChangeType(_configuration.AppSettings.Settings[setting.Name].Value, setting.PropertyType), null);
            }
        }

        public void Save()
        {
            var csl = ConnectionStrings.GetType().GetProperties();
            foreach (var cs in csl)
            {
                ConnectionStringSettings css = _configuration.ConnectionStrings.ConnectionStrings[cs.Name];
                if (css == null)
                    _configuration.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings(cs.Name, (string)cs.GetValue(ConnectionStrings, null)));
                else
                    css.ConnectionString = (string)cs.GetValue(ConnectionStrings, null);
            }
            var asl = AppSettings.GetType().GetProperties();
            foreach (var setting in asl)
            {
                object newValue = setting.GetValue(AppSettings, null);
                if (_configuration.AppSettings.Settings[setting.Name] == null)
                    _configuration.AppSettings.Settings.Add(setting.Name, newValue?.ToString() ?? string.Empty);
                else
                    _configuration.AppSettings.Settings[setting.Name].Value = newValue == null ? string.Empty : setting.GetValue(AppSettings, null).ToString();
            }
            _configuration.Save();
        }
        
        public ConnectionStrings ConnectionStrings { get; set; }
        

        public AppSettings AppSettings { get; set; }

        public string FileName => _configuration.FilePath;
    }


    public class AppSettings
    {
        public string IngestFolders { get; set; }
        public string TempDirectory { get; set; }
        public int Instance { get; set; }
        public string UiLanguage { get; set; }
        public bool IsBackupInstance { get; set; }
    }

    public class ConnectionStrings
    {
        public string tasConnectionString { get; set; }
        public string tasConnectionStringSecondary { get; set; }
    }
}
