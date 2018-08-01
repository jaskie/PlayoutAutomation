using System;
using System.IO;
using System.Linq;
using System.Net.FtpClient;
using System.Threading;
using TAS.Server.Media;

namespace TAS.Server.XDCAM
{
    public class XdcamStream: Stream
    {
        private readonly bool _isEditList;
        private int _smilIndex;
        private readonly XdcamMedia _media;
        private Stream _currentStream;
        private readonly FtpClient _client;
        private readonly Smil _smil;

        public XdcamStream(XdcamMedia media, bool forWrite)
        {
            
            if (!(media?.Directory is IngestDirectory dir))
                throw new ApplicationException("XDCAM media directory is not IngestDirectory");
            //var fileName = string.Join(media.Directory.PathSeparator.ToString(), media.Directory.Folder, "Clip",  $"{media.XdcamClipAlias?.clipId ?? media.XdcamClip.clipId}.MXF");
            _client = dir.GetFtpClient();
            if (!Monitor.TryEnter(dir.XdcamLockObject))
                throw new ApplicationException("Directory is in use");
            try
            {
                _client.Connect();
                if (media.XdcamMaterial.type == MaterialType.Edl)
                {
                    var smilXml = dir.ReadXmlDocument(media.XdcamMaterial.uri, _client);
                    if (smilXml != null)
                        _smil = SerializationHelper<Smil>.Deserialize(smilXml);
                    _isEditList = true;
                }
                _smilIndex = 0;
                _media = media;
                if (_isEditList)
                    _currentStream = _getNextStream();
                else
                {
                    _currentStream = forWrite ? _client.OpenWrite($"Clip/{media.FileName}") : _client.OpenRead(media.XdcamMaterial.uri);
                }
            }
            catch
            {
                Monitor.Exit(dir.XdcamLockObject);
                _client.Disconnect();
                throw;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _currentStream.Write(buffer, offset, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = 0;
            while (_currentStream != null && bytesRead < count)
            {
                var toReadLeft = count - bytesRead;
                var actualRead = _currentStream.Read(buffer, offset + bytesRead, toReadLeft);
                if (actualRead == 0)
                {
                    _currentStream.Dispose();
                    _currentStream = _getNextStream();
                }
                bytesRead += actualRead;
            }
            return bytesRead;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override void Flush()
        {
        }

        public override long Length => _currentStream == null || _isEditList ? -1 : _currentStream.Length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing)
                    return;
                var stream = _currentStream;
                if (stream == null)
                    return;
                stream.Flush();
                stream.Close();
            }
            finally
            {
                base.Dispose(disposing);
                _client.Disconnect();
                Monitor.Exit((_media.Directory as IngestDirectory)?.XdcamLockObject);
            }
        }

        private Stream _getNextStream()
        {
            const string umidSpecifier = "urn:smpte:umid:";
            if (!_isEditList || _smil.body.par.refList.Count <= _smilIndex)
                return null;
            var r = _smil.body.par.refList[_smilIndex];
            if (r.src?.StartsWith(umidSpecifier) != true)
                return null;
            var umid = r.src.Substring(umidSpecifier.Length);
            var startFrame = r.clipBegin.SmpteToFrame();
            var length = r.clipEnd.SmpteToFrame() - startFrame;
            var media = _media.Directory.GetFiles().Cast<XdcamMedia>().FirstOrDefault(m => umid.Equals(m.XdcamMaterial?.umid));
            if (media?.XdcamMaterial == null)
                return null;
            var fileName = media.XdcamMaterial.uri;
            _smilIndex++;
            return ((XdcamClient)_client).OpenPart(fileName, startFrame, length);
        }

    }

    public static class SmpteExtensions
    {
        public static int SmpteToFrame(this string smpte)
        {
            string[] parts = smpte.Split('=');
            if (parts.Length == 2)
            {
                int fps = 25; // PAL - should be taken form parts[0]
                string[] values = parts[1].Split(':', ';');
                if (values.Length == 4)
                {
                    int h, m, s, f;
                    if (int.TryParse(values[0], out h)
                     && int.TryParse(values[1], out m)
                     && int.TryParse(values[2], out s)
                     && int.TryParse(values[3], out f))
                    {
                        return ((h * 60 + m) * 60 + s) * fps + f;
                    }
                }
            }
            return 0;
        }
    }
}
