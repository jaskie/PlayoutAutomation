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
        public ConfigFileViewmodel(Model.ConfigFile configFile):base(configFile, new ConfigFileView(), configFile.ToString())
        {

        }
    }
}
