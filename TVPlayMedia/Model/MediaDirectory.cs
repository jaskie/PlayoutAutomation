using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using TAS.Common;
using TAS.Remoting;
using TAS.Remoting.Client;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public abstract class MediaDirectory : ProxyBase, IMediaDirectory
    {
        public MediaDirectory()
        {
        }
        public TDirectoryAccessType AccessType { get; set; }
        public string DirectoryName { get { return Get<string>(); } set { Set(value); } }

        private List<IMedia> _files;
        public  ICollection<IMedia> GetFiles()
        {
            _files = Query<List<IMedia>>();
            _files.ForEach(m => (m as Media).Directory = this);
            return _files;
        }

        public string Folder { get { return Get<string>(); } set { Set(value); } }

        public char PathSeparator { get { return Get<char>(); }  set { Set(value); } }

        public bool IsInitialized { get { return Get<bool>(); } set { Set(value); } }

        public long VolumeFreeSize { get { return Get<long>(); } internal set { Set(value); } }

        public long VolumeTotalSize { get { return Get<long>(); } internal set { Set(value); } }

        event EventHandler<MediaEventArgs> _mediaAdded;
        public event EventHandler<MediaEventArgs> MediaAdded
        {
            add
            {
                EventAdd(_mediaAdded);
                _mediaAdded += value;
            }
            remove
            {
                _mediaAdded -= value;
                EventRemove(_mediaAdded);
            }
        }

        event EventHandler<MediaEventArgs> _mediaRemoved;
        public event EventHandler<MediaEventArgs> MediaRemoved
        {
            add
            {
                EventAdd(_mediaRemoved);
                _mediaRemoved += value;
            }
            remove
            {
                _mediaRemoved -= value;
                EventRemove(_mediaRemoved);
            }
        }

        event EventHandler<MediaEventArgs> _mediaDeleted;
        public event EventHandler<MediaEventArgs> MediaDeleted
        {
            add
            {
                EventAdd(_mediaDeleted);
                _mediaDeleted += value;
            }
            remove
            {
                _mediaDeleted -= value;
                EventRemove(_mediaDeleted);
            }
        }

        event EventHandler<MediaPropertyChangedEventArgs> _mediaPropertyChanged;
        public event EventHandler<MediaPropertyChangedEventArgs> MediaPropertyChanged
        {
            add
            {
                EventAdd(_mediaPropertyChanged);
                _mediaPropertyChanged += value;
            }
            remove
            {
                _mediaPropertyChanged -= value;
                EventRemove(_mediaPropertyChanged);
            }
        }

        event EventHandler<MediaEventArgs> _mediaVerified;
        public event EventHandler<MediaEventArgs> MediaVerified
        {
            add
            {
                EventAdd(_mediaVerified);
                _mediaVerified += value;
            }
            remove
            {
                _mediaVerified -= value;
                EventRemove(_mediaVerified);
            }
        }

        protected override void OnEventNotification(WebSocketMessageEventArgs e)
        {
            if (e.Message.MemberName == nameof(MediaAdded))
                    _mediaAdded?.Invoke(this, ConvertEventArgs<MediaEventArgs>(e));
            if (e.Message.MemberName == nameof(MediaRemoved))
                _mediaRemoved?.Invoke(this, ConvertEventArgs<MediaEventArgs>(e));
            if (e.Message.MemberName == nameof(MediaVerified))
                _mediaVerified?.Invoke(this, ConvertEventArgs<MediaEventArgs>(e));
        }

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

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            Invoke();
        }

        public void SweepStaleMedia()
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MediaDirectory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public override string ToString()
        {
            return string.Format("{0}:{1} ({2})", this.GetType(), DirectoryName, Folder);
        }
    }
}
