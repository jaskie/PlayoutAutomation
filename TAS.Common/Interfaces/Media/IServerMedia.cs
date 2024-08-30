using System;

namespace TAS.Common.Interfaces.Media
{
    public interface IServerMedia: IPersistentMedia, IServerMediaProperties
    {
    }

    public interface IServerMediaProperties: IPersistentMediaProperties
    {
        bool DoNotArchive { get; set; }
        DateTime LastPlayed { get; }
    }
}
