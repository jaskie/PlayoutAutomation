using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    [DtoType(typeof(IPlayoutServerChannel))]
    public class PlayoutServerChannel : ProxyObjectBase, IPlayoutServerChannel
    {
#pragma warning disable CS0649

        [DtoMember(nameof(IPlayoutServerChannel.ChannelName))]
        private string _channelName;

        [DtoMember(nameof(IPlayoutServerChannel.Id))]
        private int _id;

        [DtoMember(nameof(IPlayoutServerChannel.IsServerConnected))]
        private bool _isServerConnected;

        [DtoMember(nameof(IPlayoutServerChannel.VideoFormat))]
        private TVideoFormat _videoFormat;

        [DtoMember(nameof(IPlayoutServerChannel.AudioLevel))]
        private int _audioLevel;

        [DtoMember(nameof(IPlayoutServerChannel.PreviewUrl))]
        private string _previewUrl;

        [DtoMember(nameof(IPlayoutServerChannel.AudioChannelCount))]
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
