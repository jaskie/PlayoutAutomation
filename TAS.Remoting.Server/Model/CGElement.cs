using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElement : ProxyBase, ICGElement
    {
        public byte Id { get { return Get<byte>(); } set { SetLocalValue(value); } }

        public BitmapImage Image { get { return Get<BitmapImage>(); } set { SetLocalValue(value); } }

        public string ImageFile { get { return Get<string>(); } set { SetLocalValue(value); } }

        public string Name { get { return Get<string>(); }  set { SetLocalValue(value); } }

        protected override void OnEventNotification(WebSocketMessage e) { }
    }
}
