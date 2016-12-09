using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.FtpClient;
using System.Text.RegularExpressions;

namespace TAS.Server.XDCAM
{
    public class XdcamStream: Stream
    {
        public XdcamStream(XDCAMMedia media, bool forWrite)
        {
            
            if (media == null || media.Directory == null)
                throw new ApplicationException();
            //var fileName = string.Join(media.Directory.PathSeparator.ToString(), media.Directory.Folder, "Clip",  $"{media.XdcamClipAlias?.clipId ?? media.XdcamClip.clipId}.MXF");
            _client = new XdcamClient();
            _client.Credentials = ((IngestDirectory)media.Directory)._getNetworkCredential();
            _client.Host = new Uri(media.Directory.Folder).Host;
            _client.UngracefullDisconnection = true;
            try
            {
                _client.Connect();
                _smil_index = 0;
                _media = media;
                _isEditList = media.XdcamEdl != null;
                if (_isEditList)
                    _currentStream = _getNextStream();
                else
                {
                    if (forWrite)
                        _currentStream = _client.OpenWrite($"/Clip/{media.FileName}");
                    else
                    {
                        string fileName = string.Join("/", "/Clip", $"{(media.XdcamAlias != null ? media.XdcamAlias.value : media.XdcamClip.clipId)}.MXF");
                        _currentStream = _client.OpenRead(fileName);
                    }
                }
            }
            catch
            {
                _client.Dispose();
            }

        }

        private readonly bool _isEditList;
        private int _smil_index;
        private readonly XDCAMMedia _media;
        private Stream _currentStream;
        private readonly XdcamClient _client;

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    var stream = _currentStream;
                    if (stream != null)
                    {
                        stream.Flush();
                        stream.Close();
                    }
                    _client.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _currentStream.Write(buffer, offset, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            while (_currentStream != null && bytesRead < count)
            {
                int toReadLeft = count - bytesRead;
                int actualRead = _currentStream.Read(buffer, offset + bytesRead, toReadLeft);
                if (actualRead == 0)
                {
                    _currentStream.Dispose();
                    _currentStream = _getNextStream();
                }
                bytesRead += actualRead;
            }
            return bytesRead;
        }

        private Stream _getNextStream()
        {
            Stream result = null;
            if (_isEditList 
                &&_media.XdcamEdl.smil.body.par.refList.Count > _smil_index)
            {
                var r = _media.XdcamEdl.smil.body.par.refList[_smil_index];
                if (r.src.StartsWith(@"urn:smpte:umid:"))
                {
                    string umid = r.src.Substring(35);
                    int startFrame = r.clipBegin.SmpteToFrame();
                    int length = r.clipEnd.SmpteToFrame() - startFrame;
                    var media = _media.Directory.GetFiles().Select( m => (XDCAMMedia)m).FirstOrDefault( m => umid.Equals(m.XdcamClip?.umid));
                    if (media != null)
                    {
                        string fileName = string.Join("/", "/Clip", $"{media.XdcamClip.clipId}.MXF");
                        if (!_client.FileExists(fileName))
                            fileName = string.Join("/", "/Clip", $"{media.XdcamAlias.value}.MXF");
                        result = _client.OpenPart(fileName, startFrame, length);
                    }
                }
                _smil_index++;
            }
            return result;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return _currentStream == null || _isEditList ? -1 : _currentStream.Length; }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
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
                        return (((h * 60 + m) * 60) + s) * fps + f;
                    }
                }
            }
            return 0;
        }
    }
}
