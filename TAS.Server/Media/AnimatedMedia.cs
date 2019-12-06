using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class AnimatedMedia : PersistentMedia, Common.Database.Interfaces.Media.IAnimatedMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TemplateMethod _method;
        private int _templateLayer;
        private Dictionary<string, string> _fields;
        private TimeSpan _scheduledDelay;
        private TStartType _startType = TStartType.WithParent;

        [JsonProperty]
        public Dictionary<string, string> Fields
        {
            get => _fields;
            set => SetField(ref _fields, value);
        }

        [JsonProperty]
        public TemplateMethod Method { get => _method; set => SetField(ref _method, value); }

        [JsonProperty]
        public int TemplateLayer { get => _templateLayer; set => SetField(ref _templateLayer, value); }

        [JsonProperty]
        public TimeSpan ScheduledDelay { get => _scheduledDelay; set => SetField(ref _scheduledDelay, value); }

        [JsonProperty]
        public TStartType StartType { get => _startType; set => SetField(ref _startType, value); }

        [JsonProperty]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Current.Database.ServerMediaFieldLengths;

        public override bool Save()
        {
            var result = false;
            try
            {
                if (Directory is AnimationDirectory directory)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            result = EngineController.Current.Database.DeleteMedia(this);
                    }
                    else if (IdPersistentMedia == 0)
                        result = EngineController.Current.Database.InsertMedia(this, directory.Server.Id);
                    else if (IsModified)
                    {
                        EngineController.Current.Database.UpdateMedia(this, directory.Server.Id);
                        result = true;
                    }
                    if (result)
                        directory.OnMediaSaved(this);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            IsModified = false;
            return result;
        }

        internal override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is AnimatedMedia a)
                Fields = a.Fields;
        }

        public override void Verify(bool updateFormatAndDurations)
        {
            if (!FileExists())
            {
                MediaStatus = TMediaStatus.Deleted;
                return;
            }
            IsVerified = true;
            MediaStatus = TMediaStatus.Available;
        }

    }
}
