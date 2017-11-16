using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Network.Osc
{
    public struct RGBA
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public RGBA(byte red, byte green, byte blue, byte alpha)
        {
            this.R = red;
            this.G = green;
            this.B = blue;
            this.A = alpha;
        }

        public override bool Equals(System.Object obj)
        {
            if (obj.GetType() == typeof(RGBA))
            {
                if (this.R == ((RGBA)obj).R && this.G == ((RGBA)obj).G && this.B == ((RGBA)obj).B && this.A == ((RGBA)obj).A)
                    return true;
                else
                    return false;
            }
            else if (obj.GetType() == typeof(byte[]))
            {
                if (this.R == ((byte[])obj)[0] && this.G == ((byte[])obj)[1] && this.B == ((byte[])obj)[2] && this.A == ((byte[])obj)[3])
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public static bool operator ==(RGBA a, RGBA b)
        {
            if (a.Equals(b))
                return true;
            else
                return false;
        }

        public static bool operator !=(RGBA a, RGBA b)
        {
            if (!a.Equals(b))
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return (R << 24) + (G << 16) + (B << 8) + (A);
        }
    }
}
