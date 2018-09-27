using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class AnimatedMedia : PersistentMedia, IAnimatedMedia
    {
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
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Database.ServerMediaFieldLengths;

        public override bool Save()
        {
            var result = false;
            if (Directory is AnimationDirectory directory)
            {
                if (MediaStatus == TMediaStatus.Deleted)
                {
                    if (IdPersistentMedia != 0)
                        result = EngineController.Database.DbDeleteMedia(this);
                }
                else
                if (IdPersistentMedia == 0)
                    result = EngineController.Database.DbInsertMedia(this, directory.Server.Id);
                else if (IsModified)
                {
                    EngineController.Database.DbUpdateMedia(this, directory.Server.Id);
                    result = true;
                }
            }
            IsModified = false;
            return result;
        }

        public override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is AnimatedMedia a)
                Fields = a.Fields;
        }

        public override void Verify()
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
