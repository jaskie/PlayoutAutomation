using jNet.RPC;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class ServerMedia: PersistentMedia, Common.Database.Interfaces.Media.IServerMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _doNotArchive;

        [DtoField]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Current.Database.ServerMediaFieldLengths;

        [DtoField]
        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => SetField(ref _doNotArchive, value);
        }

        internal override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is IServerMediaProperties serverMediaProperties)
                DoNotArchive = serverMediaProperties.DoNotArchive;
        }

        public override void Save()
        {
            var saved = false;
            try
            {
                var directory = Directory as ServerDirectory;
                if (MediaStatus != TMediaStatus.Unknown)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            saved = EngineController.Current.Database.DeleteMedia(this);
                    }
                    else
                    {
                        if (directory != null)
                        {
                            if (IdPersistentMedia == 0)
                                saved = EngineController.Current.Database.InsertMedia(this, directory.Server.Id);
                            else if (IsModified)
                            {
                                EngineController.Current.Database.UpdateMedia(this, directory.Server.Id);
                                saved = true;
                            }
                        }
                    }
                }
                if (saved)
                    directory?.OnMediaSaved(this);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            base.Save();
        }

        }
}
