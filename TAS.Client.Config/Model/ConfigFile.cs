using System;
using System.Configuration;
using TAS.Common;

namespace TAS.Client.Config.Model
{
    public class ConfigFile
    {
        public Configuration Configuration { get; }

        public ConfigFile(Configuration configuration)
        {
            Configuration = configuration;
            var asl = AppSettings.GetType().GetProperties();
            foreach (var setting in asl)
            {
                var aps = Configuration.AppSettings.Settings[setting.Name];
                if (aps == null)
                    continue;
                if (setting.PropertyType.IsEnum)
                    setting.SetValue(AppSettings, Enum.Parse(setting.PropertyType, aps.Value));
                else if (TryParseNullableEnum(setting.PropertyType, aps.Value, out var nullableEnum))
                    setting.SetValue(AppSettings, nullableEnum);
                else
                    setting.SetValue(AppSettings, Convert.ChangeType(Configuration.AppSettings.Settings[setting.Name].Value, setting.PropertyType));
            }
        }

        public void Save()
        {
            var asl = AppSettings.GetType().GetProperties();
            foreach (var setting in asl)
            {
                object newValue = setting.GetValue(AppSettings, null);
                if (Configuration.AppSettings.Settings[setting.Name] == null)
                    Configuration.AppSettings.Settings.Add(setting.Name, newValue?.ToString() ?? string.Empty);
                else
                    Configuration.AppSettings.Settings[setting.Name].Value = newValue == null ? string.Empty : setting.GetValue(AppSettings, null).ToString();
            }
            Configuration.Save();
        }

        public AppSettings AppSettings { get; } = new AppSettings();

        public string FileName => Configuration.FilePath;

        private static bool TryParseNullableEnum(Type t, string value, out object result)
        {
            Type undelyingType = Nullable.GetUnderlyingType(t);
            if (undelyingType != null && undelyingType.IsEnum)
            {
                if (string.IsNullOrEmpty(value))
                {
                    result = null;
                    return true;
                }
                if (undelyingType.IsEnum)
                {
                    result = Enum.Parse(undelyingType, value);
                    return true;
                }
            }
            result = null;
            return false;
        }
    }

    public class AppSettings
    {
        public DatabaseType? DatabaseType { get; set; }
        public double ReferenceLoudnessLevel { get; set; } = -23d;
        public string IngestFolders { get; set; }
        public string TempDirectory { get; set; }
        public int Instance { get; set; }
        public string UiLanguage { get; set; }
        public bool IsBackupInstance { get; set; }
    }

}
