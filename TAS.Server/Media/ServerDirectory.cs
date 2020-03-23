using jNet.RPC;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public class ServerDirectory : WatcherDirectory, IServerDirectory
    {
        internal readonly IPlayoutServerProperties Server;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ServerDirectory(IPlayoutServerProperties server)
        {
            Server = server;
            IsRecursive = server.IsMediaFolderRecursive;
            MovieContainerFormat = server.MovieContainerFormat;
            HaveFileWatcher = true;
        }

        internal bool RequiresInitialization { get; set; }

        [DtoField]
        public bool IsRecursive { get; }

        public TMovieContainerFormat MovieContainerFormat { get; }

        public override void Initialize()
        {
            if (IsInitialized)
                return;
            EngineController.Current.Database.LoadServerDirectory<ServerMedia>(this, Server.Id);
            IsInitialized = true;
            BeginWatch(IsRecursive);
            Debug.WriteLine(this, "Directory initialized");
        }

        public override void RemoveMedia(IMedia media)
        {
            if (!(media is ServerMedia sm))
                throw new ArgumentException(nameof(media));
            sm.MediaStatus = TMediaStatus.Deleted;
            sm.IsVerified = false;
            sm.Save();
            base.RemoveMedia(sm);
        }

        public override void SweepStaleMedia()
        {
            var currentDateTime = DateTime.UtcNow.Date;
            var staleMediaList = FindMediaList(m => m is ServerMedia && currentDateTime > ((ServerMedia) m).KillDate);
            foreach (var media in staleMediaList)
            {
                var m = (MediaBase)media;
                m.Delete();
            }
        }

        public event EventHandler<MediaEventArgs> MediaSaved;

        public event EventHandler<MediaIngestStatusEventArgs> IngestStatusUpdated;

        internal override IMedia CreateMedia(IMediaProperties media)
        {
            var newFileName = media.FileName;
            if (IsRecursive && media is ServerMedia && !string.IsNullOrEmpty(media.Folder))
            {
                if (File.Exists(Path.Combine(Folder, media.Folder, newFileName)))
                {
                    Logger.Trace("{0}: File {1} already exists", nameof(CreateMedia), newFileName);
                    newFileName = FileUtils.GetUniqueFileName(Path.Combine(Folder, media.Folder), newFileName);
                }
            }
            else if (File.Exists(Path.Combine(Folder, newFileName)))
            {
                Logger.Trace("{0}: File {1} already exists", nameof(CreateMedia), newFileName);
                newFileName = FileUtils.GetUniqueFileName(Folder, newFileName);
            }
            var result = (new ServerMedia
            {
                MediaGuid = media.MediaGuid == Guid.Empty || FindMediaByMediaGuid(media.MediaGuid) != null ? Guid.NewGuid() : media.MediaGuid,
                LastUpdated = media.LastUpdated,
                MediaType = media.MediaType == TMediaType.Unknown ? TMediaType.Movie : media.MediaType,
                Folder = IsRecursive && media is IServerMedia && !string.IsNullOrEmpty(media.Folder) ? media.Folder : string.Empty,
                FileName = newFileName,
                MediaStatus = TMediaStatus.Required,
            });
            result.CloneMediaProperties(media);
            AddMedia(result);
            return result;
        }


        internal void NotifyIngestStatusUpdated(IMedia media, TIngestStatus ingestStatus)
        {
            IngestStatusUpdated?.Invoke(this, new MediaIngestStatusEventArgs(media, ingestStatus));
        }


        protected override bool AcceptFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return false;
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            return FileUtils.VideoFileTypes.Contains(ext) || FileUtils.StillFileTypes.Contains(ext);
        }

        internal void OnMediaSaved(MediaBase media)
        {
            MediaSaved?.Invoke(this, new MediaEventArgs(media));
        }

        protected override void OnMediaRenamed(MediaBase media, string newFullPath)
        {
            ((ServerMedia)media).Save();
            base.OnMediaRenamed(media, newFullPath);
        }

        protected override void EnumerateFiles(string directory, bool includeSubdirectories, CancellationToken cancelationToken)
        {
            base.EnumerateFiles(directory, includeSubdirectories, cancelationToken);
            var unverifiedFiles = FindMediaList(mf => ((ServerMedia)mf).IsVerified == false);
            unverifiedFiles.ForEach(media => media.Verify(true));
        }

        protected override IMedia AddMediaFromPath(string fullPath, DateTime lastUpdated)
        {
            if (!AcceptFile(fullPath))
                return null;
            if (FindMediaFirstByFullPath(fullPath) is ServerMedia newMedia)
                return newMedia;
            var relativeName = fullPath.Substring(Folder.Length);
            var fileName = Path.GetFileName(relativeName);
            var mediaType = FileUtils.VideoFileTypes.Contains(Path.GetExtension(fullPath).ToLowerInvariant()) ? TMediaType.Movie : TMediaType.Still;
            newMedia = new ServerMedia
            {
                MediaName = FileUtils.GetFileNameWithoutExtension(fullPath, mediaType).ToUpper(),
                LastUpdated = lastUpdated,
                MediaType = mediaType,
                MediaGuid = Guid.NewGuid(),
                FileName = Path.GetFileName(relativeName),
                Folder = relativeName.Substring(0, relativeName.Length - fileName.Length).Trim(PathSeparator),
            };
            AddMedia(newMedia);
            newMedia.Save();
            return newMedia;
        }

        public override string ToString()
        {
            return Folder;
        }
    }
}