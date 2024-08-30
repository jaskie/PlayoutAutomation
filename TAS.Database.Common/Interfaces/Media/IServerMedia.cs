using System;

namespace TAS.Database.Common.Interfaces.Media
{
    public interface IServerMedia : TAS.Common.Interfaces.Media.IServerMedia, IPersistentMedia
    {
        new DateTime LastPlayed { get; set; }
    }
}