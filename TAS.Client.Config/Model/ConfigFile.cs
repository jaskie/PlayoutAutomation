using System;
using System.Reflection;
using System.Configuration;

namespace TAS.Client.Config.Model
{
    public class ConfigFile
    {
        readonly Configuration _configuration;
        public ConfigFile(string fileName)
        {
            _configuration = ConfigurationManager.OpenExeConfiguration(fileName);
            connectionStrings = new ConnectionStrings();
            PropertyInfo[] csl = connectionStrings.GetType().GetProperties();
            foreach (PropertyInfo cs in csl)
            {
                ConnectionStringSettings css = _configuration.ConnectionStrings.ConnectionStrings[cs.Name];
                cs.SetValue(connectionStrings, css == null ? string.Empty : css.ConnectionString, null);
            }
            appSettings = new AppSettings();
            PropertyInfo[] asl = appSettings.GetType().GetProperties();
            foreach (PropertyInfo setting in asl)
            {
                var aps = _configuration.AppSettings.Settings[setting.Name];
                if (aps != null)
                    setting.SetValue(appSettings, Convert.ChangeType(_configuration.AppSettings.Settings[setting.Name].Value, setting.PropertyType), null);
            }
        }

        public void Save()
        {
            PropertyInfo[] csl = connectionStrings.GetType().GetProperties();
            foreach (PropertyInfo cs in csl)
            {
                ConnectionStringSettings css = _configuration.ConnectionStrings.ConnectionStrings[cs.Name];
                if (css == null)
                    _configuration.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings(cs.Name, (string)cs.GetValue(connectionStrings, null)));
                else
                    css.ConnectionString = (string)cs.GetValue(connectionStrings, null);
            }
            PropertyInfo[] asl = appSettings.GetType().GetProperties();
            foreach (PropertyInfo setting in asl)
            {
                object newValue = setting.GetValue(appSettings, null);
                if (_configuration.AppSettings.Settings[setting.Name] == null)
                    _configuration.AppSettings.Settings.Add(setting.Name, newValue?.ToString() ?? string.Empty);
                else
                    _configuration.AppSettings.Settings[setting.Name].Value = newValue == null ? string.Empty : setting.GetValue(appSettings, null).ToString();
            }
            _configuration.Save();
        }

        public class ConnectionStrings
        {
            public string tasConnectionString { get; set; }
            public string tasConnectionStringSecondary { get; set; }
        }

        public ConnectionStrings connectionStrings { get; set; }
        
        public class AppSettings
        {
            public string IngestFolders { get; set; }
            public string LocalDevices { get; set; }
            public string TempDirectory { get; set; }
            public int Instance { get; set; }
            public string UiLanguage { get; set; }
            public bool IsBackupInstance { get; set; }
        }
        public AppSettings appSettings { get; set; }

        public string FileName => _configuration.FilePath;
    }
}
