using System.Drawing;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElement : ProxyObjectBase, ICGElement
    {
        #pragma warning disable CS0649

        [DtoField(nameof(ICGElement.Id))]
        private byte _id;

        [DtoField(nameof(ICGElement.Image))]
        private Bitmap _image;

        [DtoField(nameof(ICGElement.ImageFile))]
        private string _imageFile;

        [DtoField(nameof(ICGElement.Name))]
        private string _name;

        #pragma warning restore

        public byte Id => _id;
        
        public Bitmap Image => _image;

        public string ImageFile => _imageFile;

        public string Name => _name;

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
