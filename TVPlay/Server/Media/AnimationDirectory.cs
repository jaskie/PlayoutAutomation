using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TAS.Common;

namespace TAS.Server
{
    public class AnimationDirectory: MediaDirectory
    {
        public readonly PlayoutServer Server;
        public AnimationDirectory(PlayoutServer server)
        {
            Server = server;
        }

        public override void Initialize()
        {
            _isInitialized = false; // to avoid subsequent reinitializations
            DirectoryName = "Animacje";
            //DatabaseConnector.ServerLoadMediaDirectory(this, Server);
            Debug.WriteLine(Server.MediaFolder, "AnimationDirectory initialized");
        }

        protected override void Reinitialize()
        {

        }

        public override void Refresh()
        {
            
        }

        protected override Media CreateMedia()
        {
            return new ServerMedia() { MediaType = TMediaType.AnimationFlash, Directory = this, };
        }

        public override void MediaRemove(Media media)
        {
            if (media is ServerMedia)
            {
                ((ServerMedia)media).MediaStatus = TMediaStatus.Deleted;
                ((ServerMedia)media).Verified = false;
                ((ServerMedia)media).Save();
            }
            base.MediaRemove(media);
        }

        public override bool DeleteMedia(Media media)
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
