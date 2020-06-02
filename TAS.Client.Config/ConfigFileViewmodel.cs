using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config
{
    public class ConfigFileViewmodel : OkCancelViewmodelBase<Model.ConfigFile>
    {
        private string _ingestFolders;
        private string _tempDirectory;
        private int _instance;
        private bool _isBackupInstance;
        private string _uiLanguage;
        private double _referenceLoudnessLevel;
        private DatabaseType? _databaseType;
        private readonly List<IDatabase> _dbs;

        protected override void OnDispose() { }
        public ConfigFileViewmodel(Model.ConfigFile configFile)
            : base(configFile, typeof(ConfigFileView), $"Config file ({configFile.FileName})")
        {
            _dbs = DatabaseLoader.LoadDatabaseProviders().ToList();
            DatabaseTypes = _dbs.Select(db => db.DatabaseType).ToArray();
            Load(Model.AppSettings);
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
                    DatabaseConfigurator.Open(Model.Configuration);
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

        public string ExeDirectory => Path.GetDirectoryName(Model.FileName);
                       
        protected override void Update(object destObject = null)
        {
            base.Update(Model.AppSettings);
            DatabaseConfigurator?.Save();
            Model.Save();
        }

        public List<CultureInfo> SupportedLanguages { get; } = new List<CultureInfo> { CultureInfo.InvariantCulture, new CultureInfo("en"), new CultureInfo("pl") };

        private void DatabaseConfigurator_Modified(object sender, System.EventArgs e)
        {
            IsModified = true;
        }

    }
}
