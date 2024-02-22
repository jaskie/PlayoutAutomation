using System;
using System.ComponentModel;
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
    public enum EventRight: ulong
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
    [TypeConverter(typeof(EngineRightsEnumConverter))]
    public enum EngineRight: ulong
    {
        Play = 0x01,
        Preview = 0x02,
        RundownRightsAdmin = 0x8,
        Rundown = 0x10,
        MediaIngest = 0x100,
        MediaEdit = 0x200,
        MediaDelete = 0x400,
        MediaArchive = 0x800,
        MediaExport = 0x1000
    }

    class IngestFolderRightsEnumConverter : ResourceEnumConverter
    {
        public IngestFolderRightsEnumConverter()
            : base(typeof(IngestFolderRight), Properties.Rights.ResourceManager)
        { }
    }

    [Flags]
    [TypeConverter(typeof(IngestFolderRightsEnumConverter))]
    public enum IngestFolderRight : ulong
    {
        Ingest = 0x01,
        Export = 0x02,
    }
}
