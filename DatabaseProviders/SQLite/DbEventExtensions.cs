using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Database.SQLite
{
    public static class DbEventExtensions
    {
        public static uint ToFlags(this IEvent ev)
        {
            return (uint)((ev.IsEnabled ? 1 : 0) << 0
             | (ev.IsHold ? 1 : 0) << 1
             | (ev.IsLoop ? 1 : 0) << 2
             | (ev.IsCGEnabled ? 1 : 0) << 4
             | (ev.Parental & 0xF) << 6
             | (ev.Logo & 0xF) << 10
             | (ev.Crawl & 0xF) << 14
             | ((byte)ev.AutoStartFlags) << 20)
             ;
        }

        public static bool IsEnabled(this uint flags) { return (flags & (1 << 0)) != 0; }
        public static bool IsHold(this uint flags) { return (flags & (1 << 1)) != 0; }
        public static bool IsLoop(this uint flags) { return (flags & (1 << 2)) != 0; }
        public static bool IsCGEnabled(this uint flags) { return (flags & (1 << 4)) != 0; }
        public static byte Parental (this uint flags) { return (byte)((flags >> 6) & 0xF); }
        public static byte Logo(this uint flags) { return (byte)((flags >> 10) & 0xF); }
        public static byte Crawl(this uint flags) { return (byte)((flags >> 14) & 0xF); }
        public static AutoStartFlags AutoStartFlags(this uint flags) { return (AutoStartFlags)((flags >> 20) & 0x0F); }
    }
}
