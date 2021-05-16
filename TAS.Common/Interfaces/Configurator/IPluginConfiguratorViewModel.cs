using System;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IPluginConfiguratorViewModel: IDisposable
    {
        event EventHandler PluginChanged;
        string PluginName { get; }
        void Initialize(IPlugin model);
        void Save();
        void Load();
    }
}
