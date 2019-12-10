using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class ServerMedia: PersistentMedia, Common.Database.Interfaces.Media.IServerMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _doNotArchive;

        [JsonProperty]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Current.Database.ServerMediaFieldLengths;

        [JsonProperty]
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

        public override bool Save()
        {
            var result = false;
            try
            {
                var directory = Directory as ServerDirectory;
                if (MediaStatus != TMediaStatus.Unknown)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            result = EngineController.Current.Database.DeleteMedia(this);
                    }
                    else
                    {
                        if (directory != null)
                        {
                            if (IdPersistentMedia == 0)
                                result = EngineController.Current.Database.InsertMedia(this, directory.Server.Id);
                            else if (IsModified)
                            {
                                EngineController.Current.Database.UpdateMedia(this, directory.Server.Id);
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
