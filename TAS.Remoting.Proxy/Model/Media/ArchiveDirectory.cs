using System;
using jNet.RPC;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectory
    {

        public IArchiveMedia Find(Guid mediaGuid)
        {
            var ret =  Query<ArchiveMedia>(parameters: new object[] { mediaGuid });
            return ret;
        }

        public bool ContainsMedia(Guid mediaGuid)
        {
            return Query<bool>(parameters: new object[] {mediaGuid});
        }

        private event EventHandler<MediaIsArchivedEventArgs> MediaIsArchivedEvent;
        public event EventHandler<MediaIsArchivedEventArgs> MediaIsArchived
        {
            add
            {
                EventAdd(MediaIsArchivedEvent);
                MediaIsArchivedEvent += value;
            }
            remove
            {
                MediaIsArchivedEvent -= value;
                EventRemove(MediaIsArchivedEvent);
            }
        }

        public IMediaManager MediaManager { get; set; }

        protected override void OnEventNotification(string eventName, EventArgs eventArgs)
        {
            if (eventName == nameof(MediaIsArchived))
                MediaIsArchivedEvent?.Invoke(this, (MediaIsArchivedEventArgs)eventArgs);
            else
                base.OnEventNotification(eventName, eventArgs);
        }
    }
}
