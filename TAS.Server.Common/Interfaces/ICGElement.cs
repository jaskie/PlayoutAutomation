using System.Drawing;

namespace TAS.Server.Common.Interfaces
{
    public interface ICGElement
    {
        byte Id { get; }
        string Name { get; }
        string ImageFile { get; }
        Bitmap Image { get; }
    }
}
