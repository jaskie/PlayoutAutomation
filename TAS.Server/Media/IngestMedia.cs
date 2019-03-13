using System;
using System.Diagnostics;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class IngestMedia : MediaBase, IIngestMedia
    {
        internal string BmdXmlFile; // Blackmagic's Media Express Xml file containing this media information
        internal StreamInfo[] StreamInfo;
        private Lazy<TIngestStatus> _ingestStatus; 
        public IngestMedia()
        {
            _ingestStatus =  new Lazy<TIngestStatus>(() =>
            {
                if (!(((IngestDirectory)Directory).MediaManager.MediaDirectoryPRI is ServerDirectory sdir))
                    return TIngestStatus.Unknown;
                var media = sdir.FindMediaByMediaGuid(MediaGuid);
                if (media != null && media.MediaStatus == TMediaStatus.Available)
                    return TIngestStatus.Ready;
                return TIngestStatus.Unknown;
            });

        }

        public override bool FileExists()
        {
            var dir = Directory as IngestDirectory;
            if (dir?.AccessType == TDirectoryAccessType.FTP)
                return dir.FileExists(FileName, Folder);
            return base.FileExists();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public TIngestStatus IngestStatus
        {
            get => _ingestStatus.Value;
            set
            {
                if (_ingestStatus.IsValueCreated && _ingestStatus.Value != value)
                    SetField(ref _ingestStatus, new Lazy<TIngestStatus>(() => value));
            }
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
