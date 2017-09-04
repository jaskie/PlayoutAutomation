using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    [DataContract]
    public class MediaExportDescription
    {
        public MediaExportDescription(IMedia media, List<IMedia> logos, TimeSpan startTC, TimeSpan duration, double audioVolume)
        {
            Media = media;
            Logos = logos.ToArray();
            StartTC = startTC;
            Duration = duration;
            AudioVolume = audioVolume;
        }
        [DataMember]
        public IMedia Media { get; private set; }
        [DataMember]
        public IMedia[] Logos { get; private set; }
        [DataMember]
        public TimeSpan Duration;
        [DataMember]
        public TimeSpan StartTC;
        [DataMember]
        public double AudioVolume;

        public void AddLogo(IMedia logo)
        {
            var logos = Logos.AsEnumerable().ToList();
            logos.Add(logo);
            Logos = logos.ToArray();
        }
        public void RemoveLogo(IMedia logo)
        {
            var logos = Logos.AsEnumerable().ToList();
            logos.Remove(logo);
            Logos = logos.ToArray();
        }
        public override string ToString()
        {
            return Media.MediaName;
        }
    }
}
