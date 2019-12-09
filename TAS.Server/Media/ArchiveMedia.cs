using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
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

        [JsonProperty]
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

        public override bool Save()
        {
            bool result = false;
            try
            {
                if (MediaStatus != TMediaStatus.Unknown)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            result = EngineController.Current.Database.DeleteMedia(this);
                    }
                    else
                    {
                        if (IdPersistentMedia == 0)
                            result = EngineController.Current.Database.InsertMedia(this, ((ArchiveDirectory)Directory).IdArchive);
                        else if (IsModified)
                        {
                            EngineController.Current.Database.UpdateMedia(this, ((ArchiveDirectory) Directory).IdArchive);
                            result = true;
                        }
                        IsModified = false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            return result;
        }
    }
}
