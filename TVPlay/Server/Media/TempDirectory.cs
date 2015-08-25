using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace TAS.Server
{
    public class TempDirectory: MediaDirectory
    {
        public TempDirectory()
        {
            _folder = ConfigurationManager.AppSettings["TempDirectory"];
        }

        protected override Media CreateMedia()
        {
            return new TempMedia() { Directory = this };
        }

        public override void MediaAdd(Media media)
        {
            // do not add to _files
        }

        public override void Refresh()
        {
            
        }
        
        public TempMedia Get(Media media)
        {
            return new TempMedia() { Directory = this, OriginalMedia = media, _fileName = media.MediaGuid.ToString() + Path.GetExtension(media.FileName), };
        }

        public override void SweepStaleMedia()
        {
            foreach (string fileName in Directory.GetFiles(_folder))
                try
                {
                    File.Delete(fileName);
                }
                catch { }
        }
    }
}
