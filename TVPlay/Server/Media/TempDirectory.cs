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
            return new TempMedia() { };
        }

        public override void MediaAdd(Media media)
        {
            // do not add to _files
        }

        public override void Refresh()
        {
            
        }
        
        public TempMedia Get(Media media, string fileExtension = null)
        {
            return new TempMedia() { MediaName = media.MediaName, OriginalMedia = media, _fileName = string.Format("{0}{1}", Guid.NewGuid().ToString(), fileExtension == null ? Path.GetExtension(media.FileName): fileExtension), Directory = this};
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
