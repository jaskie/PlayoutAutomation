using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Interfaces;

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

        [Hibernate]
        public TAspectRatioControl AspectRatioControl { get; set; }

        [Hibernate]
        public string EngineName { get; set; }

        [Hibernate]
        public int TimeCorrection { get; set; }

        [Hibernate]
        public TVideoFormat VideoFormat { get; set; }

        [Hibernate]
        public RemoteHost Remote { get; set; }

        [Hibernate]
        public bool EnableCGElementsForNewEvents { get; set; }

        [Hibernate]
        public bool StudioMode { get; set; }

        [Hibernate]
        public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }

        [Hibernate]
        public int CGStartDelay { get; set; }

        [XmlIgnore]
        public bool IsModified = false;

        [XmlIgnore]
        public bool IsNew = true;

        [XmlIgnore]
        public IDictionary<string, int> FieldLengths { get; set; }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        internal List<CasparServer> Servers;

        internal ArchiveDirectories ArchiveDirectories;

    }
}
