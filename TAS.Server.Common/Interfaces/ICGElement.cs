using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace TAS.Server.Interfaces
{
    public interface ICGElement
    {
        byte Id { get; }
        string Name { get; }
        string ImageFile { get; }
        BitmapImage Image { get; }
    }
}
