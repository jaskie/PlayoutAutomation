using System;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Media
{
    public class IngestMedia : MediaBase, IIngestMedia, IServerIngestStatusMedia
    {
        internal IngestMedia(IngestDirectory directory, Guid guid = default(Guid)) : base(directory, guid) { }

        public override bool FileExists()
        {
            var dir = _directory as IngestDirectory;
            if (dir?.AccessType == TDirectoryAccessType.FTP)
                return dir.FileExists(_fileName, _folder);
            else
                return base.FileExists();
        }

        internal string XmlFile;
        internal StreamInfo[] StreamInfo;

        private TIngestStatus _ingestStatus;

        public TIngestStatus IngestStatus
        {
            get
            {
                if (_ingestStatus == TIngestStatus.Unknown)
                {
                    var sdir = _directory.MediaManager.MediaDirectoryPRI as ServerDirectory;
                    if (sdir != null)
                    {
                        var media = sdir.FindMediaByMediaGuid(_mediaGuid);
                        if (media != null && media.MediaStatus == TMediaStatus.Available)
                            _ingestStatus = TIngestStatus.Ready;
                    }
                }
                return _ingestStatus;
            }
            set { SetField(ref _ingestStatus, value); }                
        }

        public override Stream GetFileStream(bool forWrite)
        {
            var dir = _directory as IngestDirectory;
            if (dir != null)
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
