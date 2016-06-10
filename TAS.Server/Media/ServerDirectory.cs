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
            var handler = MediaSaved;
            if (handler != null)
                handler(this, new MediaEventArgs(media));
        }

        public override void MediaAdd(IMedia media)
        {
            base.MediaAdd(media);
            media.PropertyChanged += OnMediaPropertyChanged;
            if (media.MediaStatus != TMediaStatus.Required && File.Exists(media.FullPath))
                ThreadPool.QueueUserWorkItem(o => ((Media)media).Verify());
        }

        public override void MediaRemove(IMedia media)
        {
            ServerMedia m = (ServerMedia)media;
            m.MediaStatus = TMediaStatus.Deleted;
            m.Verified = false;
            m.Save();
            media.PropertyChanged -= OnMediaPropertyChanged;
            base.MediaRemove(media);
        }

        public event PropertyChangedEventHandler MediaPropertyChanged;

        internal virtual void OnMediaPropertyChanged(object o, PropertyChangedEventArgs e)
        {
            var handler = MediaPropertyChanged;
            if (handler != null)
                handler(o, e);
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
            ServerMedia fm = (ServerMedia)FindMediaByMediaGuid(media.MediaGuid); // check if need to select new Guid
            if (fm == null || !searchExisting)
            {
                string newFileName = Path.Combine(_folder,
                    FileUtils.GetUniqueFileName(_folder,
                        media is IngestMedia
                            ? ((media.MediaType == TMediaType.Still ? FileUtils.StillFileTypes : media.MediaType == TMediaType.Audio ? FileUtils.AudioFileTypes : FileUtils.VideoFileTypes).Any(ext => ext == Path.GetExtension(media.FileName).ToLower()) ? Path.GetFileNameWithoutExtension(media.FileName) : media.FileName) + FileUtils.DefaultFileExtension(media.MediaType)
                            : media.FileName));
                fm = (new ServerMedia(this, fm == null ? media.MediaGuid : Guid.NewGuid(), 0, MediaManager.ArchiveDirectory) // in case file with the same GUID already exists and we need to get new one
                {
                    MediaName = media.MediaName,
                    FullPath = newFileName,
                    MediaType = media.MediaType == TMediaType.Unknown ? TMediaType.Movie : media.MediaType,
                    MediaStatus = TMediaStatus.Required,
                });
                fm.CloneMediaProperties(media);
                NotifyMediaAdded(fm);
                fm.PropertyChanged += MediaPropertyChanged;
            }
            else
                if (fm.MediaStatus == TMediaStatus.Deleted)
                    fm.MediaStatus = TMediaStatus.Required;
            return fm;
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