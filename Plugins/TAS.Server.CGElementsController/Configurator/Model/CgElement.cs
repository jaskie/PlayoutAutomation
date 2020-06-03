using System.Drawing;
using TAS.Common.Interfaces;

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

        public byte Id { get; set; }

        public string Name { get; set; }

        public string ClientImagePath { get; set; }

        public string ServerImagePath { get; set; }

        public string UploadClientImagePath { get; set; }

        public string UploadServerImagePath { get; set; }

        public string Command { get; set; }

        #region ICGElement
        public string ImageFile => throw new System.NotImplementedException();

        public Bitmap Image => throw new System.NotImplementedException();
        #endregion
    }
}
