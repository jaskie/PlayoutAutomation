using System;
using System.Windows.Media;

namespace TAS.Common
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ColorAttribute: Attribute
    {
        public ColorAttribute(uint color)
        {
            Color = Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);
        }
        public Color Color { get; }
    }
}
