using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Media
{
    public class AnimatedMedia : PersistentMedia, IAnimatedMedia
    {
        private TemplateMethod _method;
        private int _templateLayer;
        private readonly SimpleDictionary<string, string> _fields = new SimpleDictionary<string, string>();


        public AnimatedMedia(IMediaDirectory directory, Guid guid, ulong idPersistentMedia) : base(directory, guid, idPersistentMedia)
        {
            _fields.DictionaryOperation += _fields_DictionaryOperation;
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
                    _fields.Add(kvp);
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
                        result = this.DbDelete();
                }
                else
                if (IdPersistentMedia == 0)
                    result = this.DbInsert(directory.Server.Id);
                else
                if (IsModified)
                    result = this.DbUpdate(directory.Server.Id);
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

        protected override void DoDispose()
        {
            _fields.DictionaryOperation -= _fields_DictionaryOperation;
            base.DoDispose();
        }

        private void _fields_DictionaryOperation(object sender, DictionaryOperationEventArgs<string, string> e)
        {
            IsModified = true;
        }

    }
}
