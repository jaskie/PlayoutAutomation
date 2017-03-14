using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class PlayoutServer : ProxyBase, IPlayoutServer
    {
        public IAnimationDirectory AnimationDirectory { get { return Get<AnimationDirectory>(); } protected set { SetLocalValue(value); } }

        public List<IPlayoutServerChannel> Channels { get { return Get<List<PlayoutServerChannel>>().Cast<IPlayoutServerChannel>().ToList(); } protected set { SetLocalValue(value); } }

        public List<IRecorder> Recorders{ get { return Get<List<Recorder>>().Cast<IRecorder>().ToList(); } protected set { SetLocalValue(value); } }

        public ulong Id { get { return Get<ulong>(); }  set { SetLocalValue(value); } }

        public bool IsConnected { get { return Get<bool>(); } protected set { SetLocalValue(value); } }

        public IServerDirectory MediaDirectory { get { return Get<ServerDirectory>(); } protected set { SetLocalValue(value); } }

        public string MediaFolder { get { return Get<string>(); } set { SetLocalValue(value); } }

        public string AnimationFolder { get { return Get<string>(); } set { SetLocalValue(value); } }

        public string ServerAddress { get { return Get<string>(); } set { SetLocalValue(value); } }

        public TServerType ServerType { get { return Get<TServerType>(); } set { SetLocalValue(value); } }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
