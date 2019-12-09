using System;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public class IngestMedia : MediaBase, IIngestMedia
    {
        internal string BmdXmlFile; // Blackmagic's Media Express Xml file containing this media information
        internal StreamInfo[] StreamInfo;

        public override bool FileExists()
        {
            var dir = Directory as IngestDirectory;
            if (dir?.AccessType == TDirectoryAccessType.FTP)
                return dir.FileExists(FileName, Folder);
            return base.FileExists();
        }

        public TIngestStatus GetIngestStatus(IServerDirectory directory)
        {
            if (!(directory is ServerDirectory sdir))
                return TIngestStatus.Unknown;
            var media = sdir.FindMediaByMediaGuid(MediaGuid);
            if (media != null && media.MediaStatus == TMediaStatus.Available)
                return TIngestStatus.Ready;
            return TIngestStatus.Unknown;
        }

        public void NotifyIngestStatus(IServerDirectory directory, TIngestStatus newStatus)
        {
            
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
