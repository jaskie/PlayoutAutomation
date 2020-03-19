using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    public class PlayoutServer : ProxyObjectBase, IPlayoutServer
    {
        #pragma warning disable CS0649
        [JsonProperty(nameof(IPlayoutServer.AnimationDirectory))]
        private IAnimationDirectory _animationDirectory;

        [JsonProperty(nameof(IPlayoutServer.Channels))]
        private List<PlayoutServerChannel> _channels;

        [JsonProperty(nameof(IPlayoutServer.Recorders))]
        private List<Recorder> _recorders;

        [JsonProperty(nameof(IPlayoutServer.IsConnected))]
        private bool _isConnected;

        [JsonProperty(nameof(IPlayoutServer.MediaDirectory))]
        private ServerDirectory _mediaDirectory;

        #pragma warning restore

        public IAnimationDirectory AnimationDirectory => _animationDirectory;

        public IEnumerable<IPlayoutServerChannel> Channels => _channels;

        public IEnumerable<IRecorder> Recorders => _recorders;

        public bool IsConnected => _isConnected;

        public IServerDirectory MediaDirectory => _mediaDirectory;

        protected override void OnEventNotification(SocketMessage message) { }

    }
}
