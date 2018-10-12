using System;
using Newtonsoft.Json;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public abstract class MediaDirectoryBase : ProxyBase, IMediaDirectory
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IWatcherDirectory.DirectoryName))]
        private string _directoryName;

        [JsonProperty(nameof(IWatcherDirectory.Folder))]
        private string _folder;

        [JsonProperty(nameof(IWatcherDirectory.PathSeparator))]
        private char _pathSeparator;

        [JsonProperty(nameof(IWatcherDirectory.IsInitialized))]
        private bool _isInitialized;

        [JsonProperty(nameof(IWatcherDirectory.VolumeFreeSize))]
        private long _volumeFreeSize;

        [JsonProperty(nameof(IWatcherDirectory.VolumeTotalSize))]
        private long _volumeTotalSize;

        #pragma warning restore

        public string DirectoryName { get => _directoryName; set => Set(value); }

        public string Folder { get => _folder; set => Set(value); }

        public char PathSeparator => _pathSeparator;

        public bool IsInitialized => _isInitialized;

        public long VolumeFreeSize => _volumeFreeSize;

        public long VolumeTotalSize => _volumeTotalSize;

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
            if (message.MemberName == nameof(MediaAdded))
                    MediaAddedEvent?.Invoke(this, Deserialize<MediaEventArgs>(message));
            if (message.MemberName == nameof(MediaRemoved))
                MediaRemovedEvent?.Invoke(this, Deserialize<MediaEventArgs>(message));
            if (message.MemberName == nameof(MediaVerified))
                MediaVerifiedEvent?.Invoke(this, Deserialize<MediaEventArgs>(message));
        }

        #endregion // Ehent handling

        public bool DeleteMedia(IMedia media)
        {
            return Query<bool>(parameters: media );
        }

        public bool FileExists(string filename, string subfolder = null)
        {
            return Query<bool>(parameters: new object[] { filename, subfolder });
        }

        public bool DirectoryExists()
        {
            return Query<bool>();
        }

        public void Refresh()
        {
            Invoke();
        }

        public void SweepStaleMedia()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return DirectoryName;
        }

        public string GetUniqueFileName(string fileName)
        {
            return Query<string>(parameters: new object[] {fileName});
        }

    }
}
