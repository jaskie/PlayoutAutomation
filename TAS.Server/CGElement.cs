using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    [DebuggerDisplay("{Id}:{Name}")]
    [XmlType(nameof(ICGElementsState.Crawl))]
    public class CGElement : Remoting.Server.DtoBase, ICGElement
    {
        [XmlAttribute]
        public byte Id { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
    }
}
