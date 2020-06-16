namespace TAS.Common.Interfaces
{
    public interface IPluginConfigurator : IPlugin
    {
        object GetModel();
        string PluginName { get; }
        void Initialize(object model);        
        void Save();        
    }
}
