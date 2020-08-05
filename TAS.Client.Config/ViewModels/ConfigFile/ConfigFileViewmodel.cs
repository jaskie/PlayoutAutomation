using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.ConfigFile
{
    public class ConfigFileViewModel : OkCancelViewModelBase
    {
        private string _ingestFolders;
        private string _tempDirectory;
        private int _instance;
        private bool _isBackupInstance;
        private string _uiLanguage;
        private double _referenceLoudnessLevel;
        private DatabaseType? _databaseType;
        private readonly List<IDatabase> _dbs;

        private Model.ConfigFile _configFile;

        protected override void OnDispose() { }
        public ConfigFileViewModel(Model.ConfigFile configFile)
        {
            _configFile = configFile;
            _dbs = DatabaseLoader.LoadDatabaseProviders().ToList();
            DatabaseTypes = _dbs.Select(db => db.DatabaseType).ToArray();
            Init();
        }

        public void Init()
        {
            IngestFolders = _configFile.AppSettings.IngestFolders;
            ReferenceLoudnessLevel = _configFile.AppSettings.ReferenceLoudnessLevel;
            TempDirectory = _configFile.AppSettings.TempDirectory;
            DatabaseType = _configFile.AppSettings.DatabaseType;
            Instance = _configFile.AppSettings.Instance;
            UiLanguage = _configFile.AppSettings.UiLanguage;
            IsBackupInstance = _configFile.AppSettings.IsBackupInstance;
        }

        public override bool Ok(object obj)
        {
            DatabaseConfigurator?.Save();
            
            _configFile.AppSettings.IngestFolders = IngestFolders;
            _configFile.AppSettings.ReferenceLoudnessLevel = ReferenceLoudnessLevel;
            _configFile.AppSettings.TempDirectory = TempDirectory;
            _configFile.AppSettings.DatabaseType = DatabaseType ?? TAS.Common.DatabaseType.MySQL;
            _configFile.AppSettings.Instance = Instance;
            _configFile.AppSettings.UiLanguage = UiLanguage;
            _configFile.AppSettings.IsBackupInstance = IsBackupInstance;
            _configFile.Save();
            return true;
        }

        public string IngestFolders { get => _ingestFolders; set => SetField(ref _ingestFolders, value); }

        public double ReferenceLoudnessLevel { get => _referenceLoudnessLevel; set => SetField(ref _referenceLoudnessLevel, value); }
        
        public string TempDirectory { get => _tempDirectory; set => SetField(ref _tempDirectory, value); }

        public int Instance { get => _instance; set => SetField(ref _instance, value); }

        public DatabaseType? DatabaseType
        {
            get => _databaseType; set
            {
                var oldConfigurator = DatabaseConfigurator;
                if (!SetField(ref _databaseType, value))
                    return;
                DatabaseConfigurator = value == null ? null : DatabaseLoader.LoadDatabaseConfigurator(value.Value);
                if (DatabaseConfigurator != null)
                {
                    DatabaseConfigurator.Open(_configFile.Configuration);
                    DatabaseConfigurator.Modified += DatabaseConfigurator_Modified;
                }
                if (oldConfigurator != null)
                    oldConfigurator.Modified -= DatabaseConfigurator_Modified;
                NotifyPropertyChanged(nameof(DatabaseConfigurator));
            }
        }

        public DatabaseType[] DatabaseTypes { get; }

        public IDatabaseConfigurator DatabaseConfigurator { get; private set; }

        public bool IsBackupInstance
        {
            get => _isBackupInstance;
            set => SetField(ref _isBackupInstance, value);
        }

        public string UiLanguage
        {
            get => _uiLanguage;
            set => SetField(ref _uiLanguage, value);
        }

        public string ExeDirectory => Path.GetDirectoryName(_configFile.FileName);                               

        public List<CultureInfo> SupportedLanguages { get; } = new List<CultureInfo> { CultureInfo.InvariantCulture, new CultureInfo("en"), new CultureInfo("pl") };

        private void DatabaseConfigurator_Modified(object sender, System.EventArgs e)
        {
            IsModified = true;
        }       
    }
}
