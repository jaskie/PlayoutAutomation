using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class AnimationDirectory : MediaDirectory, IAnimationDirectory
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
            DirectoryName = "Animacje";
            EngineController.Database.LoadAnimationDirectory<AnimatedMedia>(this, Server.Id);
            base.Initialize();
            Debug.WriteLine(Server.AnimationFolder, "AnimationDirectory initialized");
        }

        public override void Refresh()
        {

        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }

        public IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            var result = new AnimatedMedia
            {
                MediaName = source.MediaName,
                LastUpdated = source.LastUpdated,
                MediaGuid = source.MediaGuid,
                Folder = source.Folder,
                FileName = source.FileName
            };
            result.CloneMediaProperties(source);
            result.MediaStatus = source.MediaStatus;
            result.LastUpdated = DateTime.UtcNow;
            result.Save();
            return result;
        }

        public override void RemoveMedia(IMedia media)
        {
            media.MediaStatus = TMediaStatus.Deleted;
            ((AnimatedMedia)media).IsVerified = false;
            ((AnimatedMedia)media).Save();
            base.RemoveMedia(media);
        }

        public override void SweepStaleMedia() { }

        protected override bool AcceptFile(string fullPath)
        {
            return !string.IsNullOrWhiteSpace(fullPath) 
                && FileUtils.AnimationFileTypes.Contains(Path.GetExtension(fullPath).ToLowerInvariant());
        }

        protected override IMedia AddFile(string fullPath, DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            var newMedia = FindMediaFirstByFullPath(fullPath) as AnimatedMedia;
            if (newMedia == null && AcceptFile(fullPath))
            {
                var mediaName = FileUtils.GetFileNameWithoutExtension(fullPath, TMediaType.Animation).ToUpper();
                var lastUpdated = lastWriteTime == default(DateTime) ? File.GetLastWriteTimeUtc(fullPath) : lastWriteTime;
                newMedia = (AnimatedMedia)CreateMedia(fullPath, mediaName, lastUpdated, TMediaType.Animation, guid);
                newMedia.MediaStatus = TMediaStatus.Available;
                AddMedia(newMedia);
                newMedia.Save();
            }
            return newMedia;
        }

        protected override IMedia CreateMedia(string fullPath, string mediaName, DateTime lastUpdated, TMediaType mediaType, Guid guid = default(Guid))
        {
            var relativeName = fullPath.Substring(Folder.Length);
            var fileName = Path.GetFileName(relativeName);
            return new AnimatedMedia
            {
                MediaName = mediaName,
                LastUpdated = lastUpdated,
                MediaGuid = guid,
                IsVerified = true,
                FileName = fileName,
                Folder = relativeName.Substring(0, relativeName.Length - fileName.Length).Trim(PathSeparator)
            };
        }

    }
}
