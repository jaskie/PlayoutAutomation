using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public class AnimationDirectory : WatcherDirectory, IAnimationDirectory
    {
        public readonly CasparServer Server;

        internal AnimationDirectory(CasparServer server, MediaManager manager) : base(manager)
        {
            Server = server;
        }

        public override void Initialize()
        {
            if (IsInitialized)
                return;
            EngineController.Database.LoadAnimationDirectory<AnimatedMedia>(this, Server.Id);
            BeginWatch("*", false, TimeSpan.Zero);
            Debug.WriteLine(Server.AnimationFolder, "AnimationDirectory initialized");
        }

        internal override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }

        public void CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            var result = new AnimatedMedia
            {
                LastUpdated = source.LastUpdated,
                MediaGuid = newMediaGuid == Guid.Empty ? source.MediaGuid : newMediaGuid,
                Folder = source.Folder,
                FileName = source.FileName,
                Directory = this
            };
            result.CloneMediaProperties(source);
            result.MediaStatus = source.MediaStatus;
            result.LastUpdated = DateTime.UtcNow;
            AddMedia(result);
            result.Save();
        }

        public override void RemoveMedia(IMedia media)
        {
            if (!(media is AnimatedMedia am))
                throw new ArgumentException(nameof(media));
            am.MediaStatus = TMediaStatus.Deleted;
            am.IsVerified = false;
            am.Save();
            base.RemoveMedia(am);
        }

        public override void SweepStaleMedia() { }


        public event EventHandler<MediaEventArgs> MediaSaved;

        protected override bool AcceptFile(string fullPath)
        {
            return !string.IsNullOrWhiteSpace(fullPath) 
                && FileUtils.AnimationFileTypes.Contains(Path.GetExtension(fullPath).ToLowerInvariant());
        }

        protected override IMedia AddMediaFromPath(string fullPath, DateTime lastUpdated)
        {
            if (!AcceptFile(fullPath))
                return null;
            if (FindMediaFirstByFullPath(fullPath) is AnimatedMedia newMedia)
                return newMedia;
            var relativeName = fullPath.Substring(Folder.Length);
            var fileName = Path.GetFileName(relativeName);
            newMedia = new AnimatedMedia
            {
                MediaName = FileUtils.GetFileNameWithoutExtension(fullPath, TMediaType.Animation).ToUpper(),
                LastUpdated = lastUpdated,
                MediaType = TMediaType.Animation,
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

        internal void OnMediaSaved(AnimatedMedia animatedMedia)
        {
            MediaSaved?.Invoke(this, new MediaEventArgs(animatedMedia));
        }
    }
}
