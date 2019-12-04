using jNet.RPC;
using jNet.RPC.Client;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class PlayoutServerChannel : ProxyBase, IPlayoutServerChannel
    {
#pragma warning disable CS0649

        [JsonProperty(nameof(IPlayoutServerChannel.ChannelName))]
        private string _channelName;

        [JsonProperty(nameof(IPlayoutServerChannel.Id))]
        private int _id;

        [JsonProperty(nameof(IPlayoutServerChannel.IsServerConnected))]
        private bool _isServerConnected;

        [JsonProperty(nameof(IPlayoutServerChannel.VideoFormat))]
        private TVideoFormat _videoFormat;

        [JsonProperty(nameof(IPlayoutServerChannel.AudioLevel))]
        private int _audioLevel;

        [JsonProperty(nameof(IPlayoutServerChannel.PreviewUrl))]
        private string _previewUrl;

        [JsonProperty(nameof(IPlayoutServerChannel.AudioChannelCount))]
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
