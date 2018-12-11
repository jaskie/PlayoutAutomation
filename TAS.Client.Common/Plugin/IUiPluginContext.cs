using System.ComponentModel;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPluginContext: INotifyPropertyChanged
    {
        IEvent SelectedEvent { get; }
        IMedia SelectedMedia { get; }
        IEngine Engine { get; }
    }
}
