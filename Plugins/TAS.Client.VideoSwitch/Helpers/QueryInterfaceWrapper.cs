using System;
using System.Runtime.InteropServices;

namespace TAS.Server.VideoSwitch.Helpers
{
    public static class QueryInterfaceWrapper
    {
        public static T GetObject<T>(object obj) where T : class
        {
            Guid iid = typeof(T).GUID;

            if (Marshal.QueryInterface(Marshal.GetIUnknownForObject(obj), ref iid, out var mixParamsPtr) != 0)
                return default(T);

            return (T)Marshal.GetTypedObjectForIUnknown(mixParamsPtr, typeof(T));            
        }
    }
}
