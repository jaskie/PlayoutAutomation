using System.Collections.Generic;
using System.Xml.Serialization;

namespace TAS.Client.Config.Model
{
    public class CgElementsController
    {       
        [XmlAttribute]
        public string EngineName { get; set; }
        [XmlArray]
        [XmlArrayItem("Command")]
        public List<string> Startup { get; set; }        
        [XmlArray]
        [XmlArrayItem("Crawl")]
        public List<CgElement> Crawls { get; set; }
        [XmlArray]
        [XmlArrayItem("Logo")]
        public List<CgElement> Logos { get; set; }
        [XmlArray]
        [XmlArrayItem("Parental")]
        public List<CgElement> Parentals { get; set; }
        [XmlArray]
        [XmlArrayItem("Aux")]
        public List<CgElement> Auxes { get; set; }


        public CgElementsController()
        {
            Crawls = new List<CgElement>();
            Logos = new List<CgElement>();
            Parentals = new List<CgElement>();
            Auxes = new List<CgElement>();
        }
    }
}
