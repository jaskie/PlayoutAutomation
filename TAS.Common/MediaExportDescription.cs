using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class MediaExportDescription
    {
        public MediaExportDescription(IMedia media, List<IMedia> logos, TimeSpan startTC, TimeSpan duration, double audioVolume)
        {
            Media = media;
            Logos = logos;
            StartTC = startTC;
            Duration = duration;
            AudioVolume = audioVolume;
        }

        public IMedia Media { get; private set; }
        
        public List<IMedia> Logos { get; }

        public TimeSpan Duration;

        public TimeSpan StartTC;

        public double AudioVolume;

        public void AddLogo(IMedia logo)
        {
            Logos.Add(logo);
        }

        public void RemoveLogo(IMedia logo)
        {
            Logos.Remove(logo);
        }

        public override string ToString()
        {
            return Media.MediaName;
        }
    }
}
