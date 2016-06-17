using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Database;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class AnimatedMedia : PersistentMedia, IAnimatedMedia
    {
        public AnimatedMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid, idPersistentMedia)
        {
            _fields = new SimpleDictionary<string, string>();
            _fields.DictionaryOperation += _fields_DictionaryOperation;
        }

        

        private void _fields_DictionaryOperation(object sender, DictionaryOperationEventArgs<string, string> e)
        {
            Modified = true;
        }

        private readonly SimpleDictionary<string, string> _fields;

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

        private TemplateMethod _method;
        public TemplateMethod Method { get { return _method; } set { SetField(ref _method, value, "Method"); } }

        private int _templateLayer;
        public int TemplateLayer { get { return _templateLayer; } set { SetField(ref _templateLayer, value, "TemplateLayer"); } }

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
                if (Modified)
                    result = this.DbUpdate(directory.Server.Id);
            }
            Modified = false;
            return result;
        }

        public override void CloneMediaProperties(IMedia fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            var a = fromMedia as AnimatedMedia;
            if (a != null)
            {
                _fields.Clear();
                foreach (var field in a.Fields)
                    _fields.Add(field);
            }
        }

        internal override void Verify()
        {
            if (FileExists())
            {
                Verified = true;
                MediaStatus = TMediaStatus.Available;
            }
        }
    }
}
