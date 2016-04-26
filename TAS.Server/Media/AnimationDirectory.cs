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
    [JsonObject(MemberSerialization.OptIn)]
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
                this.Load<ServerMedia>(_server.Id);
                Debug.WriteLine(_server.MediaFolder, "AnimationDirectory initialized");
                IsInitialized = true;
            }
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
