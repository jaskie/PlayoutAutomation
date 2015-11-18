using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;
using WebSocketSharp;

namespace TAS.Client.Model
{
    public class MediaDirectory : ProxyBase, IMediaDirectory
    {
        public MediaDirectory()
        {
        }
        public TDirectoryAccessType AccessType { get; set; }
        public string DirectoryName { get { return Get<string>(); } set { } }
        public string[] Extensions { get { return Get<string[]>(); } set { } }
        public List<IMedia> Files
        {
            get
            {
                return Get<List<Media>>().Cast<IMedia>().ToList();
            }
        }

        public string Folder { get { return Get<string>(); } set { } }

        public bool IsInitialized { get; set; }

        public NetworkCredential NetworkCredential
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Password
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Username
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ulong VolumeFreeSize { get { return Get<ulong>(); } set { } }

        public ulong VolumeTotalSize { get { return Get<ulong>(); } set { } }

        public event EventHandler<MediaEventArgs> MediaAdded;
        public event EventHandler<MediaEventArgs> MediaRemoved;
        public event EventHandler<MediaEventArgs> MediaVerified;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool DeleteMedia(IMedia media)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string filename, string subfolder = null)
        {
            throw new NotImplementedException();
        }

        public List<IMedia> FindMedia(Func<IMedia, bool> condition)
        {
            throw new NotImplementedException();
        }

        public IMedia FindMedia(Guid mediaGuid)
        {
            throw new NotImplementedException();
        }

        public IMedia FindMedia(IMedia media)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void MediaAdd(IMedia media)
        {
            throw new NotImplementedException();
        }

        public void MediaRemove(IMedia media)
        {
            throw new NotImplementedException();
        }

        public void OnMediaVerified(IMedia media)
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

        public override void OnMessage(object sender, WebSocketMessageEventArgs e)
        {
            //throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", DirectoryName, Folder);
        }
    }
}
