using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using TAS.Common;
using TAS.Server.Interfaces;
using Newtonsoft.Json;
using TAS.FFMpegUtils;

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IngestMedia : Media, IIngestMedia
    {
        public IngestMedia(IngestDirectory directory, Guid guid = default(Guid)) : base(directory, guid) { }

        public override bool RenameTo(string newFileName)
        {
            if (((IngestDirectory)_directory).AccessType == TDirectoryAccessType.Direct)
                return base.RenameTo(newFileName);
                else throw new NotImplementedException("Cannot rename on remote directories");
        }

        public override bool FileExists()
        {
            if (((IngestDirectory)_directory).AccessType == TDirectoryAccessType.FTP)
                return true;
            else
                return base.FileExists();
        }

        internal XDCAM.NonRealTimeMeta ClipMetadata;
        internal XDCAM.Smil SmilMetadata;
        internal string XmlFile;
        internal StreamInfo[] StreamInfo;

        public override Stream GetFileStream(bool forWrite)
        {
            if (((IngestDirectory)_directory).AccessType == TDirectoryAccessType.Direct)
                return base.GetFileStream(forWrite);
            else
            if (((IngestDirectory)_directory).AccessType == TDirectoryAccessType.FTP)
            {
                        if (_directory is IngestDirectory)
                        {
                            if (((IngestDirectory)_directory).IsXDCAM)
                                return new XDCAM.XdcamStream(this, forWrite);
                            else
                                return new FtpMediaStream(this);
                        }
            }
            throw new NotImplementedException("Access types other than Direct and readonly FTP not implemented");
        }
    }
}
