using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using TAS.Common;
using System.Diagnostics;

namespace TAS.Server
{
    
    public class ServerMedia: PersistentMedia
    {

        // media properties
        private bool _isPGM;
        public bool IsPGM { get { return _isPGM; } set { if (value) _isPGM = true; } } //one way to true only possible
        internal bool _doNotArchive;
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value, "DoNotArchive"); }
        }

        public override void CloneMediaProperties(Media fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is ServerMedia)
            {
                HasExtraLines = (fromMedia as ServerMedia).HasExtraLines;
                DoNotArchive = (fromMedia as ServerMedia).DoNotArchive;
            }
        }

        public override bool Save()
        {

            bool result = false;
            if (MediaStatus != TMediaStatus.Unknown)
            {
                if (MediaStatus == TMediaStatus.Deleted && !DatabaseConnector.ServerMediaInUse(this))
                {
                    if (idPersistentMedia != 0)
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
