using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class PlayoutServer : ProxyBase, IPlayoutServer
    {
        public IAnimationDirectory AnimationDirectory { get { return Get<AnimationDirectory>(); } protected set { SetField(value); } }

        public List<IPlayoutServerChannel> Channels { get { return Get<List<PlayoutServerChannel>>().Cast<IPlayoutServerChannel>().ToList(); } protected set { SetField(value); } }

        public ulong Id { get { return Get<ulong>(); }  set { SetField(value); } }

        public bool IsConnected { get { return Get<bool>(); } protected set { SetField(value); } }

        public IServerDirectory MediaDirectory { get { return Get<ServerDirectory>(); } protected set { SetField(value); } }

        public string MediaFolder { get { return Get<string>(); } set { SetField(value); } }

        public string AnimationFolder { get { return Get<string>(); } set { SetField(value); } }

        public string ServerAddress { get { return Get<string>(); } set { SetField(value); } }

        public void Initialize()
        {
            throw new NotImplementedException();
        }
    }
}
