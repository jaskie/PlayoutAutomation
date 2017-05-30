using System;
using System.IO;

namespace TAS.Server.Media
{
    public class FtpMediaStream : Stream
    {
        private readonly System.Net.FtpClient.FtpClient _client;
        private readonly Stream _ftpStream;

        public FtpMediaStream(IngestMedia media, bool forWrite)
        {
            if (media?.Directory == null)
                throw new ApplicationException();
            var uri = new Uri(media.FullPath);
            _client = new System.Net.FtpClient.FtpClient
            {
                Credentials = ((IngestDirectory) media.Directory).GetNetworkCredential(),
                Host = uri.Host
            };
            try
            {
                _client.Connect();
                _ftpStream = forWrite ? 
                    _client.OpenWrite(uri.LocalPath) : 
                    _client.OpenRead(uri.LocalPath);
            }
            catch
            {
                _client.Dispose();
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ftpStream.Dispose();
                _client.Dispose();
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => _ftpStream.CanRead;

        public override bool CanSeek => _ftpStream.CanSeek;

        public override bool CanWrite => _ftpStream.CanWrite;

        public override long Length => _ftpStream.Length;

        public override long Position
        {
            get
            {
                return _ftpStream.Position;
            }
            set
            {
                _ftpStream.Position = value;
            }
        }

        public override void Flush()
        {
            _ftpStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _ftpStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _ftpStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _ftpStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _ftpStream.Write(buffer, offset, count);
        }
    }
}
