using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    [DtoType(typeof(IPlayoutServer))]
    public class PlayoutServer : ProxyObjectBase, IPlayoutServer
    {
        #pragma warning disable CS0649
        [DtoMember(nameof(IPlayoutServer.AnimationDirectory))]
        private IAnimationDirectory _animationDirectory;

        [DtoMember(nameof(IPlayoutServer.Channels))]
        private List<PlayoutServerChannel> _channels;

        [DtoMember(nameof(IPlayoutServer.Recorders))]
        private List<Recorder> _recorders;

        [DtoMember(nameof(IPlayoutServer.IsConnected))]
        private bool _isConnected;

        [DtoMember(nameof(IPlayoutServer.MediaDirectory))]
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
