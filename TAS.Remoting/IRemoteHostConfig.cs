using System.Xml.Serialization;

namespace TAS.Remoting
{
    public interface IRemoteHostConfig
    {
        [XmlAttribute]
        ushort ListenPort { get; set; }
    }
}
