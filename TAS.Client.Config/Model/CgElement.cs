using System.Drawing;
using System.Xml.Serialization;

namespace TAS.Client.Config.Model
{
    public class CgElement
    {
        public enum Type
        {
            Crawl,
            Logo,
            Parental,
            Aux
        };
        [XmlIgnore]
        public Type CgType { get; set; }
        [XmlAttribute]
        public byte Id { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string ClientImagePath { get; set; }
        [XmlAttribute]
        public string ServerImagePath { get; set; }
        [XmlAttribute]
        public string UploadClientImagePath { get; set; }
        [XmlAttribute]
        public string UploadServerImagePath { get; set; }
        [XmlAttribute]
        public string Command { get; set; }       
    }
}
