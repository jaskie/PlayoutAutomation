using System.Collections.Specialized;

namespace TAS.Common.Interfaces
{
    public interface IEnginePlugin
    {
        string EngineName { get; }
    }

    public struct EnginePluginContext
    {
        public IEngine Engine;
        public NameValueCollection AppSettings;
    }
}
