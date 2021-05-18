using System;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IPluginConfiguratorViewModel: IDisposable
    {
        string PluginName { get; }
        void Save();
        void Load();
        bool IsEnabled { get; set; }
        IPlugin Model { get; }
        
        event EventHandler ModifiedChanged;
    }
}
