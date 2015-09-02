using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server
{
    public class MediaExport
    {
        public MediaExport(Media media, TimeSpan startTC, TimeSpan duration)
        {
            this.Media = media;
            this.StartTC = startTC;
            this.Duration = duration;
        }
        public Media Media { get; private set; }
        public TimeSpan Duration; 
        public TimeSpan StartTC;
    }
}
