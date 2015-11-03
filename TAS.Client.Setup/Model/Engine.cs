using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup.Model
{
    public class Engine : IEngineConfig
    {
        [XmlIgnore]
        public bool IsNew = true;
        [XmlIgnore]
        public ulong Id { get; set; }
        [XmlIgnore]
        public bool Modified = false;
        public TAspectRatioControl AspectRatioControl { get; set; }
        public string EngineName { get; set; }
        public int TimeCorrection { get; set; }
        public TVideoFormat VideoFormat { get; set; }
        public ulong Instance { get; set; }
        public ulong IdServerPGM { get; set; }
        public int ServerChannelPGM { get; set; }
        public ulong IdServerPRV { get; set; }
        public int ServerChannelPRV { get; set; }
        public ulong IdArchive { get; set; }
        public Gpi Gpi { get; set; }
        public RemoteHost Remote { get; set; }
        internal List<CasparServer> Servers;
    }
}
