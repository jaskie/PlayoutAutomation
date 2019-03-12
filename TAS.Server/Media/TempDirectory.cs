using NLog;
using System;
using System.Configuration;
using System.IO;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class TempDirectory: MediaDirectoryBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public TempDirectory()
        {
            Folder = ConfigurationManager.AppSettings["TempDirectory"];
            SweepStaleMedia();
        }

        public override void RemoveMedia(IMedia media)
        {
        }

        internal override IMedia CreateMedia(IMediaProperties media)
        {
            if (!DirectoryExists())
                throw new DirectoryNotFoundException(Folder);
            return new TempMedia(this, media);
        }
        
        private void SweepStaleMedia()
        {
            if (!DirectoryExists())
                return;
            foreach (string fileName in Directory.GetFiles(Folder))
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
        }
    }
}
