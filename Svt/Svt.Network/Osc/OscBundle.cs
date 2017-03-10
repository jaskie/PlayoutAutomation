using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Network.Osc
{
    public class OscBundle : OscPacket
    {
        Timetag _timetag;

        public UInt64 Timetag
        {
            get { return _timetag.Tag; }
            set { _timetag.Tag = value; }
        }

        public DateTime Timestamp
        {
            get { return _timetag.Timestamp; }
            set { _timetag.Timestamp = value; }
        }

        public List<OscMessage> Messages;

        public OscBundle(UInt64 timetag, params OscMessage[] args)
        {
            _timetag = new Timetag(timetag);
            Messages = new List<OscMessage>();
            Messages.AddRange(args);
        }

        public override byte[] GetBytes()
        {
            string bundle = "#bundle";
            int bundleTagLen = Utils.AlignedStringLength(bundle);
            byte[] tag = setULong(_timetag.Tag);

            List<byte[]> outMessages = new List<byte[]>();
            foreach (OscMessage msg in Messages)
            {
                outMessages.Add(msg.GetBytes());
            }

            int len = bundleTagLen + tag.Length + outMessages.Sum(x => x.Length + 4);

            int i = 0;
            byte[] output = new byte[len];
            Encoding.ASCII.GetBytes(bundle).CopyTo(output, i);
            i += bundleTagLen;
            tag.CopyTo(output, i);
            i += tag.Length;

            foreach (byte[] msg in outMessages)
            {
                var size = setInt(msg.Length);
                size.CopyTo(output, i);
                i += size.Length;

                msg.CopyTo(output, i);
                i += msg.Length; // msg size is always a multiple of 4
            }

            return output;
        }

    }
}
