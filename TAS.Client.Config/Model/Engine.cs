using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class Engine: IEnginePersistent
    {
        // Ignored properties are readed from DB fields
        [XmlIgnore]
        public ulong Id { get; set; }
        [XmlIgnore]
        public ulong Instance { get; set; }
        [XmlIgnore]
        public ulong IdServerPRI { get; set; }
        [XmlIgnore]
        public int ServerChannelPRI { get; set; }
        [XmlIgnore]
        public ulong IdServerSEC { get; set; }
        [XmlIgnore]
        public int ServerChannelSEC { get; set; }
        [XmlIgnore]
        public ulong IdServerPRV { get; set; }
        [XmlIgnore]
        public int ServerChannelPRV { get; set; }
        [XmlIgnore]
        public ulong IdArchive { get; set; }

        public TAspectRatioControl AspectRatioControl { get; set; }
        public string EngineName { get; set; }
        public int TimeCorrection { get; set; }
        public TVideoFormat VideoFormat { get; set; }
        public double VolumeReferenceLoudness { get; set; }
        public RemoteHost Remote { get; set; }
        public bool EnableCGElementsForNewEvents { get; set; }
        public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }
        public int CGStartDelay { get; set; }

        internal List<CasparServer> Servers;
        internal ArchiveDirectories ArchiveDirectories;
        [XmlIgnore]
        public bool IsModified = false;
        [XmlIgnore]
        public bool IsNew = true;
    }
}
