using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TAS.Common;
using TAS.Server.Database;
using TAS.Server.Interfaces;
using System.IO;
using Newtonsoft.Json;
using TAS.Server.Common;
using System.ComponentModel;

namespace TAS.Server
{
    public class AnimationDirectory : MediaDirectory, IAnimationDirectory
    {
        public readonly CasparServer Server;
        public AnimationDirectory(CasparServer server, MediaManager manager) : base(manager)
        {
            Server = server;
        }

        public override void Initialize()
        {
            if (!_isInitialized)
            {
                DirectoryName = "Animacje";
                this.Load<AnimatedMedia>(Server.Id);
                base.Initialize();
                Debug.WriteLine(Server.AnimationFolder, "AnimationDirectory initialized");
            }
        }

        public override void Refresh()
        {

        }

        protected override bool AcceptFile(string fullPath)
        {
            return FileUtils.AnimationFileTypes.Contains(Path.GetExtension(fullPath).ToLowerInvariant());
        }

        protected override IMedia AddFile(string fullPath, DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            AnimatedMedia newMedia = _files.Values.FirstOrDefault(m => fullPath.Equals(m.FullPath, StringComparison.CurrentCultureIgnoreCase)) as AnimatedMedia;
            if (newMedia == null && AcceptFile(fullPath))
            {
                newMedia = (AnimatedMedia)CreateMedia(fullPath, guid);
                newMedia.MediaName = FileUtils.GetFileNameWithoutExtension(fullPath, TMediaType.Animation).ToUpper();
                newMedia.LastUpdated = lastWriteTime == default(DateTime) ? File.GetLastWriteTimeUtc(fullPath) : lastWriteTime;
                newMedia.MediaStatus = TMediaStatus.Available;
                newMedia.Save();
            }
            return newMedia;
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }

        protected override IMedia CreateMedia(string fullPath, Guid guid)
        {
            return new AnimatedMedia(this, guid, 0) { FullPath = fullPath, IsVerified = true };
        }

        public IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            var result = new AnimatedMedia(this, newMediaGuid, 0);
            result.Folder = source.Folder;
            result.FileName = source.FileName;
            result.CloneMediaProperties(source);
            result.MediaStatus = source.MediaStatus;
            result.LastUpdated = DateTime.UtcNow;
            result.Save();
            return result;
        }

        public override bool DeleteMedia(IMedia media)
        {
            if (base.DeleteMedia(media))
            {
                MediaRemove(media);
                return true;
            }
            return false;
        }

        public override void SweepStaleMedia() { }

    }
}
