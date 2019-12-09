using NLog;
using System;
using System.Configuration;
using System.IO;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public class TempDirectory: MediaDirectoryBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static TempDirectory Current { get; } = new TempDirectory();

        private TempDirectory()
        {
            Folder = ConfigurationManager.AppSettings["TempDirectory"];
            SweepStaleMedia();
        }

        public override void RemoveMedia(IMedia media)
        {
        }

        public override IMediaSearchProvider Search(TMediaCategory? category, string searchString)
        {
            throw new NotImplementedException();
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

        public override string ToString()
        {
            return "TEMP";
        }
    }
}
