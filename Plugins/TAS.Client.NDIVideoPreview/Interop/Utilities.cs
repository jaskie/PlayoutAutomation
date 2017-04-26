using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.NDIVideoPreview.Interop
{
    public static class Utilities
    {
        // This REQUIRES you to use Marshal.FreeHGlobal() on the returned pointer!
        public static IntPtr StringToUtf8(String managedString)
        {
            int len = Encoding.UTF8.GetByteCount(managedString);

            byte[] buffer = new byte[len + 1];

            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);

            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);

            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);

            return nativeUtf8;
        }

        // this version will also return the length of the utf8 string
        // This REQUIRES you to use Marshal.FreeHGlobal() on the returned pointer!
        public static IntPtr StringToUtf8(String managedString, out int utf8Length)
        {
            utf8Length = Encoding.UTF8.GetByteCount(managedString);

            byte[] buffer = new byte[utf8Length + 1];

            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);

            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);

            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);

            return nativeUtf8;
        }

        // Length is optional, but recommended
        // This is all potentially dangerous
        public static string Utf8ToString(IntPtr nativeUtf8, uint? length = null)
        {
            uint len = 0;

            if (length.HasValue)
            {
                len = length.Value;
            }
            else
            {
                // try to find the terminator
                while (Marshal.ReadByte(nativeUtf8, (int)len) != 0)
                {
                    ++len;
                }
            }

            byte[] buffer = new byte[len];

            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(buffer);
        }

        // C# doesn't allow #define as in C/C++, so we do this instead
        public static long NDIlib_send_timecode_synthesize = Int64.MaxValue;

    }
}
