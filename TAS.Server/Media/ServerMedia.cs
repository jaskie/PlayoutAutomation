using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using TAS.Common;
using TAS.Server.Database;
using TAS.Server.Interfaces;
using Newtonsoft.Json;

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerMedia: PersistentMedia, IServerMedia
    {

        public ServerMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid, idPersistentMedia) { IdPersistentMedia = idPersistentMedia; }

        // media properties
        private bool _isPRI;
        public bool IsPRI { get { return _isPRI; } set { if (value) _isPRI = true; } } //one way to true only possible
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
                    if (IdPersistentMedia != 0)
                        result = this.DbDelete();
                }
                else
                {
                    if (Modified)
                    {
                        if (IdPersistentMedia == 0)
                            result = this.DbInsert();
                        else
                            result = this.DbUpdate();
                        Modified = false;
                    }
                }
            }
            if (result && _directory is ServerDirectory)
                (_directory as ServerDirectory).OnMediaSaved(this);
            return result;
        }

    }
}
