using System;
using TAS.Common.Interfaces;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginConfiguratorViewModel : IPlugin
    {
        event EventHandler PluginChanged;
        object GetModel();
        string PluginName { get; }
        void Initialize(object model);
        void Save();
    }
}
