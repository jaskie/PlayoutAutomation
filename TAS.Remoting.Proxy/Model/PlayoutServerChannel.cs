using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class PlayoutServerChannel : ProxyObjectBase, IPlayoutServerChannel
    {
#pragma warning disable CS0649

        [DtoField(nameof(IPlayoutServerChannel.ChannelName))]
        private string _channelName;

        [DtoField(nameof(IPlayoutServerChannel.Id))]
        private int _id;

        [DtoField(nameof(IPlayoutServerChannel.IsServerConnected))]
        private bool _isServerConnected;

        [DtoField(nameof(IPlayoutServerChannel.VideoFormat))]
        private TVideoFormat _videoFormat;

        [DtoField(nameof(IPlayoutServerChannel.AudioLevel))]
        private int _audioLevel;

        [DtoField(nameof(IPlayoutServerChannel.PreviewUrl))]
        private string _previewUrl;

        [DtoField(nameof(IPlayoutServerChannel.AudioChannelCount))]
        private int _audioChannelCount;

#pragma warning restore

        public string ChannelName => _channelName;

        public int Id => _id;

        public bool IsServerConnected => _isServerConnected;

        public TVideoFormat VideoFormat => _videoFormat;

        public int AudioLevel => _audioLevel;

        public string PreviewUrl => _previewUrl;

        public int AudioChannelCount => _audioChannelCount;

        protected override void OnEventNotification(SocketMessage message) { }

    }
}
