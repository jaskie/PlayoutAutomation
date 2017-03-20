using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using TAS.Common;
using TAS.Server.Database;
using TAS.Server.Interfaces;
using TAS.Server.Common;

namespace TAS.Server
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
            if (!_isInitialized)
            {
                this.Load<ServerMedia>(MediaManager.ArchiveDirectory, Server.Id);
                base.Initialize();
                Debug.WriteLine(this, "Directory initialized");
            }
        }

        public override void Refresh() { }

        protected override IMedia CreateMedia(string fullPath, Guid guid)
        {
            return new ServerMedia(this, guid, 0, MediaManager.ArchiveDirectory)
            {
                FullPath = fullPath,
            };
        }

        protected override bool AcceptFile(string fullPath)
        {
            string ext = Path.GetExtension(fullPath).ToLowerInvariant();
            return FileUtils.VideoFileTypes.Contains(ext) || FileUtils.StillFileTypes.Contains(ext);
        }

        public event EventHandler<MediaEventArgs> MediaSaved;
        internal virtual void OnMediaSaved(Media media)
        {
            MediaSaved?.Invoke(this, new MediaEventArgs(media));
        }

        public override void MediaAdd(Media media)
        {
            base.MediaAdd(media);
            if (media.MediaStatus != TMediaStatus.Required && File.Exists(media.FullPath))
                ThreadPool.QueueUserWorkItem(o => ((Media)media).Verify());
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
            IEnumerable<IMedia> StaleMediaList = FindMediaList(m => (m is ServerMedia) && currentDateTime > (m as ServerMedia).KillDate);
            foreach (Media m in StaleMediaList)
                m.Delete();
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            var newFileName = mediaProperties.FileName;
            if (File.Exists(Path.Combine(_folder, newFileName)))
            {
                Logger.Trace("{0}: File {1} already exists", nameof(CreateMedia), newFileName);
                newFileName = FileUtils.GetUniqueFileName(_folder, newFileName);
            }
            var newFileFullPath = Path.Combine(_folder, newFileName);
            var result = (new ServerMedia(this, FindMediaByMediaGuid(mediaProperties.MediaGuid) == null ? mediaProperties.MediaGuid : Guid.NewGuid(), 0, MediaManager.ArchiveDirectory)
            {
                FullPath = newFileFullPath,
                MediaType = mediaProperties.MediaType == TMediaType.Unknown ? TMediaType.Movie : mediaProperties.MediaType,
                MediaStatus = TMediaStatus.Required,
            });
            result.CloneMediaProperties(mediaProperties);
            return result;
        }

        protected override void OnMediaRenamed(Media media, string newFullPath)
        {
            ((ServerMedia)media).Save();
            base.OnMediaRenamed(media, newFullPath);
        }

        protected override void EnumerateFiles(string directory, string filter, bool includeSubdirectories, CancellationToken cancelationToken)
        {
            base.EnumerateFiles(directory, filter, includeSubdirectories, cancelationToken);
            var unverifiedFiles = _files.Values.Where(mf => ((ServerMedia)mf).IsVerified == false).Cast<Media>().ToList();
            unverifiedFiles.ForEach(media => media.Verify());
        }
    }
}