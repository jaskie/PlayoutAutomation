using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public abstract class MediaDirectory : ProxyBase, IMediaDirectory
    {
        public TDirectoryAccessType AccessType { get; set; }
        public string DirectoryName { get { return Get<string>(); } set { Set(value); } }

        private List<IMedia> _files;
        public  IList<IMedia> GetFiles()
        {
            _files = Query<List<IMedia>>();
            return _files;
        }

        public string Folder { get { return Get<string>(); } set { Set(value); } }

        public char PathSeparator { get { return Get<char>(); }  set { Set(value); } }

        public bool IsInitialized { get { return Get<bool>(); } set { Set(value); } }

        public long VolumeFreeSize { get { return Get<long>(); } internal set { Set(value); } }

        public long VolumeTotalSize { get { return Get<long>(); } internal set { Set(value); } }

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

        protected override void OnEventNotification(WebSocketMessage e)
        {
            if (e.MemberName == nameof(MediaAdded))
                    MediaAddedEvent?.Invoke(this, ConvertEventArgs<MediaEventArgs>(e));
            if (e.MemberName == nameof(MediaRemoved))
                MediaRemovedEvent?.Invoke(this, ConvertEventArgs<MediaEventArgs>(e));
            if (e.MemberName == nameof(MediaVerified))
                MediaVerifiedEvent?.Invoke(this, ConvertEventArgs<MediaEventArgs>(e));
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

        public abstract IMedia CreateMedia(IMediaProperties mediaProperties);
    }
}
