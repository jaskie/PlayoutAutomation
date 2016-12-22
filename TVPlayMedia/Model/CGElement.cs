using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class CGElement : ProxyBase, ICGElement
    {
        public byte Id { get { return Get<byte>(); } set { SetField(value); } }

        public BitmapImage Image { get { return Get<BitmapImage>(); } }

        public string ImageFile { get { return Get<string>(); } }

        public string Name { get { return Get<string>(); }  set { SetField(value); } }
    }
}
