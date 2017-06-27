using System.Xml.Serialization;
using TAS.Server.Common.Interfaces;


namespace TAS.Client.Config.Model
{
    public class RemoteHost: IRemoteHostConfig
    {
        [XmlAttribute]
        public ushort ListenPort {get; set;}
    }
}
