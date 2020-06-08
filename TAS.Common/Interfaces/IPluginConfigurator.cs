namespace TAS.Common.Interfaces
{
    public interface IPluginConfigurator : IPlugin
    {
        string PluginName { get; }
        void Initialize();        
        void Save();        
    }
}
