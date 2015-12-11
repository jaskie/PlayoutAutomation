using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Collections.ObjectModel;
using TAS.Client.Common;
using System.Windows.Input;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class TemplateViewmodel: ViewmodelBase, IDataErrorInfo
    {
        public readonly ITemplate Template;
        private readonly TemplatesViewmodel _owner;
        public TemplateViewmodel(ITemplate template, TemplatesViewmodel owner)
        {
            Template = template;
            _owner = owner;
            template.PropertyChanged += _onTemplatePropertyChanged;
            CommandSaveEdit = new UICommand() { ExecuteDelegate = Save, CanExecuteDelegate = o => Modified && IsValid };
            CommandCancelEdit = new UICommand() { ExecuteDelegate = Load, CanExecuteDelegate = o => Modified };
            Load(null);
            Modified = false;
        }

        protected override void OnDispose()
        {
            Template.PropertyChanged -= _onTemplatePropertyChanged;
        }

        private bool _modified;
        public bool Modified
        {
            get { return _modified; }
            private set
            {
                if (_modified != value)
                {
                    _modified = value;
                    NotifyPropertyChanged("CommandCancelEdit");
                }
                NotifyPropertyChanged("CommandSaveEdit");
            }
        }

        private string _templateName;
        public string TemplateName
        {
            get { return _templateName; }
            set { SetField(ref _templateName, value, "TemplateName"); }
        }

        private MediaViewViewmodel _mediaFile;
        public MediaViewViewmodel MediaFile
        {
            get { return _mediaFile; }
            set { SetField(ref _mediaFile, value, "MediaFile"); }
        }

        private int _layer;
        public int Layer
        {
            get { return _layer; }
            set { SetField(ref _layer, value, "Layer"); }
        }

        private readonly ObservableCollection<KeyValuePair<string, string>> _templateFields = new ObservableCollection<KeyValuePair<string,string>>();
        public ObservableCollection<KeyValuePair<string, string>> TemplateFields
        {
            get { return _templateFields; }
        }

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (base.SetField(ref field, value, propertyName))
            {
                Modified = true;
                return true;
            }
            return false;
        }


        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                //switch (propertyName)
                //{
                //}
                return validationResult;
            }
        }

        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }

        internal void Load(object o)
        {
            TemplateName = Template.TemplateName;
            Layer = Template.Layer;
            MediaFile = _owner.MediaFiles.FirstOrDefault(m => m.Media == Template.MediaFile);
            foreach (var f in Template.TemplateFields)
                _templateFields.Add(f);
            Modified = false;
            NotifyPropertyChanged(null);
        }

        internal void Save(object o)
        {
            Template.TemplateName = this.TemplateName;
            Template.Layer = this.Layer;
            Template.MediaFile = this.MediaFile == null ? null : MediaFile.Media;
            Template.Save();
            Modified = false;
        }

        public IEnumerable<MediaViewViewmodel> MediaFiles
        {
            get { return _owner.MediaFiles; }
        }

        public ICommand CommandSaveEdit { get; private set; }
        public ICommand CommandCancelEdit { get; private set; }
        public ICommand CommandSelectMedia { get; private set; }

        public string Error
        {
            get { return String.Empty; }
        }

        private void _onTemplatePropertyChanged(object o, PropertyChangedEventArgs e)
        {
            if (o == Template)
                if (string.IsNullOrEmpty(e.PropertyName))
                    Load(null);
                else
                {
                    switch (e.PropertyName)
                    {
                        case "TemplateName":
                            TemplateName = Template.TemplateName;
                            break;
                        case "Layer":
                            Layer = Template.Layer;
                            break;
                        case "MediaFile":
                            MediaFile = _owner.MediaFiles.FirstOrDefault(m => m.Media == Template.MediaFile);
                            break;
                    }
                }
        }

        internal void Delete()
        {
            Template.Delete();
            NotifyPropertyChanged(null);
        }
    }
}
