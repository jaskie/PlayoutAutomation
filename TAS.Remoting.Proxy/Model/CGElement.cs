using Newtonsoft.Json;
using System.Drawing;
using TAS.Remoting.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElement : ProxyBase, ICGElement
    {
        [JsonProperty(nameof(ICGElement.Id))]
        private byte _id;

        [JsonProperty(nameof(ICGElement.Image)), JsonConverter(typeof(BitmapConverter))]
        private Bitmap _image;

        [JsonProperty(nameof(ICGElement.ImageFile))]
        private string _imageFile;

        [JsonProperty(nameof(ICGElement.Name))]
        private string _name;

        public byte Id => _id;
        
        public Bitmap Image => _image;

        public string ImageFile => _imageFile;

        public string Name => _name;

        protected override void OnEventNotification(WebSocketMessage e) { }
    }
}
