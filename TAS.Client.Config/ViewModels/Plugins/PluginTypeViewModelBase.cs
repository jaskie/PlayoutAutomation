using System;
using TAS.Client.Common;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public abstract class PluginTypeViewModelBase : ModifyableViewModelBase
    {
        public PluginTypeViewModelBase(string pluginTypeName)
        {
            Name = pluginTypeName;
        }

        protected override void OnDispose() { }
        public string Name { get; }

        public abstract void Save();

    }
}
