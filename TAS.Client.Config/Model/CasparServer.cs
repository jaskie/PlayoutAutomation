using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;
using System.ComponentModel;

namespace TAS.Client.Config.Model
{
    public class CasparServer: IPlayoutServer
    {
        public CasparServer()
        {
            Channels = new List<IPlayoutServerChannel>();
        }
        [XmlIgnore]
        public bool IsNew = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ServerAddress { get; set; }
        public string MediaFolder { get; set; }
        [XmlIgnore]
        public ulong Id { get; set; }

        public TServerType ServerType { get; set; }

        [XmlArrayItem("CasparServerChannel", Type = typeof(CasparServerChannel))]
        public List<IPlayoutServerChannel> Channels { get; set; }

        #region notImplemented
        public bool IsConnected { get { throw new NotImplementedException(); } }
        public IServerDirectory MediaDirectory { get { throw new NotImplementedException(); } }
        public IAnimationDirectory AnimationDirectory { get { throw new NotImplementedException(); } }
        public void Initialize() { throw new NotImplementedException(); }
        #endregion

        public override string ToString()
        {
            return ServerAddress;
        }

    }

}
