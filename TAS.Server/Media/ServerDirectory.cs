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
            ServerMedia m = (ServerMedia)media;
            m.MediaStatus = TMediaStatus.Deleted;
            m.Verified = false;
            m.Save();
            base.MediaRemove(media);
        }

        public override void SweepStaleMedia()
        {
            DateTime currentDateTime = DateTime.UtcNow.Date;
            IEnumerable<IMedia> StaleMediaList = FindMediaList(m => (m is ServerMedia) && currentDateTime > (m as ServerMedia).KillDate);
            foreach (Media m in StaleMediaList)
                m.Delete();
        }

        public IServerMedia GetServerMedia(IMedia media, bool searchExisting = true)
        {
            if (media == null)
                return null;
            ServerMedia result = (ServerMedia)FindMediaByMediaGuid(media.MediaGuid); // check if need to select new Guid
            if (result == null || !searchExisting)
            {
                string newFileFullPath = null;
                if (searchExisting)
                {
                    FileInfo fi = new FileInfo(Path.Combine(_folder, media.Folder, media.FileName));
                    if (fi.Exists && (ulong)fi.Length == media.FileSize && fi.LastWriteTimeUtc == media.LastUpdated)
                        newFileFullPath = fi.FullName;
                }
                if (string.IsNullOrWhiteSpace(newFileFullPath))
                    newFileFullPath = Path.Combine(_folder,
                        FileUtils.GetUniqueFileName(_folder,
                            media is IngestMedia
                                ? ((media.MediaType == TMediaType.Still ? FileUtils.StillFileTypes : media.MediaType == TMediaType.Audio ? FileUtils.AudioFileTypes : FileUtils.VideoFileTypes).Any(ext => ext == Path.GetExtension(media.FileName).ToLower()) ? Path.GetFileNameWithoutExtension(media.FileName) : media.FileName) + FileUtils.DefaultFileExtension(media.MediaType)
                                : media.FileName));
                result = (new ServerMedia(this, result == null ? media.MediaGuid : Guid.NewGuid(), 0, MediaManager.ArchiveDirectory) // in case file with the same GUID already exists and we need to get new one
                {
                    MediaName = media.MediaName,
                    FullPath = newFileFullPath,
                    MediaType = media.MediaType == TMediaType.Unknown ? TMediaType.Movie : media.MediaType,
                    MediaStatus = TMediaStatus.Required,
                });
                result.CloneMediaProperties(media);
                NotifyMediaAdded(result);
            }
            else
                if (result.MediaStatus == TMediaStatus.Deleted)
                    result.MediaStatus = TMediaStatus.Required;
            return result;
        }

        protected override void OnMediaRenamed(Media media, string newName)
        {
            ((ServerMedia)media).Save();
        }

        protected override void EnumerateFiles(string directory, string filter, bool includeSubdirectories, CancellationToken cancelationToken)
        {
            base.EnumerateFiles(directory, filter, includeSubdirectories, cancelationToken);
            var unverifiedFiles = _files.Values.Where(mf => ((ServerMedia)mf).Verified == false).Cast<Media>().ToList();
            unverifiedFiles.ForEach(media => media.Verify());
        }
    }
}