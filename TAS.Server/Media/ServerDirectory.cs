using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ServerDirectory : MediaDirectory, IServerDirectory
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

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
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

        protected override IMedia CreateMedia(string fullPath, string mediaName, DateTime lastUpdated, TMediaType mediaType, Guid guid = default(Guid))
        {
            var relativeName = fullPath.Substring(Folder.Length);
            var fileName = Path.GetFileName(relativeName);
            return new ServerMedia
            {
                MediaName = mediaName,
                MediaType = mediaType,
                MediaGuid = guid,
                LastUpdated = lastUpdated,
                FileName = fileName,
                Folder = relativeName.Substring(0, relativeName.Length - fileName.Length).Trim(PathSeparator)
            };
        }

        protected override bool AcceptFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return false;
            string ext = Path.GetExtension(fullPath).ToLowerInvariant();
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

        protected override IMedia AddFile(string fullPath, DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            IMedia media = FindMediaFirstByFullPath(fullPath);
            if (media != null)
                return media;
            media = base.AddFile(fullPath, lastWriteTime, guid);
            if (media != null)
                Logger.Warn("Unknown media added to server directory: {0}", fullPath);
            return media;
        }
    }
}