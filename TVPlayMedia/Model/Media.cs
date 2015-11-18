using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class Media : Server.Interfaces.IMedia
    {
        public TAudioChannelMapping AudioChannelMapping { get; set; }
        public decimal AudioLevelIntegrated { get; set; }

        public decimal AudioLevelPeak { get; set; }

        public decimal AudioVolume { get; set; }

        public IMediaDirectory Directory { get; }

        public TimeSpan Duration { get; set; }

        public TimeSpan DurationPlay { get; set; }

        public string FileName { get; set; }

        public ulong FileSize { get; set; }

        public string Folder { get; set; }

        public RationalNumber FrameRate { get; set; }

        public string FullPath { get; set; }

        public bool HasExtraLines { get; }

        public DateTime LastUpdated { get; set; }

        public TMediaCategory MediaCategory { get; set; }

        public Guid MediaGuid { get; }

        public string MediaName { get; set; }
        
        public TMediaStatus MediaStatus { get; set; }

        public TMediaType MediaType { get; set; }
        
        public TParental Parental { get; set; }

        public TimeSpan TCPlay { get; set; }

        public TimeSpan TCStart { get; set; }

        public bool Verified { get; set; }

        public TVideoFormat VideoFormat { get; set; }

        public VideoFormatDescription VideoFormatDescription { get; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void CloneMediaProperties(IMedia from)
        {
            throw new NotImplementedException();
        }

        public bool CopyMediaTo(IMedia destMedia, ref bool abortCopy)
        {
            throw new NotImplementedException();
        }

        public bool Delete()
        {
            throw new NotImplementedException();
        }

        public bool FileExists()
        {
            throw new NotImplementedException();
        }

        public bool FilePropertiesEqual(IMedia m)
        {
            throw new NotImplementedException();
        }

        public Stream GetFileStream(bool forWrite)
        {
            throw new NotImplementedException();
        }

        public void GetLoudness()
        {
            throw new NotImplementedException();
        }

        public void GetLoudness(TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback)
        {
            throw new NotImplementedException();
        }

        public void Remove()
        {
            throw new NotImplementedException();
        }

        public void Verify()
        {
            throw new NotImplementedException();
        }
    }
}
