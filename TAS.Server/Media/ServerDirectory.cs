using System;
using System.Collections.Generic;
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

        public ServerDirectory(IPlayoutServerProperties server, MediaManager manager)
            : base(manager)
        {
            Server = server;
        }

        public override void Initialize()
        {
            if (!IsInitialized)
            {
                EngineController.Database.LoadServerDirectory<ServerMedia>(this, Server.Id);
                base.Initialize();
                Debug.WriteLine(this, "Directory initialized");
            }
        }

        public override void Refresh() { }

        public override void AddMedia(IMedia media)
        {
            base.AddMedia(media);
            if (media.MediaStatus != TMediaStatus.Required && File.Exists(((MediaBase)media).FullPath))
                ThreadPool.QueueUserWorkItem(o => media.Verify());
        }

        public override void RemoveMedia(IMedia media)
        {
            if (!(media is ServerMedia sm))
                throw new ApplicationException("Media provided to RemoveMedia is not ServerMedia");
            sm.MediaStatus = TMediaStatus.Deleted;
            sm.IsVerified = false;
            sm.Save();
            base.RemoveMedia(sm);
        }

        public override void SweepStaleMedia()
        {
            DateTime currentDateTime = DateTime.UtcNow.Date;
            IEnumerable<IMedia> staleMediaList = FindMediaList(m => (m is ServerMedia) && currentDateTime > (m as ServerMedia).KillDate);
            foreach (var media in staleMediaList)
            {
                var m = (MediaBase)media;
                m.Delete();
            }
        }

        internal override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            var newFileName = mediaProperties.FileName;
            if (File.Exists(Path.Combine(Folder, newFileName)))
            {
                Logger.Trace("{0}: File {1} already exists", nameof(CreateMedia), newFileName);
                newFileName = FileUtils.GetUniqueFileName(Folder, newFileName);
            }
            var result = (new ServerMedia
            {
                MediaName = mediaProperties.MediaName,
                MediaGuid = FindMediaByMediaGuid(mediaProperties.MediaGuid)?.MediaGuid ?? Guid.NewGuid(),
                LastUpdated = mediaProperties.LastUpdated,
                MediaType = mediaProperties.MediaType == TMediaType.Unknown ? TMediaType.Movie : mediaProperties.MediaType,
                Folder = mediaProperties.Folder,
                FileName = newFileName,
                MediaStatus = TMediaStatus.Required,
            });
            result.CloneMediaProperties(mediaProperties);
            AddMedia(result);
            return result;
        }

        public event EventHandler<MediaEventArgs> MediaSaved;

        protected override bool AcceptFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return false;
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            return FileUtils.VideoFileTypes.Contains(ext) || FileUtils.StillFileTypes.Contains(ext);
        }

        internal virtual void OnMediaSaved(MediaBase media)
        {
            MediaSaved?.Invoke(this, new MediaEventArgs(media));
        }

        protected override void OnMediaRenamed(MediaBase media, string newFullPath)
        {
            ((ServerMedia)media).Save();
            base.OnMediaRenamed(media, newFullPath);
        }

        protected override void EnumerateFiles(string directory, string filter, bool includeSubdirectories, CancellationToken cancelationToken)
        {
            base.EnumerateFiles(directory, filter, includeSubdirectories, cancelationToken);
            var unverifiedFiles = FindMediaList(mf => ((ServerMedia)mf).IsVerified == false);
            unverifiedFiles.ForEach(media => media.Verify());
        }

        protected override IMedia AddMediaFromPath(string fullPath, DateTime lastUpdated)
        {
            if (!AcceptFile(fullPath))
                return null;
            var newMedia = FindMediaFirstByFullPath(fullPath) as ServerMedia;
            if (newMedia != null || !AcceptFile(fullPath))
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
                MediaStatus = TMediaStatus.Available,
                IsVerified = true
            };
            AddMedia(newMedia);
            newMedia.Save();
            return newMedia;
        }
    }
}