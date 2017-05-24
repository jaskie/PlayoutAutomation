using System.Xml.Serialization;

namespace TAS.Server.Common.Interfaces
{
    public interface IRemoteHostConfig
    {
        [XmlAttribute]
        ushort ListenPort { get; set; }
    }
}
