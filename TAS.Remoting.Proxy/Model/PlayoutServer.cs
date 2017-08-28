using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class PlayoutServer : ProxyBase, IPlayoutServer
    {
        public IAnimationDirectory AnimationDirectory { get { return Get<AnimationDirectory>(); } protected set { SetLocalValue(value); } }

        public IEnumerable<IPlayoutServerChannel> Channels { get { return Get<List<PlayoutServerChannel>>(); } protected set { SetLocalValue(value); } }

        public IEnumerable<IRecorder> Recorders{ get { return Get<List<Recorder>>(); } protected set { SetLocalValue(value); } }

        public ulong Id { get { return Get<ulong>(); }  set { SetLocalValue(value); } }

        public bool IsConnected { get { return Get<bool>(); } protected set { SetLocalValue(value); } }

        public IServerDirectory MediaDirectory { get { return Get<ServerDirectory>(); } protected set { SetLocalValue(value); } }

        public string MediaFolder { get { return Get<string>(); } set { SetLocalValue(value); } }

        public string AnimationFolder { get { return Get<string>(); } set { SetLocalValue(value); } }

        public string ServerAddress { get { return Get<string>(); } set { SetLocalValue(value); } }

        public int OscPort { get { return Get<int>(); } set { SetLocalValue(value); } }

        public TServerType ServerType { get { return Get<TServerType>(); } set { SetLocalValue(value); } }

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

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
