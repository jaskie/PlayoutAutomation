using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;


namespace TAS.Client.Setup.Model
{
    public class RemoteHost: IRemoteHostConfig
    {
        [XmlAttribute]
        public string EndpointAddress {get; set;}
    }
}
