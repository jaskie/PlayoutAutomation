using System;
using Newtonsoft.Json;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ServerMedia: PersistentMedia, IServerMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ServerMedia));
        private bool _isPRI;
        private bool _doNotArchive;
        Lazy<bool> _isArchived;

        public ServerMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia, IArchiveDirectory archiveDirectory) : base(directory, guid, idPersistentMedia)
        {
            IdPersistentMedia = idPersistentMedia;
            _isArchived = new Lazy<bool>(() => archiveDirectory?.DbArchiveContainsMedia(this) ?? false);
        }

        // media properties
        public bool IsPRI { get { return _isPRI; } set { if (value) _isPRI = true; } } //one-way to true only

        [JsonProperty]
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value); }
        }

        public bool IsArchived
        {
            get { return _isArchived.Value; }
            set
            {
                if (_isArchived.IsValueCreated && _isArchived.Value != value)
                    SetField(ref _isArchived, new Lazy<bool>(() => value));
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
                            result = this.DbDeleteMedia();
                    }
                    else
                    {
                        if (directory != null)
                        {
                            if (IdPersistentMedia == 0)
                                result = this.DbInsertMedia(directory.Server.Id);
                            else if (IsModified)
                            {
                                this.DbUpdateMedia(directory.Server.Id);
                                result = true;
                            }
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
