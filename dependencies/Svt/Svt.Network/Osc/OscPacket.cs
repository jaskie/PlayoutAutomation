using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Network.Osc
{
    public abstract class OscPacket
    {
        public static OscPacket GetPacket(byte[] OscData)
        {
            if (OscData[0] == '#')
                return parseBundle(OscData);
            else
                return parseMessage(OscData);
        }

        public abstract byte[] GetBytes();

        #region Parse OSC packages

        /// <summary>
        /// Takes in an OSC bundle package in byte form and parses it into a more usable OscBundle object
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>Message containing various arguments and an address</returns>
        private static OscMessage parseMessage(byte[] msg)
        {
            int index = 0;

            string address = null;
            char[] types = new char[0];
            List<object> arguments = new List<object>();
            List<object> mainArray = arguments; // used as a reference when we are parsing arrays to get the main array back

            // Get address
            address = getAddress(msg, index);
            index += msg.FirstIndexAfter(address.Length, x => x == ',');

            if (index % 4 != 0)
                throw new Exception("Misaligned OSC Packet data. Address string is not padded correctly and does not align to 4 byte interval");

            // Get type tags
            types = getTypes(msg, index);

            index += types.Length;

            while (index % 4 != 0)
                index++;

            bool commaParsed = false;

            foreach (char type in types)
            {
                // skip leading comma
                if (type == ',' && !commaParsed)
                {
                    commaParsed = true;
                    continue;
                }

                switch (type)
                {
                    case ('\0'):
                        break;

                    case ('i'):
                        int intVal = getInt(msg, index);
                        arguments.Add(intVal);
                        index += 4;
                        break;

                    case ('f'):
                        float floatVal = getFloat(msg, index);
                        arguments.Add(floatVal);
                        index += 4;
                        break;

                    case ('s'):
                        string stringVal = getString(msg, index);
                        arguments.Add(stringVal);
                        index += stringVal.Length;
                        break;

                    case ('b'):
                        byte[] blob = getBlob(msg, index);
                        arguments.Add(blob);
                        index += 4 + blob.Length;
                        break;

                    case ('h'):
                        Int64 hval = getLong(msg, index);
                        arguments.Add(hval);
                        index += 8;
                        break;

                    case ('t'):
                        UInt64 sval = getULong(msg, index);
                        arguments.Add(new Timetag(sval));
                        index += 8;
                        break;

                    case ('d'):
                        double dval = getDouble(msg, index);
                        arguments.Add(dval);
                        index += 8;
                        break;

                    case ('S'):
                        string SymbolVal = getString(msg, index);
                        arguments.Add(new Symbol(SymbolVal));
                        index += SymbolVal.Length;
                        break;

                    case ('c'):
                        char cval = getChar(msg, index);
                        arguments.Add(cval);
                        index += 4;
                        break;

                    case ('r'):
                        RGBA rgbaval = getRGBA(msg, index);
                        arguments.Add(rgbaval);
                        index += 4;
                        break;

                    case ('m'):
                        Midi midival = getMidi(msg, index);
                        arguments.Add(midival);
                        index += 4;
                        break;

                    case ('T'):
                        arguments.Add(true);
                        break;

                    case ('F'):
                        arguments.Add(false);
                        break;

                    case ('N'):
                        arguments.Add(null);
                        break;

                    case ('I'):
                        arguments.Add(double.PositiveInfinity);
                        break;

                    case ('['):
                        if (arguments != mainArray)
                            throw new Exception("SharopOSC does not support nested arrays");
                        arguments = new List<object>(); // make arguments point to a new object array
                        break;

                    case (']'):
                        mainArray.Add(arguments); // add the array to the main array
                        arguments = mainArray; // make arguments point back to the main array
                        break;

                    default:
                        throw new Exception("OSC type tag '" + type + "' is unknown.");
                }

                while (index % 4 != 0)
                    index++;
            }

            return new OscMessage(address, arguments.ToArray());
        }

        /// <summary>
        /// Takes in an OSC bundle package in byte form and parses it into a more usable OscBundle object
        /// </summary>
        /// <param name="bundle"></param>
        /// <returns>Bundle containing elements and a timetag</returns>
        private static OscBundle parseBundle(byte[] bundle)
        {
            UInt64 timetag;
            List<OscMessage> messages = new List<OscMessage>();

            int index = 0;

            var bundleTag = Encoding.ASCII.GetString(bundle.SubArray(0, 8));
            index += 8;

            timetag = getULong(bundle, index);
            index += 8;

            if (bundleTag != "#bundle\0")
                throw new Exception("Not a bundle");

            while (index < bundle.Length)
            {
                int size = getInt(bundle, index);
                index += 4;

                byte[] messageBytes = bundle.SubArray(index, size);
                var message = parseMessage(messageBytes);

                messages.Add(message);

                index += size;
                while (index % 4 != 0)
                    index++;
            }

            OscBundle output = new OscBundle(timetag, messages.ToArray());
            return output;
        }

        #endregion

        #region Get arguments from byte array

        private static string getAddress(byte[] msg, int index)
        {
            int i = index;
            string address = "";
            for (; i < msg.Length; i += 4)
            {
                if (msg[i] == ',')
                {
                    if (i == 0)
                        return "";

                    address = Encoding.ASCII.GetString(msg.SubArray(index, i - 1));
                    break;
                }
            }

            if (i >= msg.Length && address == null)
                throw new Exception("no comma found");

            return address.Replace("\0", "");
        }

        private static char[] getTypes(byte[] msg, int index)
        {
            int i = index + 4;
            char[] types = null;

            for (; i < msg.Length; i += 4)
            {
                if (msg[i - 1] == 0)
                {
                    types = Encoding.ASCII.GetChars(msg.SubArray(index, i - index));
                    break;
                }
            }

            if (i >= msg.Length && types == null)
                types = new char[0];

            return types;
        }

        private static int getInt(byte[] msg, int index)
        {
            int val = (msg[index] << 24) + (msg[index + 1] << 16) + (msg[index + 2] << 8) + (msg[index + 3] << 0);
            return val;
        }

        private static float getFloat(byte[] msg, int index)
        {
            byte[] reversed = new byte[4];
            reversed[3] = msg[index];
            reversed[2] = msg[index + 1];
            reversed[1] = msg[index + 2];
            reversed[0] = msg[index + 3];
            float val = System.BitConverter.ToSingle(reversed, 0);
            return val;
        }

        private static string getString(byte[] msg, int index)
        {
            string output = null;
            int i = index + 4;
            for (; (i - 1) < msg.Length; i += 4)
            {
                if (msg[i - 1] == 0)
                {
                    output = Encoding.ASCII.GetString(msg.SubArray(index, i - index));
                    break;
                }
            }

            if (i >= msg.Length && output == null)
                throw new Exception("No null terminator after type string");

            return output.Replace("\0", "");
        }

        private static byte[] getBlob(byte[] msg, int index)
        {
            int size = getInt(msg, index);
            return msg.SubArray(index + 4, size);
        }

        private static UInt64 getULong(byte[] msg, int index)
        {
            UInt64 val = ((UInt64)msg[index] << 56) + ((UInt64)msg[index + 1] << 48) + ((UInt64)msg[index + 2] << 40) + ((UInt64)msg[index + 3] << 32)
                    + ((UInt64)msg[index + 4] << 24) + ((UInt64)msg[index + 5] << 16) + ((UInt64)msg[index + 6] << 8) + ((UInt64)msg[index + 7] << 0);
            return val;
        }

        private static Int64 getLong(byte[] msg, int index)
        {
            byte[] var = new byte[8];
            var[7] = msg[index];
            var[6] = msg[index + 1];
            var[5] = msg[index + 2];
            var[4] = msg[index + 3];
            var[3] = msg[index + 4];
            var[2] = msg[index + 5];
            var[1] = msg[index + 6];
            var[0] = msg[index + 7];

            Int64 val = BitConverter.ToInt64(var, 0);
            return val;
        }

        private static double getDouble(byte[] msg, int index)
        {
            byte[] var = new byte[8];
            var[7] = msg[index];
            var[6] = msg[index + 1];
            var[5] = msg[index + 2];
            var[4] = msg[index + 3];
            var[3] = msg[index + 4];
            var[2] = msg[index + 5];
            var[1] = msg[index + 6];
            var[0] = msg[index + 7];

            double val = BitConverter.ToDouble(var, 0);
            return val;
        }

        private static char getChar(byte[] msg, int index)
        {
            return (char)msg[index + 3];
        }

        private static RGBA getRGBA(byte[] msg, int index)
        {
            return new RGBA(msg[index], msg[index + 1], msg[index + 2], msg[index + 3]);
        }

        private static Midi getMidi(byte[] msg, int index)
        {
            return new Midi(msg[index], msg[index + 1], msg[index + 2], msg[index + 3]);
        }

        #endregion

        #region Create byte arrays for arguments

        protected static byte[] setInt(int value)
        {
            byte[] msg = new byte[4];

            var bytes = BitConverter.GetBytes(value);
            msg[0] = bytes[3];
            msg[1] = bytes[2];
            msg[2] = bytes[1];
            msg[3] = bytes[0];

            return msg;
        }

        protected static byte[] setFloat(float value)
        {
            byte[] msg = new byte[4];

            var bytes = BitConverter.GetBytes(value);
            msg[0] = bytes[3];
            msg[1] = bytes[2];
            msg[2] = bytes[1];
            msg[3] = bytes[0];

            return msg;
        }

        protected static byte[] setString(string value)
        {
            int len = value.Length + (4 - value.Length % 4);
            if (len <= value.Length) len = len + 4;

            byte[] msg = new byte[len];

            var bytes = Encoding.ASCII.GetBytes(value);
            bytes.CopyTo(msg, 0);

            return msg;
        }

        protected static byte[] setBlob(byte[] value)
        {
            int len = value.Length + 4;
            len = len + (4 - len % 4);

            byte[] msg = new byte[len];
            byte[] size = setInt(value.Length);
            size.CopyTo(msg, 0);
            value.CopyTo(msg, 4);
            return msg;
        }

        protected static byte[] setLong(Int64 value)
        {
            byte[] rev = BitConverter.GetBytes(value);
            byte[] output = new byte[8];
            output[0] = rev[7];
            output[1] = rev[6];
            output[2] = rev[5];
            output[3] = rev[4];
            output[4] = rev[3];
            output[5] = rev[2];
            output[6] = rev[1];
            output[7] = rev[0];
            return output;
        }

        protected static byte[] setULong(UInt64 value)
        {
            byte[] rev = BitConverter.GetBytes(value);
            byte[] output = new byte[8];
            output[0] = rev[7];
            output[1] = rev[6];
            output[2] = rev[5];
            output[3] = rev[4];
            output[4] = rev[3];
            output[5] = rev[2];
            output[6] = rev[1];
            output[7] = rev[0];
            return output;
        }

        protected static byte[] setDouble(double value)
        {
            byte[] rev = BitConverter.GetBytes(value);
            byte[] output = new byte[8];
            output[0] = rev[7];
            output[1] = rev[6];
            output[2] = rev[5];
            output[3] = rev[4];
            output[4] = rev[3];
            output[5] = rev[2];
            output[6] = rev[1];
            output[7] = rev[0];
            return output;
        }

        protected static byte[] setChar(char value)
        {
            byte[] output = new byte[4];
            output[0] = 0;
            output[1] = 0;
            output[2] = 0;
            output[3] = (byte)value;
            return output;
        }

        protected static byte[] setRGBA(RGBA value)
        {
            byte[] output = new byte[4];
            output[0] = value.R;
            output[1] = value.G;
            output[2] = value.B;
            output[3] = value.A;
            return output;
        }

        protected static byte[] setMidi(Midi value)
        {
            byte[] output = new byte[4];
            output[0] = value.Port;
            output[1] = value.Status;
            output[2] = value.Data1;
            output[3] = value.Data2;
            return output;
        }

        #endregion

    }
}