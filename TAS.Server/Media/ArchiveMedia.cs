using jNet.RPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TAS.Common;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public class ArchiveMedia : PersistentMedia, Common.Database.Interfaces.Media.IArchiveMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        ~ArchiveMedia()
        {
            Debug.WriteLine("ArchiveMedia finalized");    
        }

        [DtoMember]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Current.Database.ArchiveMediaFieldLengths;


        public TIngestStatus GetIngestStatus(IServerDirectory directory)
        {
            if (!(Directory is ServerDirectory sdir))
                return TIngestStatus.Unknown;
            var media = sdir.FindMediaByMediaGuid(MediaGuid);
            if (media != null && media.MediaStatus == TMediaStatus.Available)
                return TIngestStatus.Ready;
            return TIngestStatus.Unknown;
        }

        public void NotifyIngestStatus(IServerDirectory directory, TIngestStatus newStatus)
        {

        }

        public override void Save()
        {
            try
            {
                var directory = Directory as ArchiveDirectory ?? throw new ApplicationException("Media directory not set on Save");
                if (MediaStatus != TMediaStatus.Unknown)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            EngineController.Current.Database.DeleteMedia(this);
                    }
                    else
                    {
                        if (IdPersistentMedia == 0)
                            EngineController.Current.Database.InsertMedia(this, directory.IdArchive);
                        else if (IsModified)
                            EngineController.Current.Database.UpdateMedia(this, directory.IdArchive);
                    }

                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            base.Save();
        }
    }
}
