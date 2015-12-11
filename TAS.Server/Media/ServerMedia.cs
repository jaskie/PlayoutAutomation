using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using TAS.Common;
using TAS.Data;
using TAS.Server.Interfaces;
using Newtonsoft.Json;

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerMedia: PersistentMedia, IServerMedia
    {

        public ServerMedia(ServerDirectory directory) : base(directory) { }
        public ServerMedia(ServerDirectory directory, Guid guid) : base(directory, guid) { }
        public ServerMedia(AnimationDirectory directory) : base(directory) { }
        public ServerMedia(AnimationDirectory directory, Guid guid) : base(directory, guid) { }

        // media properties
        private bool _isPGM;
        public bool IsPGM { get { return _isPGM; } set { if (value) _isPGM = true; } } //one way to true only possible
        internal bool _doNotArchive;
        [JsonProperty]
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value, "DoNotArchive"); }
        }

        public override void CloneMediaProperties(IMedia fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is ServerMedia)
            {
                DoNotArchive = (fromMedia as ServerMedia).DoNotArchive;
            }
        }

        public override bool Save()
        {

            bool result = false;
            if (MediaStatus != TMediaStatus.Unknown)
            {
                if (MediaStatus == TMediaStatus.Deleted)
                {
                    if (idPersistentMedia != 0 
                        && this.DbMediaInUse().Reason == MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                        result = this.DbDelete();
                }
                else
                {
                    if (Modified)
                    {
                        if (idPersistentMedia == 0)
                            result = this.DbInsert();
                        else
                            result = this.DbUpdate();
                    }
                }
            }
            if (result && _directory is ServerDirectory)
                (_directory as ServerDirectory).OnMediaSaved(this);
            return result;
        }

        //protected override void setMediaStatus(TMediaStatus newStatus)
        //{
        //    if (newStatus != TMediaStatus.Available)
        //        Verified = false;
        //    base.setMediaStatus(newStatus);
        //}

    }
}
