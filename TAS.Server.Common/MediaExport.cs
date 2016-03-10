using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class MediaExport
    {
        public MediaExport(IMedia media, List<IMedia> logos, TimeSpan startTC, TimeSpan duration, decimal audioVolume)
        {
            this.Media = media;
            this.Logos = logos;
            this.StartTC = startTC;
            this.Duration = duration;
            this.AudioVolume = audioVolume;
            ExportWithLogo = logos.Count > 0;
        }
        public IMedia Media { get; private set; }
        public List<IMedia> Logos { get; private set; }
        public bool ExportWithLogo;
        public TimeSpan Duration;
        public TimeSpan StartTC;
        public decimal AudioVolume;
    }
}
