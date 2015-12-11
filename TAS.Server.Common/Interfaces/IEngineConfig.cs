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
        [XmlIgnore]
        ulong Instance { get; set; }
        [XmlIgnore]
        ulong IdServerPGM { get; set; }
        [XmlIgnore]
        int ServerChannelPGM { get; set; }
        [XmlIgnore]
        ulong IdServerPRV { get; set; }
        [XmlIgnore]
        int ServerChannelPRV { get; set; }
        [XmlIgnore]
        ulong IdArchive { get; set; }
    }
}
