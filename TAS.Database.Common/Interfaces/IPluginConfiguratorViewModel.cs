using System;
using TAS.Common.Interfaces;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginConfiguratorViewModel: IDisposable
    {
        event EventHandler PluginChanged;
        string PluginName { get; }
        void Initialize(IPlugin model);
        void Save();
    }
}
