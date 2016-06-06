using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Server
{
    public class GPIState
    {
        [XmlIgnore]
        public object SyncRoot = new object();
        [XmlIgnore]
        public System.Net.EndPoint EndPoint {
            set { Address = value.ToString(); }
        }
        public string Address;
        public bool AspectNarrow;
        public byte ConfigNr;
        public bool CrawlVisible;
        public bool LogoVisible;
        public byte LogoStyle;
        public bool Mono;
        public bool ParentalVisible;
        public byte ParentalStyle;
        [XmlArrayItem(ElementName = "Aux")]
        public List<byte> VisibleAuxes = new List<byte>();
        
        public GPIState Clone()
        {
            GPIState newCrawlState = (GPIState)this.MemberwiseClone();
            newCrawlState.VisibleAuxes = new List<byte>(VisibleAuxes);
            return newCrawlState;
        }

    };
}
