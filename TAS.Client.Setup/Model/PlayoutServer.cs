using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup.Model
{
    public class PlayoutServer: IPlayoutServer
    {
        public string ServerAddress { get; set; }
        public string MediaFolder { get; set; }
        public ulong Id { get; set; }
        public TServerType ServerType { get; set; }

        public List<PlayoutServerChannel> _channels;
        
        IEnumerable<IPlayoutServerChannel> IPlayoutServer.Channels
        {
            get { return _channels.Cast<IPlayoutServerChannel>(); }
        }
    }
}
