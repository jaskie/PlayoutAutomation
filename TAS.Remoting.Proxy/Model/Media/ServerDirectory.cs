using System;
using jNet.RPC;
using TAS.Common;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public class ServerDirectory : WatcherDirectory, IServerDirectory
    {
#pragma warning disable CS0649
        [DtoMember(nameof(IServerDirectory.IsRecursive))]
        private bool _isRecursive;

        [DtoMember(nameof(IServerDirectory.MovieContainerFormat))]
        private readonly TMovieContainerFormat _movieContainerFormat;

#pragma warning restore

        public TMovieContainerFormat MovieContainerFormat => _movieContainerFormat;

        private event EventHandler<MediaIngestStatusEventArgs> IngestStatusUpdatedEvent;

        public event EventHandler<MediaIngestStatusEventArgs> IngestStatusUpdated
        {
            add
            {
                EventAdd(IngestStatusUpdatedEvent);
                IngestStatusUpdatedEvent += value;
            }
            remove
            {
                IngestStatusUpdatedEvent -= value;
                EventRemove(IngestStatusUpdatedEvent);
            }
        }

        public bool IsRecursive => _isRecursive;

        protected override void OnEventNotification(SocketMessage message)
        {
            base.OnEventNotification(message);
            switch (message.MemberName)
            {
                case nameof(IngestStatusUpdated):
                    IngestStatusUpdatedEvent?.Invoke(this, DeserializeEventArgs<MediaIngestStatusEventArgs>(message));
                    break;
            }
        }
    }
}
