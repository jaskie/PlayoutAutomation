using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using TAS.Common;
using TAS.Data;

namespace TAS.Server
{
    public class ServerDirectory : MediaDirectory
    {
        public readonly PlayoutServer Server;
        public ServerDirectory(PlayoutServer server)
            : base()
        {
            Server = server;
            Extensions = new string[MediaDirectory.VideoFileTypes.Length + MediaDirectory.StillFileTypes.Length];
            MediaDirectory.VideoFileTypes.CopyTo(Extensions, 0);
            MediaDirectory.StillFileTypes.CopyTo(Extensions, MediaDirectory.VideoFileTypes.Length);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Initialize()
        {
            this.Load();
            VerifyMedia();
            base.Initialize();
            Debug.WriteLine(this, "Directory initialized");
        }

        public override void Refresh()
        {

        }

        protected override Media CreateMedia(string fileNameOnly)
        {
            ServerMedia newMedia = new ServerMedia(this)
            {
                FileName = fileNameOnly,
            };
            return newMedia;
        }

        public event EventHandler<MediaEventArgs> MediaSaved;
        internal virtual void OnMediaSaved(Media media)
        {
            var handler = MediaSaved;
            if (handler != null)
                handler(media, new MediaEventArgs(media));
        }

        public override void MediaAdd(Media media)
        {
            base.MediaAdd(media);
            media.PropertyChanged += OnMediaPropertyChanged;
            if (media.MediaStatus != TMediaStatus.Required && File.Exists(media.FullPath))
                ThreadPool.QueueUserWorkItem(o => media.Verify());
        }

        public override void MediaRemove(Media media)
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
            if (this.MediaPropertyChanged != null)
                this.MediaPropertyChanged(o, e);
        }

        public override void SweepStaleMedia()
        {
            DateTime currentDateTime = DateTime.UtcNow.Date;
            IEnumerable<Media> StaleMediaList;
            _files.Lock.EnterReadLock();
            try
            {
                StaleMediaList = _files.Where(m => (m is ServerMedia) && currentDateTime > (m as ServerMedia).KillDate);
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
            foreach (Media m in StaleMediaList)
                m.Delete();
        }

        public ServerMedia GetServerMedia(Media media, bool searchExisting = true)
        {
            if (media == null)
                return null;
            ServerMedia fm = null;
            _files.Lock.EnterUpgradeableReadLock();
            try
            {
                fm = (ServerMedia)FindMedia(media);
                if (fm == null || !searchExisting)
                {
                    _files.Lock.EnterWriteLock();
                    try
                    {
                        fm = (new ServerMedia(this)
                        {
                            MediaName = media.MediaName,
                            Folder = string.Empty,
                            FileName = (media is IngestMedia) ? (VideoFileTypes.Any(ext => ext == Path.GetExtension(media.FileName).ToLower()) ? Path.GetFileNameWithoutExtension(media.FileName) : media.FileName) + DefaultFileExtension(media.MediaType) : media.FileName,
                            MediaType = (media.MediaType == TMediaType.Unknown) ? (StillFileTypes.Any(ve => ve == Path.GetExtension(media.FullPath).ToLowerInvariant()) ? TMediaType.Still : TMediaType.Movie) : media.MediaType,
                            MediaStatus = TMediaStatus.Required,
                            TCStart = media.TCStart,
                            TCPlay = media.TCPlay,
                            Duration = media.Duration,
                            DurationPlay = media.DurationPlay,
                            VideoFormat = media.VideoFormat,
                            AudioChannelMapping = media.AudioChannelMapping,
                            AudioVolume = media.AudioVolume,
                            AudioLevelIntegrated = media.AudioLevelIntegrated,
                            AudioLevelPeak = media.AudioLevelPeak,
                            KillDate = default(DateTime),
                            DoNotArchive = (media is ServerMedia && (media as ServerMedia).DoNotArchive),
                            MediaCategory = media.MediaCategory,
                            Parental = media.Parental,
                            idAux = (media is PersistentMedia) ? (media as PersistentMedia).idAux : string.Empty,
                            idProgramme = (media is PersistentMedia) ? (media as PersistentMedia).idProgramme : 0L,
                            MediaGuid = fm == null ? media.MediaGuid : Guid.NewGuid(), // in case file with the same GUID already exists and we need to get new one
                            OriginalMedia = media,
                        });
                    }
                    finally
                    {
                        _files.Lock.ExitWriteLock();
                    }
                    fm.PropertyChanged += MediaPropertyChanged;
                }
                else
                    if (fm.MediaStatus == TMediaStatus.Deleted)
                        fm.MediaStatus = TMediaStatus.Required;
            }
            finally
            {
                _files.Lock.ExitUpgradeableReadLock();
            }
            return fm;
        }

        public override Media FindMedia(Media media)
        {
            if (media is ServerMedia && media.Directory == this)
                return media;
            if (media == null)
                return null;
            _files.Lock.EnterReadLock();
            try
            {
                return _files.FirstOrDefault(m => m.MediaGuid == (media.MediaGuid));
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
        }

        protected override void OnMediaRenamed(Media media, string newName)
        {
            base.OnMediaRenamed(media, newName);
            ((ServerMedia)media).Save();
        }

        public void VerifyMedia()
        {
            _files.Lock.EnterReadLock();
            try
            {
                var unverifiedFiles = _files.Where(mf => ((ServerMedia)mf).Verified == false).ToList();
                unverifiedFiles.ForEach(media => media.Verify());
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
        }

    }
}