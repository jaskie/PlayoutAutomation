using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Network.Osc
{
    public struct Midi
    {
        public byte Port;
        public byte Status;
        public byte Data1;
        public byte Data2;

        public Midi(byte port, byte status, byte data1, byte data2)
        {
            this.Port = port;
            this.Status = status;
            this.Data1 = data1;
            this.Data2 = data2;
        }

        public override bool Equals(System.Object obj)
        {
            if (obj.GetType() == typeof(Midi))
            {
                if (this.Port == ((Midi)obj).Port && this.Status == ((Midi)obj).Status && this.Data1 == ((Midi)obj).Data1 && this.Data2 == ((Midi)obj).Data2)
                    return true;
                else
                    return false;
            }
            else if (obj.GetType() == typeof(byte[]))
            {
                if (this.Port == ((byte[])obj)[0] && this.Status == ((byte[])obj)[1] && this.Data1 == ((byte[])obj)[2] && this.Data2 == ((byte[])obj)[3])
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public static bool operator ==(Midi a, Midi b)
        {
            if (a.Equals(b))
                return true;
            else
                return false;
        }

        public static bool operator !=(Midi a, Midi b)
        {
            if (!a.Equals(b))
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return (Port << 24) + (Status << 16) + (Data1 << 8) + (Data2);
        }
    }
}
