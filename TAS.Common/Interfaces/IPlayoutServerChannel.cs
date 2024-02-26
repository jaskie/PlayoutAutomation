using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IPlayoutServerChannel : INotifyPropertyChanged
    {
        int Id { get; }
        bool IsServerConnected { get; }
        int AudioLevel { get; }
        string ChannelName { get; }
        TVideoFormat VideoFormat { get; }
        TMovieContainerFormat MovieContainerFormat { get; }
        string PreviewUrl { get; }
        int AudioChannelCount { get; }
    }

    public interface IPlayoutServerChannelProperties
    {
        string ChannelName { get; set; }
        double MasterVolume { get; set; }
        string LiveDevice { get; set; }
        string PreviewUrl { get; set; }
        int AudioChannelCount { get; set; }
    }
}
