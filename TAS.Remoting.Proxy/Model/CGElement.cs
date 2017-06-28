using Newtonsoft.Json;
using System.Drawing;
using TAS.Remoting.Client;
using TAS.Server.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElement : ProxyBase, ICGElement
    {
        public byte Id { get { return Get<byte>(); } set { SetLocalValue(value); } }

        [JsonConverter(typeof(BitmapConverter))]
        public Bitmap Image { get { return Get<Bitmap>(); } set { SetLocalValue(value); } }

        public string ImageFile { get { return Get<string>(); } set { SetLocalValue(value); } }

        public string Name { get { return Get<string>(); }  set { SetLocalValue(value); } }

        protected override void OnEventNotification(WebSocketMessage e) { }
    }
}
