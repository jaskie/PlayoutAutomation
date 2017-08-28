using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IPlayoutServerChannel: IPlayoutServerChannelProperties, INotifyPropertyChanged
    {
        bool IsServerConnected { get; }
        int  AudioLevel { get; } 
    }

    public interface IPlayoutServerChannelProperties
    {
        int Id { get; }
        string ChannelName { get; set; }
        decimal MasterVolume { get; set; }
        string LiveDevice { get; set; }
        string PreviewUrl { get; set; }
        TVideoFormat VideoFormat { get; }
    }
}
