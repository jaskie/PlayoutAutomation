using System;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public abstract class MediaDirectoryBase : ProxyObjectBase, IMediaDirectory
    {
        #pragma warning disable CS0649

        [DtoField(nameof(IMediaDirectory.Folder))]
        private string _folder;

        [DtoField(nameof(IMediaDirectory.PathSeparator))]
        private char _pathSeparator;

        [DtoField(nameof(IMediaDirectory.VolumeFreeSize))]
        private long _volumeFreeSize;

        [DtoField(nameof(IMediaDirectory.VolumeTotalSize))]
        private long _volumeTotalSize;

        [DtoField(nameof(IMediaDirectory.HaveFileWatcher))]
        private bool _haveFileWatcher;


        #pragma warning restore
        public string Folder { get => _folder; set => Set(value); }

        public char PathSeparator => _pathSeparator;

        public long VolumeFreeSize => _volumeFreeSize;

        public long VolumeTotalSize => _volumeTotalSize;

        public bool HaveFileWatcher => _haveFileWatcher;


        #region Event handling
        private event EventHandler<MediaEventArgs> MediaAddedEvent;
        public event EventHandler<MediaEventArgs> MediaAdded
        {
            add
            {
                EventAdd(MediaAddedEvent);
                MediaAddedEvent += value;
            }
            remove
            {
                MediaAddedEvent -= value;
                EventRemove(MediaAddedEvent);
            }
        }

        private event EventHandler<MediaEventArgs> MediaRemovedEvent;
        public event EventHandler<MediaEventArgs> MediaRemoved
        {
            add
            {
                EventAdd(MediaRemovedEvent);
                MediaRemovedEvent += value;
            }
            remove
            {
                MediaRemovedEvent -= value;
                EventRemove(MediaRemovedEvent);
            }
        }

        private event EventHandler<MediaEventArgs> MediaDeletedEvent;
        public event EventHandler<MediaEventArgs> MediaDeleted
        {
            add
            {
                EventAdd(MediaDeletedEvent);
                MediaDeletedEvent += value;
            }
            remove
            {
                MediaDeletedEvent -= value;
                EventRemove(MediaDeletedEvent);
            }
        }

        private event EventHandler<MediaEventArgs> MediaVerifiedEvent;
        public event EventHandler<MediaEventArgs> MediaVerified
        {
            add
            {
                EventAdd(MediaVerifiedEvent);
                MediaVerifiedEvent += value;
            }
            remove
            {
                MediaVerifiedEvent -= value;
                EventRemove(MediaVerifiedEvent);
            }
        }

        protected override void OnEventNotification(SocketMessage message)
        {
            switch (message.MemberName)
            {
                case nameof(MediaAdded):
                    MediaAddedEvent?.Invoke(this, Deserialize<MediaEventArgs>(message));
                    break;
                case nameof(MediaRemoved):
                    MediaRemovedEvent?.Invoke(this, Deserialize<MediaEventArgs>(message));
                    break;
                case nameof(MediaVerified):
                    MediaVerifiedEvent?.Invoke(this, Deserialize<MediaEventArgs>(message));
                    break;
            }
        }

        #endregion // Ehent handling

        public bool DeleteMedia(IMedia media) => Query<bool>(parameters: media);

        public bool FileExists(string filename, string subfolder = null)
        {
            return Query<bool>(parameters: new object[] { filename, subfolder });
        }

        public bool DirectoryExists => Get<bool>();

        public void SweepStaleMedia() => throw new NotImplementedException();

        public string GetUniqueFileName(string fileName)
        {
            return Query<string>(parameters: new object[] {fileName});
        }

        public IMediaSearchProvider Search(TMediaCategory? category, string searchString)
        {
            return Query<MediaSearchProvider>(parameters: new object[] { category, searchString });
        }
    }
}
