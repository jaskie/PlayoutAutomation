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
       
        public Type CgType { get; set; }
        
        public byte Id { get; set; }
       
        public string Name { get; set; }
       
        public string ClientImagePath { get; set; }
        
        public string ServerImagePath { get; set; }
        
        public string UploadClientImagePath { get; set; }
       
        public string UploadServerImagePath { get; set; }
       
        public string Command { get; set; }        
    }
}
