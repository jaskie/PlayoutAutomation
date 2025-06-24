using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class MediaExportDescription
    {
        private readonly List<IMedia> _logos = new List<IMedia>();
        public MediaExportDescription(IMedia media, IEnumerable<IMedia> logos, TimeSpan startTC, TimeSpan duration, double audioVolume)
        {
            Media = media;
            _logos = new List<IMedia>(logos);
            StartTC = startTC;
            Duration = duration;
            AudioVolume = audioVolume;
        }

        public IMedia Media { get; private set; }
        
        public IEnumerable<IMedia> Logos => _logos;

        public TimeSpan Duration;

        public TimeSpan StartTC;

        public double AudioVolume;

        public void AddLogo(IMedia logo)
        {
            _logos.Add(logo);
        }

        public void RemoveLogo(IMedia logo)
        {
            _logos.Remove(logo);
        }

        public override string ToString()
        {
            return Media.MediaName;
        }
    }
}
