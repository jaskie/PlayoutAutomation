using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class AnimatedMedia : PersistentMedia, IAnimatedMedia
    {
        private TemplateMethod _method;
        private int _templateLayer;
        private readonly ConcurrentDictionary<string, string> _fields = new ConcurrentDictionary<string, string>();


        public AnimatedMedia(IMediaDirectory directory, Guid guid, ulong idPersistentMedia) : base(directory, guid, idPersistentMedia)
        {
            base.MediaType = TMediaType.Animation;
        }

        [JsonProperty]
        public IDictionary<string, string> Fields
        {
            get { return _fields; }
            set
            {
                _fields.Clear();
                foreach (var kvp in value)
                    _fields.TryAdd(kvp.Key, kvp.Value);
                IsModified = true;
            }
        }

        [JsonProperty]
        public TemplateMethod Method { get { return _method; } set { SetField(ref _method, value); } }

        [JsonProperty]
        public int TemplateLayer { get { return _templateLayer; } set { SetField(ref _templateLayer, value); } }

        public override bool Save()
        {
            bool result = false;
            var directory = Directory as AnimationDirectory;
            if (directory != null)
            {
                if (MediaStatus == TMediaStatus.Deleted)
                {
                    if (IdPersistentMedia != 0)
                        result = this.DbDeleteMedia();
                }
                else
                if (IdPersistentMedia == 0)
                    result = this.DbInsertMedia(directory.Server.Id);
                else if (IsModified)
                {
                    this.DbUpdateMedia(directory.Server.Id);
                    result = true;
                }
            }
            IsModified = false;
            return result;
        }

        public override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            var a = (fromMedia as AnimatedMedia);
            if (a != null)
                Fields = a.Fields;
        }

        public override void Verify()
        {
            if (FileExists())
            {
                IsVerified = true;
                MediaStatus = TMediaStatus.Available;
            }
        }

    }
}
