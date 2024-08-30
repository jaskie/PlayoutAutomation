using jNet.RPC;
using System;
using TAS.Common.Interfaces.Media;

namespace TAS.Remoting.Model.Media
{
    public class ServerMedia : PersistentMedia, IServerMedia
    {
#pragma warning disable CS0649

        [DtoMember(nameof(IServerMedia.DoNotArchive))]
        private bool _doNotArchive;

        [DtoMember(nameof(IServerMedia.LastPlayed))]
        private DateTime _lastPlayed;

#pragma warning restore

        public bool DoNotArchive { get => _doNotArchive; set => Set(value); }
        
        public DateTime LastPlayed => _lastPlayed;
    }
}
