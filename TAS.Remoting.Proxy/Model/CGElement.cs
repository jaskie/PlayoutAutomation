using Newtonsoft.Json;
using System.Drawing;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElement : ProxyObjectBase, ICGElement
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(ICGElement.Id))]
        private byte _id;

        [JsonProperty(nameof(ICGElement.Image)), JsonConverter(typeof(BitmapJsonConverter))]
        private Bitmap _image;

        [JsonProperty(nameof(ICGElement.ImageFile))]
        private string _imageFile;

        [JsonProperty(nameof(ICGElement.Name))]
        private string _name;

        #pragma warning restore

        public byte Id => _id;
        
        public Bitmap Image => _image;

        public string ImageFile => _imageFile;

        public string Name => _name;

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
