using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Data.Common;
using System.Configuration;

namespace TAS.Client.Setup.Model
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
                cs.SetValue(connectionStrings, _configuration.ConnectionStrings.ConnectionStrings[cs.Name].ConnectionString, null);
            appSettings = new AppSettings();
            PropertyInfo[] asl = appSettings.GetType().GetProperties();
            foreach (PropertyInfo setting in asl)
                if (_configuration.AppSettings.Settings[setting.Name]!= null)
                    setting.SetValue(appSettings,  Convert.ChangeType(_configuration.AppSettings.Settings[setting.Name].Value, setting.PropertyType), null);
        }

        public void Save()
        {
            PropertyInfo[] csl = connectionStrings.GetType().GetProperties();
            foreach (PropertyInfo cs in csl)
                _configuration.ConnectionStrings.ConnectionStrings[cs.Name].ConnectionString = (string)cs.GetValue(connectionStrings, null);
            PropertyInfo[] asl = appSettings.GetType().GetProperties();
            foreach (PropertyInfo setting in asl)
                if (_configuration.AppSettings.Settings[setting.Name] == null)
                    _configuration.AppSettings.Settings.Add(setting.Name, setting.GetValue(appSettings, null).ToString());
                else
                    _configuration.AppSettings.Settings[setting.Name].Value = setting.GetValue(appSettings, null).ToString();
            _configuration.Save();
        }

        public class ConnectionStrings
        {
            public string tasConnectionString { get; set; }
        }

        public ConnectionStrings connectionStrings { get; set; }
        
        public class AppSettings
        {
            public string IngestFolders { get; set; }
            public string LocalDevices { get; set; }
            public string TempDirectory { get; set; }
            public int Instance { get; set; }
            public double VolumeReferenceLoudness { get; set; }
        }
        public AppSettings appSettings { get; set; }

        public string FileName { get { return _configuration.FilePath; } }
    }
}
