using jNet.RPC;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class ServerMedia: PersistentMedia, Database.Common.Interfaces.Media.IServerMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _doNotArchive;
        private DateTime _lastPlayed;

        [DtoMember]
        public override IDictionary<string, int> FieldLengths { get; } = DatabaseProvider.Database.ServerMediaFieldLengths;

        [DtoMember]
        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => SetField(ref _doNotArchive, value);
        }

        [DtoMember]
        public DateTime LastPlayed
        {
            get => _lastPlayed;
            set => SetField(ref _lastPlayed, value);
        }

        internal override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is IServerMediaProperties serverMediaProperties)
            {
                DoNotArchive = serverMediaProperties.DoNotArchive;
                LastPlayed = serverMediaProperties.LastPlayed;
            }
        }

        public override void Save()
        {
            try
            {
                if (!(MediaStatus != TMediaStatus.Unknown && MediaStatus != TMediaStatus.Deleted && Directory is ServerDirectory directory))
                    return;
                if (IdPersistentMedia == 0)
                {
                    DatabaseProvider.Database.InsertMedia(this, directory.Server.Id);
                    IsModified = false;
                    directory.OnMediaSaved(this);
                }
                else
                {
                    DatabaseProvider.Database.UpdateMedia(this, directory.Server.Id);
                    IsModified = false;
                    directory.OnMediaSaved(this);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
        }

        }
}
