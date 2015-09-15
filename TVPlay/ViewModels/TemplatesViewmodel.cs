using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TAS.Server;
using System.Windows;
using System.Windows.Input;
using TAS.Common;

namespace TAS.Client.ViewModels
{
    public class TemplatesViewmodel: ViewmodelBase
    {
        private readonly Engine _engine;
        private readonly ObservableCollection<TemplateViewmodel> _items;

        public TemplatesViewmodel(Engine engine)
        {
            _engine = engine;
            _items = new ObservableCollection<TemplateViewmodel>();
            engine.MediaManager.Templates.CollectionOperation += _onSourceCollectionOperation;
            foreach (Media m in _engine.PlayoutChannelPGM.OwnerServer.AnimationDirectory.Files)
                _mediaFiles.Add(new MediaViewViewmodel(m));
            foreach (Template t in engine.MediaManager.Templates.ToList())
                _items.Add(new TemplateViewmodel(t, this));
            _createCommands();
        }

        protected override void OnDispose()
        {
            _engine.MediaManager.Templates.CollectionOperation -= _onSourceCollectionOperation;
        }

        public ObservableCollection<TemplateViewmodel> Items { get { return _items; } }

        public ICommand CommandAddTemplate { get; private set; }
        public ICommand CommandDeleteSelected { get; private set; }

        private void _onSourceCollectionOperation(object o, CollectionOperationEventArgs<Template> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TCollectionOperation.Insert)
                    _items.Add(new TemplateViewmodel(e.Item, this));
                if (e.Operation == TCollectionOperation.Remove)
                {
                    var itemToRemove = _items.FirstOrDefault(i => i.Template == e.Item);
                    if (itemToRemove != null)
                        _items.Remove(itemToRemove);
                }
            }, null);
        }

        private void _createCommands()
        {
            CommandAddTemplate = new UICommand() { ExecuteDelegate = _addTemplate };
            CommandDeleteSelected = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        var sel = SelectedTemplate;
                        if (sel != null)
                            sel.Delete();
                    },
                CanExecuteDelegate = o => _selectedTemplate != null
            };
        }

        private void _addTemplate(object o)
        {
            var template = new Template(_engine);
            var tvm = new TemplateViewmodel(template, this);
            _items.Add(tvm);
        }

        private TemplateViewmodel _selectedTemplate;
        public TemplateViewmodel SelectedTemplate
        {
            get { return _selectedTemplate; }
            set
            {
                var oldTemplate = _selectedTemplate;
                if (oldTemplate != null && oldTemplate.Modified)
                {
                    if (MessageBox.Show(Properties.Resources._query_SaveChangedData, Properties.Resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        oldTemplate.Save(null);
                    else
                        oldTemplate.Load(null);
                }
                _selectedTemplate = value;
            }
        }

        private readonly ObservableCollection<MediaViewViewmodel> _mediaFiles = new ObservableCollection<MediaViewViewmodel>();
        public ObservableCollection<MediaViewViewmodel> MediaFiles { get { return _mediaFiles; } }

    }
}
