using System;
using System.Xml.Serialization;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IEngineConfig: IPersistent
    {
        TAspectRatioControl AspectRatioControl { get; set; }
        string EngineName { get; set; }
        int TimeCorrection { get; set; }
        TVideoFormat VideoFormat { get; set; }
        double VolumeReferenceLoudness { get; set; }
        bool EnableGPIForNewEvents { get; set; }
        bool EnableGPICrawlForShows { get; set; }
        [XmlIgnore]
        ulong Instance { get; set; }
        [XmlIgnore]
        ulong IdServerPRI { get; set; }
        [XmlIgnore]
        int ServerChannelPRI { get; set; }
        [XmlIgnore]
        ulong IdServerSEC { get; set; }
        [XmlIgnore]
        int ServerChannelSEC { get; set; }
        [XmlIgnore]
        ulong IdServerPRV { get; set; }
        [XmlIgnore]
        int ServerChannelPRV { get; set; }
        [XmlIgnore]
        ulong IdArchive { get; set; }
    }
}
