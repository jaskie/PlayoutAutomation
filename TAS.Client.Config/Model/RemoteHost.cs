using System.Xml.Serialization;
using jNet.RPC;


namespace TAS.Client.Config.Model
{
    public class RemoteHost: IRemoteHostConfig
    {
        [XmlAttribute]
        public ushort ListenPort {get; set;}
    }
}
