using System.Drawing;
using System.Xml.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CgElement : ICGElement
    {
        public enum Type
        {
            Crawl,
            Logo,
            Parental,
            Aux
        };
        [XmlAttribute]
        public byte Id { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string ImageFile { get; set; }
        [XmlAttribute]
        public string Command { get; set; }
        [XmlIgnore]
        public Bitmap Image { get; set; }
    }
}
