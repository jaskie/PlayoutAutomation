using System;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class IngestMedia : MediaBase, IIngestMedia
    {
        private TIngestStatus _ingestStatus;
        internal string BmdXmlFile; // Blackmagic's Media Express Xml file containing this media information
        internal StreamInfo[] StreamInfo;

        internal IngestMedia(IngestDirectory directory, Guid guid = default(Guid)) : base(directory, guid) { }

        public override bool FileExists()
        {
            var dir = Directory as IngestDirectory;
            if (dir?.AccessType == TDirectoryAccessType.FTP)
                return dir.FileExists(FileName, Folder);
            return base.FileExists();
        }
        
        public TIngestStatus IngestStatus
        {
            get
            {
                if (_ingestStatus == TIngestStatus.Unknown)
                {
                    if (((IngestDirectory)Directory).MediaManager.MediaDirectoryPRI is ServerDirectory sdir)
                    {
                        var media = sdir.FindMediaByMediaGuid(MediaGuid);
                        if (media != null && media.MediaStatus == TMediaStatus.Available)
                            _ingestStatus = TIngestStatus.Ready;
                    }
                }
                return _ingestStatus;
            }
            set => SetField(ref _ingestStatus, value);
        }

        public override Stream GetFileStream(bool forWrite)
        {
            if (Directory is IngestDirectory dir)
            {
                if (dir.AccessType == TDirectoryAccessType.Direct)
                    return base.GetFileStream(forWrite);
                else
                if (dir.AccessType == TDirectoryAccessType.FTP)
                    return new FtpMediaStream(this, forWrite);
                throw new NotImplementedException("Access types other than Direct and readonly FTP not implemented");
            }
            throw new InvalidOperationException("IngestMedia: _directory must be IngestDirectory");
        }
    }
}
