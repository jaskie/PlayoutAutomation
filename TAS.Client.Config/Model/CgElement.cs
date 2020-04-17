using System.Drawing;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CgElement : ICGElement
    {
        public byte Id { get; set; }

        public string Name { get; set; }

        public string ImageFile { get; set; }

        public Bitmap Image { get; set; }
    }
}
