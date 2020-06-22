using System;

namespace TAS.Common.Interfaces
{
    public interface IPluginConfigurator : IPlugin
    {
        event EventHandler PluginChanged;
        object GetModel();
        string PluginName { get; }
        void Initialize(object model);        
        void Save();        
    }
}
