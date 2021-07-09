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

        [DtoMember(nameof(ICGElement.Thumbnail))]
        private Bitmap _image;

        [DtoMember(nameof(ICGElement.Name))]
        private string _name;

        #pragma warning restore

        public byte Id => _id;
        
        public Bitmap Thumbnail => _image;

        public string Name => _name;

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
