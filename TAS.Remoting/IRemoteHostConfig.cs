using System.Xml.Serialization;

namespace TAS.Common.Interfaces
{
    public interface IRemoteHostConfig
    {
        [XmlAttribute]
        ushort ListenPort { get; set; }
    }
}
