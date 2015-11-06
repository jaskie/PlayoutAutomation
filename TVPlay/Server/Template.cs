using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TAS.Data;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class Template : ITemplate
    {

        public readonly Engine Engine;
        private readonly Dictionary<string, string> _templateFields = new Dictionary<string, string>();

        public Template(Engine engine)
        {
            Engine = engine;
            _mediaGuid = Guid.Empty;
            engine.MediaManager.getTemplates().Add(this);
        }

        internal UInt64 idTemplate;

        private string _templateName;
        public string TemplateName
        {
            get { return _templateName; }
            set { SetField(ref _templateName, value, "TemplateName"); }
        }

        private int _layer;
        public int Layer { get { return _layer; }
            set { SetField(ref _layer, value, "Layer"); }
        }

        private Guid _mediaGuid;
        public Guid MediaGuid
        {
            get { return _mediaGuid; }
            internal set { SetField(ref _mediaGuid, value, "MediaGuid"); }
        }

        private IMedia _mediaFile;
        public IMedia MediaFile
        {
            get
            {
                var mg = _mediaGuid;
                if (_mediaFile == null && mg != Guid.Empty)
                    _mediaFile = Engine.PlayoutChannelPGM.OwnerServer.AnimationDirectory.FindMedia(_mediaGuid); // lazy loading Media
                return _mediaFile;
            }
            set
            {
                if (SetField(ref _mediaFile, value, "MediaFile"))
                    MediaGuid = value.MediaGuid;
            }
        }

        public Dictionary<string, string> TemplateFields
        {
            get { return _templateFields; }
            set
            {
                _templateFields.Clear();
                if (value != null)
                    foreach (var item in value)
                        _templateFields.Add(item.Key, item.Value);
                NotifyPropertyChanged("TemplateFields");
            }
        }

        public void Save()
        {
            this.DbSave();
        }

        public void Delete()
        {
            this.DbDelete();
            Engine.MediaManager.getTemplates().Remove(this);
        }


        public override string ToString()
        {
            return TemplateName;
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            lock (this)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
            }
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
