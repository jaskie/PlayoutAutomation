using System.Xml.Serialization;
using jNet.RPC;
using TAS.Common.Database;

namespace TAS.Client.Config.Model
{
    public class RemoteHost: IRemoteHostConfig
    {
        [XmlAttribute, Hibernate]
        public ushort ListenPort {get; set;}
    }
}
