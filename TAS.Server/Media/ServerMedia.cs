using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ServerMedia: PersistentMedia, IServerMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ServerMedia));
        private bool _doNotArchive;
        Lazy<bool> _isArchived;

        public ServerMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia, IArchiveDirectory archiveDirectory) : base(directory, guid, idPersistentMedia)
        {
            IdPersistentMedia = idPersistentMedia;
            _isArchived = new Lazy<bool>(() => archiveDirectory != null && EngineController.Database.DbArchiveContainsMedia(archiveDirectory, this));
        }

        [JsonProperty]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Database.ServerMediaFieldLengths;

        [JsonProperty]
        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => SetField(ref _doNotArchive, value);
        }

        public bool IsArchived
        {
            get => _isArchived.Value;
            set
            {
                if (_isArchived.IsValueCreated && _isArchived.Value != value)
                    SetField(ref _isArchived, new Lazy<bool>(() => value));
            }
        }

        public override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is IServerMediaProperties serverMediaProperties)
                DoNotArchive = serverMediaProperties.DoNotArchive;
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
                            result = EngineController.Database.DbDeleteMedia(this);
                    }
                    else
                    {
                        if (directory != null)
                        {
                            if (IdPersistentMedia == 0)
                                result = EngineController.Database.DbInsertMedia(this, directory.Server.Id);
                            else if (IsModified)
                            {
                                EngineController.Database.DbUpdateMedia(this, directory.Server.Id);
                                result = true;
                            }
                        }
                        IsModified = false;
                    }
                }
                if (result)
                    directory?.OnMediaSaved(this);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            return result;
        }

        }
}
