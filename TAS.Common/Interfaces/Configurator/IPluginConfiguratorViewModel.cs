using System;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IPluginConfiguratorViewModel: IDisposable
    {
        event EventHandler ModifiedChanged;
        string PluginName { get; }
        void Save();
        void Load();
        bool IsEnabled { get; set; }
        IPlugin Model { get; }
    }
}
