using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infralution.Localization.Wpf;

namespace TAS.Common
{
    class EventRightsEnumConverter : ResourceEnumConverter
    {
        public EventRightsEnumConverter()
            : base(typeof(EventRight), Properties.Rights.ResourceManager)
        { }
    }

    [Flags]
    [TypeConverter(typeof(EventRightsEnumConverter))]
    public enum EventRight
    {
        Create = 0x01,
        Delete = 0x02,
        Modify = 0x04
    }

    class EngineRightsEnumConverter : ResourceEnumConverter
    {
        public EngineRightsEnumConverter()
            : base(typeof(EngineRight), Properties.Rights.ResourceManager)
        { }
    }

    [Flags]
    public enum EngineRight
    {
        Play = 0x01,
        Preview = 0x02,
        Rundown = 0x10,
        MediaIngest = 0x20,
        MediaEdit = 0x40,
        MediaDelete = 0x80,
    }
}
