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
        public string[] Extensions { get; set; }
        public abstract IEnumerable<IMedia> GetFiles();

        public string Folder { get { return Get<string>(); } set { Set(value); } }

        public bool IsInitialized { get; set; }

        public NetworkCredential NetworkCredential { get { return null; } }

        public string Password { get; set; }
        
        public string Username { get; set; }

        public ulong VolumeFreeSize { get { return Get<ulong>(); } internal set { Set(value); } }

        public ulong VolumeTotalSize { get { return Get<ulong>(); } internal set { Set(value); } }

        event EventHandler<MediaDtoEventArgs> _mediaAdded;
        public event EventHandler<MediaDtoEventArgs> MediaAdded
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

        event EventHandler<MediaDtoEventArgs> _mediaRemoved;
        public event EventHandler<MediaDtoEventArgs> MediaRemoved
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
        event EventHandler<MediaDtoEventArgs> _mediaVerified;
        public event EventHandler<MediaDtoEventArgs> MediaVerified
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
            if (e.Message.MemberName == "MediaAdded")
            {
                var h = _mediaAdded;
                if (h != null)
                    h(this, ConvertEventArgs<MediaDtoEventArgs>(e));
            }
            if (e.Message.MemberName == "MediaRemoved")
            {
                var h = _mediaRemoved;
                if (h != null)
                    h(this, ConvertEventArgs<MediaDtoEventArgs>(e));
            }
            if (e.Message.MemberName == "MediaVerified")
            {
                var h = _mediaVerified;
                if (h != null)
                    h(this, ConvertEventArgs<MediaDtoEventArgs>(e));
            }
        }

        public bool DeleteMedia(IMedia media)
        {
            return Query<bool>(parameters: media );
        }

        public bool FileExists(string filename, string subfolder = null)
        {
            return Query<bool>(parameters: new object[] { filename, subfolder });
        }
        public abstract IMedia FindMediaByDto(Guid dtoGuid);

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
            return string.Format("{0} ({1})", DirectoryName, Folder);
        }
    }
}
