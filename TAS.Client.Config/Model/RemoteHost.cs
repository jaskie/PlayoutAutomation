using System.Xml.Serialization;
using TAS.Server.Common.Interfaces;


namespace TAS.Client.Config.Model
{
    public class RemoteHost: IRemoteHostConfig
    {
        public ushort ListenPort {get; set;}
    }
}
