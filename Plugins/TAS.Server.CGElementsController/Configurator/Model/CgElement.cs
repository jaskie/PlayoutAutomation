using System.Drawing;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.CgElementsController.Configurator.Model
{    
    public class CgElement : ICGElement
    {
        public enum Type
        {
            Crawl,
            Logo,
            Parental,
            Aux
        }
        public Type CgType { get; set; }
        [Hibernate]
        public byte Id { get; set; }
        [Hibernate]
        public string Name { get; set; }
        [Hibernate]
        public string ClientImagePath { get; set; }
        [Hibernate]
        public string ServerImagePath { get; set; }

        public string UploadClientImagePath { get; set; }

        public string UploadServerImagePath { get; set; }
        [Hibernate]
        public string Command { get; set; }
        [Hibernate]
        public string ImageFile { get; set; }

        public Bitmap Image { get; }
        public CgElement()
        {

        }
    }
}
