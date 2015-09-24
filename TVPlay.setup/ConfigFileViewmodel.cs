using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;

namespace TAS.Client.Setup
{
    public class ConfigFileViewmodel:OkCancelViewmodelBase<ConfigFile>
    {
        protected override void OnDispose() { }
        public ConfigFileViewmodel(string fileName):base(new ConfigFile(fileName), new ConfigFileView(), fileName, 500, 300)
        {

        }
    }
}
