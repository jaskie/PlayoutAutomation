using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    [DataContract]
    public class ExportMedia
    {
        public ExportMedia(IMedia media, List<IMedia> logos, TimeSpan startTC, TimeSpan duration, decimal audioVolume)
        {
            this.Media = media;
            this.Logos = logos;
            this.StartTC = startTC;
            this.Duration = duration;
            this.AudioVolume = audioVolume;
        }
        [DataMember]
        public IMedia Media { get; private set; }
        [DataMember]
        public List<IMedia> Logos { get; private set; }
        [DataMember]
        public TimeSpan Duration;
        [DataMember]
        public TimeSpan StartTC;
        [DataMember]
        public decimal AudioVolume;
    }
}
