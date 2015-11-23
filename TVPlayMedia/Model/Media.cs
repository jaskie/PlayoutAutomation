using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;

namespace TAS.Client.Model
{
    [JsonObject]
    public class Media : ProxyBase, Server.Interfaces.IMedia
    {
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public TAudioChannelMapping AudioChannelMapping { get { return Get<TAudioChannelMapping>(); } set { Set(value); } }

        public decimal AudioLevelIntegrated { get { return Get<decimal>(); } set { Set(value); } }

        public decimal AudioLevelPeak { get { return Get<decimal>(); } set { Set(value); } }

        public decimal AudioVolume { get { return Get<decimal>(); } set { Set(value); } }

        [JsonIgnore]
        public virtual IMediaDirectory Directory { get; set; }

        public TimeSpan Duration { get { return Get<TimeSpan>(); } set { Set(value); } }

        public TimeSpan DurationPlay { get { return Get<TimeSpan>(); } set { Set(value); } }

        public string FileName { get { return Get<string>(); } set { Set(value); } }

        public ulong FileSize { get { return Get<ulong>(); } set { Set(value); } }

        public string Folder { get { return Get<string>(); } set { Set(value); } }

        public RationalNumber FrameRate { get { return Get<RationalNumber>(); } set { Set(value); } }

        public string FullPath { get { return Get<string>(); } set { Set(value); } }

        public bool HasExtraLines { get { return Get<bool>(); } set { Set(value); } }

        public DateTime LastUpdated { get { return Get<DateTime>(); } set { Set(value); } }

        public TMediaCategory MediaCategory { get { return Get<TMediaCategory>(); } set { Set(value); } }

        public Guid MediaGuid { get { return Get<Guid>(); } set { Set(value); } }

        public string MediaName { get { return Get<string>(); } set { Set(value); } }

        public TMediaStatus MediaStatus { get { return Get<TMediaStatus>(); } set { Set(value); } }

        public TMediaType MediaType { get { return Get<TMediaType>(); } set { Set(value); } }

        public TParental Parental { get { return Get<TParental>(); } set { Set(value); } }

        public TimeSpan TCPlay { get { return Get<TimeSpan>(); } set { Set(value); } }

        public TimeSpan TCStart { get { return Get<TimeSpan>(); } set { Set(value); } }

        public bool Verified { get { return Get<bool>(); } set { Set(value); } }

        public TVideoFormat VideoFormat { get { return Get<TVideoFormat>(); } set { Set(value); } }

        public VideoFormatDescription VideoFormatDescription { get { return Get<VideoFormatDescription>(); } set { Set(value); } }

        public bool Delete()
        {
            return Query<bool>();
        }

        public bool FileExists()
        {
            return Query<bool>();
        }

        public void GetLoudness()
        {
            Invoke();
        }

        public void GetLoudnessWithCallback(TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback)
        {
            throw new NotImplementedException();
        }

        public void Verify()
        {
            Invoke();
        }

        public override string ToString()
        {
            return MediaName;
        }
    }
}
