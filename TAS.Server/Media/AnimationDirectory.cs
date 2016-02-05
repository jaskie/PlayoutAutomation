using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TAS.Common;
using TAS.Data;
using TAS.Server.Interfaces;
using System.IO;

namespace TAS.Server
{
    public class AnimationDirectory: MediaDirectory, IAnimationDirectory
    {
        public readonly IPlayoutServer Server;
        public AnimationDirectory(IPlayoutServer server, MediaManager manager): base(manager)
        {
            Server = server;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Initialize()
        {
            _isInitialized = false; // to avoid subsequent reinitializations
            DirectoryName = "Animacje";
            this.Load();
            Debug.WriteLine(Server.MediaFolder, "AnimationDirectory initialized");
        }

        protected override void Reinitialize()
        {

        }

        public override void Refresh()
        {
            
        }

        protected override IMedia CreateMedia(string fullPath, Guid guid)
        {
            throw new NotImplementedException();
        }

        public override void MediaRemove(IMedia media)
        {
            if (media is ServerMedia)
            {
                ((ServerMedia)media).MediaStatus = TMediaStatus.Deleted;
                ((ServerMedia)media).Verified = false;
                ((ServerMedia)media).Save();
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
