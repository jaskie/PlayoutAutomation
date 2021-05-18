using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitchPort : IVideoSwitchPortProperties
    {
        bool? IsSignalPresent { get; }
    }

    public interface IVideoSwitchPortProperties
    {
        short Id { get; }
        string Name { get; }

    }
}
