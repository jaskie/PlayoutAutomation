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
    public class ServerMedia: PersistentMedia, IServerMedia
    {
        private NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ServerMedia));

        readonly IArchiveDirectory _archiveDirectory;
        public ServerMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia, IArchiveDirectory archiveDirectory) : base(directory, guid, idPersistentMedia)
        {
            IdPersistentMedia = idPersistentMedia;
            _archiveDirectory = archiveDirectory;
            _isArchived = new Lazy<bool>(() => _archiveDirectory == null ? false :_archiveDirectory.DbArchiveContainsMedia(this));
        }

        // media properties
        private bool _isPRI;
        public bool IsPRI { get { return _isPRI; } set { if (value) _isPRI = true; } } //one way to true only possible
        internal bool _doNotArchive;
        [JsonProperty]
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value, nameof(DoNotArchive)); }
        }

        Lazy<bool> _isArchived;
        public bool IsArchived
        {
            get { return _isArchived.Value; }
            set
            {
                if (_isArchived.IsValueCreated && _isArchived.Value != value)
                    SetField(ref _isArchived, new Lazy<bool>(() => value), nameof(IsArchived));
            }
        }

        public override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is IServerMediaProperties)
            {
                DoNotArchive = (fromMedia as IServerMediaProperties).DoNotArchive;
            }
        }

        public override bool Save()
        {
            bool result = false;
            try
            {
                var directory = Directory as ServerDirectory;
                if (MediaStatus != TMediaStatus.Unknown)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            result = this.DbDelete();
                    }
                    else
                    {
                        if (directory != null)
                        {
                            if (IdPersistentMedia == 0)
                                result = this.DbInsert(directory.Server.Id);
                            else
                            if (IsModified)
                                result = this.DbUpdate(directory.Server.Id);
                        }
                        IsModified = false;
                    }
                }
                if (result && directory != null)
                    directory.OnMediaSaved(this);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            return result;
        }

        }
}
