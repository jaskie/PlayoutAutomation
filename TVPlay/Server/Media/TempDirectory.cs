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

        protected override Media CreateMedia(string fileNameOnly)
        {
            return new TempMedia(this) { FileName = fileNameOnly };
        }

        protected override Media CreateMedia(string fileNameOnly, Guid guid)
        {
            return new TempMedia(this, guid) { FileName = fileNameOnly };
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
            return new TempMedia(this, media);
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
