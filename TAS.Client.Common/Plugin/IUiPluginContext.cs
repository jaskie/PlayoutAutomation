using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPluginContext
    {
        IEvent SelectedEvent { get; }
        IMedia SelectedMedia { get; }
        IEngine Engine { get; }
    }
}
