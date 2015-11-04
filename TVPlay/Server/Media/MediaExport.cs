using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class MediaExport
    {
        public MediaExport(IMedia media, TimeSpan startTC, TimeSpan duration, decimal audioVolume)
        {
            this.Media = media;
            this.StartTC = startTC;
            this.Duration = duration;
            this.AudioVolume = audioVolume;
        }
        public IMedia Media { get; private set; }
        public TimeSpan Duration; 
        public TimeSpan StartTC;
        public decimal AudioVolume;             
    }
}
