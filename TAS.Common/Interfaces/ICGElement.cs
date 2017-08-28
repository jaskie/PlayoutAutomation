using System.Drawing;

namespace TAS.Common.Interfaces
{
    public interface ICGElement
    {
        byte Id { get; }
        string Name { get; }
        string ImageFile { get; }
        Bitmap Image { get; }
    }
}
