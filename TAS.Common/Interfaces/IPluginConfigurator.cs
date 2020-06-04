namespace TAS.Common.Interfaces
{
    public interface IPluginConfigurator : IEnginePlugin
    {
        void RegisterUiTemplates();
        string PluginName { get; }
        void Save();
    }
}
