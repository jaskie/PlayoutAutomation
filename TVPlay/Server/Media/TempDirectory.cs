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
            throw new InvalidOperationException("Temp media must have OriginalMedia property. Use Get() to acquire one.");
        }

        protected override Media CreateMedia(string fileNameOnly, Guid guid)
        {
            throw new InvalidOperationException("Temp media must have OriginalMedia property. Use Get() to acquire one.");
        }

        public override void MediaAdd(Media media)
        {
            // do not add to _files
        }

        public override void Refresh()
        {
            
        }
        
        public TempMedia CreateMedia(Media media, string fileExtension = null)
        {
            return new TempMedia(this, media, fileExtension);
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
