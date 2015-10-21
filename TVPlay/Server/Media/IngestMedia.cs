using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace TAS.Server
{
    public class IngestMedia : Media
    {
        public IngestMedia(IngestDirectory directory) : base(directory) { }
        internal XDCAM.NonRealTimeMeta ClipMetadata;
        internal XDCAM.Smil SmilMetadata;
        internal string XmlFile;
        protected override Stream _getFileStream(bool forWrite)
        {
            if (_directory.AccessType == TDirectoryAccessType.Direct)
                return base._getFileStream(forWrite);
            if (_directory.AccessType == TDirectoryAccessType.FTP)
            {
                try
                {
                        if (_directory is IngestDirectory)
                        {
                            if (((IngestDirectory)_directory).IsXDCAM)
                                return new XDCAM.XdcamStream(this, forWrite);
                            else
                                return new FtpMediaStream(this);
                        }
                }
                catch (Exception we)
                {
                    Debug.WriteLine(we.Message);
                }
            }
            throw new NotImplementedException("Access types other than Direct and readonly FTP not implemented");
        }
    }
}
