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

namespace TAS.Server
{
    public class AnimationDirectory: MediaDirectory, IAnimationDirectory
    {
        private readonly IPlayoutServer _server;
        public AnimationDirectory(IPlayoutServer server, MediaManager manager): base(manager)
        {
            _server = server;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Initialize()
        {
            if (!_isInitialized)
            {
                DirectoryName = "Animacje";
                this.Load<TemplatedMedia>(_server.Id);
                Debug.WriteLine(_server.MediaFolder, "AnimationDirectory initialized");
                IsInitialized = true;
            }
        }

        public override void Refresh()
        {
            
        }

        protected override IMedia CreateMedia(string fullPath, Guid guid)
        {
            return new TemplatedMedia(this, guid, 0) { FullPath = fullPath };
        }

        public override void MediaRemove(IMedia media)
        {
            var tm = media as TemplatedMedia;
            if (tm != null)
            {
                tm.MediaStatus = TMediaStatus.Deleted;
                tm.Verified = false;
                tm.Save();
            }
            base.MediaRemove(media);
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
