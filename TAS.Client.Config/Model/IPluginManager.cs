namespace TAS.Client.Config.Model
{
    public interface IPluginManager
    {
        string Name { get; }
        bool? IsEnabled { get; }
    }
}
