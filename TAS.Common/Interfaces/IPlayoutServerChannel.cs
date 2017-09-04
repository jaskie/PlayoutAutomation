using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IPlayoutServerChannel: INotifyPropertyChanged
    {
        int Id { get; }
        bool IsServerConnected { get; }
        int  AudioLevel { get; }
        string ChannelName { get; }
        TVideoFormat VideoFormat { get; }
        string PreviewUrl { get; }
    }

    public interface IPlayoutServerChannelProperties
    {
        string ChannelName { get; set; }
        double MasterVolume { get; set; }
        string LiveDevice { get; set; }
        string PreviewUrl { get; set; }
    }
}
