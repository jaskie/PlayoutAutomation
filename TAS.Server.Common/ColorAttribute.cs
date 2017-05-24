using System;
using System.Windows.Media;

namespace TAS.Server.Common
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple=false)]
    public class ColorAttribute: Attribute
    {
        protected Color _color;
        public ColorAttribute(UInt32 color)
        {
            _color = Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);
        }
        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }
    }
}
