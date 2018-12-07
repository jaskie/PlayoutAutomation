using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PIEHid64Net;
using TAS.Common.Interfaces;

namespace TAS.Client.XKeys
{
    public class XKey
    {
        [XmlAttribute]
        public string EngineName { get; set; }

        internal IEngine Engine { get; set; }

        internal void SetupDevice()
        {
            var devices = PIEDevice.EnumeratePIE().Where(d => d.HidUsagePage == 0xC);

        }


    }
}
