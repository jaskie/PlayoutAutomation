using System;
using System.Drawing;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElement : ProxyObjectBase, ICGElement
    {
        #pragma warning disable CS0649

        [DtoMember(nameof(ICGElement.Id))]
        private byte _id;

        [DtoMember(nameof(ICGElement.Image))]
        private Bitmap _image;

        [DtoMember(nameof(ICGElement.ImageFile))]
        private string _imageFile;

        [DtoMember(nameof(ICGElement.Name))]
        private string _name;

        #pragma warning restore

        public byte Id => _id;
        
        public Bitmap Image => _image;

        public string ImageFile => _imageFile;

        public string Name => _name;

    }
}
