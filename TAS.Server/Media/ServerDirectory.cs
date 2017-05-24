using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ServerDirectory : MediaDirectory, IServerDirectory
    {
        internal readonly IPlayoutServer Server;

        public ServerDirectory(IPlayoutServer server, MediaManager manager)
            : base(manager)
        {
            Server = server;
        }

        public override void Initialize()
        {
            if (!IsInitialized)
            {
                this.Load<ServerMedia>(MediaManager.ArchiveDirectory, Server.Id);
                base.Initialize();
                Debug.WriteLine(this, "Directory initialized");
            }
        }

        public override void Refresh() { }

        public override void MediaAdd(MediaBase media)
        {
            base.MediaAdd(media);
            if (media.MediaStatus != TMediaStatus.Required && File.Exists(media.FullPath))
                ThreadPool.QueueUserWorkItem(o => media.Verify());
        }

        public override void MediaRemove(IMedia media)
        {
            media.MediaStatus = TMediaStatus.Deleted;
            media.IsVerified = false;
            (media as ServerMedia)?.Save();
            base.MediaRemove(media);
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
            var newFileFullPath = Path.Combine(Folder, newFileName);
            var result = (new ServerMedia(this, FindMediaByMediaGuid(mediaProperties.MediaGuid) == null ? mediaProperties.MediaGuid : Guid.NewGuid(), 0, MediaManager.ArchiveDirectory)
            {
                FullPath = newFileFullPath,
                MediaType = mediaProperties.MediaType == TMediaType.Unknown ? TMediaType.Movie : mediaProperties.MediaType,
                MediaStatus = TMediaStatus.Required,
            });
            result.CloneMediaProperties(mediaProperties);
            return result;
        }

        public event EventHandler<MediaEventArgs> MediaSaved;

        protected override IMedia CreateMedia(string fullPath, Guid guid = new Guid())
        {
            return new ServerMedia(this, guid, 0, MediaManager.ArchiveDirectory)
            {
                FullPath = fullPath,
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
            var unverifiedFiles = Files.Values.Where(mf => ((ServerMedia)mf).IsVerified == false).ToList();
            unverifiedFiles.ForEach(media => media.Verify());
        }
    }
}