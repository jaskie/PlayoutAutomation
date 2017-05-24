using System;
using System.Configuration;
using System.IO;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Media
{
    public class TempDirectory: MediaDirectory
    {
        public TempDirectory(MediaManager manager): base(manager)
        {
            Folder = ConfigurationManager.AppSettings["TempDirectory"];
        }

        public override void MediaAdd(MediaBase media)
        {
            // do not add to _files
        }

        public override void Refresh() { }

        public override IMedia CreateMedia(IMediaProperties media)
        {
            return new TempMedia(this, media);
        }

        public override void SweepStaleMedia()
        {
            foreach (string fileName in Directory.GetFiles(Folder))
                try
                {
                    File.Delete(fileName);
                }
                catch { }
        }

        protected override IMedia CreateMedia(string fileNameOnly, Guid guid = new Guid())
        {
            throw new NotImplementedException();
        }

        protected override void Reinitialize() {}

    }
}
