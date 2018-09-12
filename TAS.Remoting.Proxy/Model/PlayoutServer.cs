using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class PlayoutServer : ProxyBase, IPlayoutServer
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

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        protected override void OnEventNotification(SocketMessage message) { }

    }
}
