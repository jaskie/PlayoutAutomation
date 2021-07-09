using System.Drawing;

namespace TAS.Common.Interfaces
{
    public interface ICGElement
    {
        byte Id { get; }
        string Name { get; }        
        Bitmap Thumbnail { get; }
    }
}
