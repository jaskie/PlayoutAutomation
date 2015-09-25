using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;

namespace TAS.Client.Setup
{
    public class ConfigFileViewmodel:OkCancelViewmodelBase<Model.ConfigFile>
    {
        protected override void OnDispose() { }
        public ConfigFileViewmodel(Model.ConfigFile configFile) : base(configFile, new ConfigFileView(), string.Format("Config file ({0})", configFile.FileName)) { }

        protected override void Load(object source)
        {
            base.Load(Model.appSettings);
            base.Load(Model.connectionStrings);
        }

        protected override void Apply(object parameter)
        {
            base.Apply(Model.appSettings);
            base.Apply(Model.connectionStrings);
            Model.Save();
        }

        string _ingestFolders;
        public string IngestFolders { get { return _ingestFolders; } set { SetField(ref _ingestFolders, value, "IngestFolders"); } }
        string _localDevices;
        public string LocalDevices { get { return _localDevices; } set { SetField(ref _localDevices, value, "LocalDevices"); } }
        string _tempDirectory;
        public string TempDirectory { get { return _tempDirectory; } set { SetField(ref _tempDirectory, value, "TempDirectory"); } }
        double _volumeReferenceLoudness;
        public double VolumeReferenceLoudness { get { return _volumeReferenceLoudness; } set { SetField(ref _volumeReferenceLoudness, value, "VolumeReferenceLoudness"); } }
        int _instance;
        public int Instance { get { return _instance; } set { SetField(ref _instance, value, "Instance"); } }
        
    }
}
