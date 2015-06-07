using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TAS.Server
{
    public class FtpMediaStream : Stream
    {
        private readonly IngestMedia _media;
        private readonly System.Net.FtpClient.FtpClient _client;
        private readonly Stream _ftpStream;
        public FtpMediaStream(IngestMedia media)
        {
            _media = media;
            if (media == null || media.Directory == null)
                throw new ApplicationException();
            Uri uri = new Uri(media.FullPath);
            _client = new System.Net.FtpClient.FtpClient();
            _client.Credentials = media.Directory.NetworkCredential;
            _client.Host = uri.Host;
            try
            {
                _client.Connect();
                _ftpStream = _client.OpenRead(uri.LocalPath);
            }
            catch
            {
                _client.Dispose();
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

        public override bool CanRead
        {
            get { return _ftpStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _ftpStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _ftpStream.CanWrite; }
        }

        public override void Flush()
        {
            _ftpStream.Flush();
        }

        public override long Length
        {
            get { return _ftpStream.Length; }
        }

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
