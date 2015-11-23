using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using TAS.Common;
using TAS.Server.Interfaces;
using Newtonsoft.Json;

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IngestMedia : Media, IIngestMedia
    {
        public IngestMedia(IngestDirectory directory) : base(directory) { }
        public IngestMedia(IngestDirectory directory, Guid guid) : base(directory, guid) { }

        internal XDCAM.NonRealTimeMeta ClipMetadata;
        internal XDCAM.Smil SmilMetadata;
        internal string XmlFile;
        public override Stream GetFileStream(bool forWrite)
        {
            if (_directory.AccessType == TDirectoryAccessType.Direct)
                return base.GetFileStream(forWrite);
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
