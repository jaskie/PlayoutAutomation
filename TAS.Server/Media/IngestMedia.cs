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

        private TIngestStatus _ingestState;
        public TIngestStatus IngestState
        {
            get
            {
                if (_ingestState == TIngestStatus.Unknown)
                {
                    var sdir = _directory.MediaManager.MediaDirectoryPRI as ServerDirectory;
                    if (sdir != null)
                    {
                        var media = sdir.FindMediaByMediaGuid(_mediaGuid);
                        if (media != null && media.MediaStatus == TMediaStatus.Available)
                            _ingestState = TIngestStatus.Ready;
                    }
                }
                return _ingestState;
            }
            internal set { SetField(ref _ingestState, value, "IngestState"); }                
        }

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
