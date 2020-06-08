using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Server.CgElementsController.Configurator
{    
    [Export(typeof(IPluginConfigurator))]
    public class CgElementsControllerPluginManager : ModifyableViewModelBase, IPluginConfigurator
    {                
        private readonly IConfigEngine _engine;
        private Configurator.Model.CgElementsController _cgElementsController;

        private List<Model.CgElement> _cgElements;
        private List<Model.CgElement> _crawls;
        private List<Model.CgElement> _logos;
        private List<Model.CgElement> _auxes;
        private List<Model.CgElement> _parentals;
        private Model.CgElement _selectedElement;
        private Model.CgElement.Type _selectedElementType;
        private OkCancelViewModelBase _currentViewModel;
        private List<string> _elementTypes;
        private List<string> _startup;
        private bool _isEnabled;        
        private Configurator.Model.CgElement _newElement;

        [ImportingConstructor]
        public CgElementsControllerPluginManager([Import("Engine")]IConfigEngine engine)
        {
            _engine = engine;                         
            LoadCommands();
            Init();
        }

        private void LoadCommands()
        {
            AddCgElementCommand = new UiCommand(AddCgElement, CanAddCgElement);
            MoveCgElementUpCommand = new UiCommand(MoveCgElementUp, CanMoveCgElementUp);
            MoveCgElementDownCommand = new UiCommand(MoveCgElementDown, CanMoveCgElementDown);
            EditElementCommand = new UiCommand(EditElement);
            DeleteElementCommand = new UiCommand(DeleteElement);
            SaveCommand = new UiCommand(LocalSave, CanSave);
            UndoCommand = new UiCommand(Undo, CanUndo);
        }

        private void Init()
        {
            _crawls = new List<Model.CgElement>();
            _logos = new List<Model.CgElement>();
            _auxes = new List<Model.CgElement>();
            _parentals = new List<Model.CgElement>();
            _startup = new List<string>();

            if (_engine.CGElementsController != null)
            {
                var cgElementsController = _engine.CGElementsController as Model.CgElementsController;
                foreach (var crawl in cgElementsController.Crawls)
                {
                    var cgElement = crawl as Model.CgElement;
                    cgElement.CgType = Model.CgElement.Type.Crawl;
                    _crawls.Add(cgElement);
                }
                foreach (var logo in cgElementsController.Logos)
                {
                    var cgElement = logo as Model.CgElement;
                    cgElement.CgType = Model.CgElement.Type.Logo;
                    _logos.Add(cgElement);
                }
                foreach (var aux in cgElementsController.Auxes)
                {
                    var cgElement = aux as Model.CgElement;
                    cgElement.CgType = Model.CgElement.Type.Aux;
                    _auxes.Add(cgElement);
                }
                foreach (var parental in cgElementsController.Parentals)
                {
                    var cgElement = parental as Model.CgElement;
                    cgElement.CgType = Model.CgElement.Type.Parental;
                    _parentals.Add(cgElement);
                }                
                _startup = ((Model.CgElementsController)_engine.CGElementsController).Startup;                
                _isEnabled = cgElementsController.IsEnabled;                
            }
            else
            {               
                _isEnabled = false;
            }

            _elementTypes = Enum.GetNames(typeof(Configurator.Model.CgElement.Type)).ToList();            

            ElementTypes = CollectionViewSource.GetDefaultView(_elementTypes);
            SelectedElementType = _elementTypes.LastOrDefault();
            IsModified = false;
        }

        private bool CanUndo(object obj)
        {
            return IsModified && IsEnabled;
        }

        private void Undo(object obj)
        {
            _newElement = null;
            CgElementViewModel = null;
            Init();
        }

        private bool CanSave(object obj)
        {
            return IsModified && IsEnabled;
        }

        private void DeleteElement(object obj)
        {
            if (!(obj is Configurator.Model.CgElement element))
                return;

            _cgElements.Remove(element);
            CgElements.Refresh();
        }

        private void EditElement(object obj)
        {
            if (!(obj is Configurator.Model.CgElement element))
                return;

            CgElementViewModel = new CgElementViewModel(element, "Edit");
        }

        private bool CanMoveCgElementDown(object obj)
        {
            if (_selectedElement != null && _selectedElement.Id < (_cgElements.Count() - 1))
                return true;

            return false;
        }

        private void MoveCgElementDown(object obj)
        {
            var swapElement = _cgElements.FirstOrDefault(c => c.Id == _selectedElement.Id + 1);

            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id += 1;

            CgElements.Refresh();
        }

        private bool CanMoveCgElementUp(object obj)
        {
            if (_selectedElement != null && _selectedElement.Id > 0)
                return true;

            return false;
        }

        private void MoveCgElementUp(object obj)
        {
            var swapElement = _cgElements.FirstOrDefault(c => c.Id == _selectedElement.Id - 1);

            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id -= 1;
            CgElements.Refresh();
        }

        private bool CanAddCgElement(object obj)
        {
            if (_newElement == null)
                return true;

            return false;
        }

        private void AddCgElement(object obj)
        {
            _newElement = new Model.CgElement();
            _newElement.CgType = _selectedElementType;

            CgElementViewModel = new CgElementViewModel(_newElement);
        }

        public void Save()
        {
            if (_cgElementsController == null)
                return;

            _engine.CGElementsController = _cgElementsController;

            var cgElements = _auxes.Concat(_crawls).Concat(_logos).Concat(_parentals);
            foreach (var cgElement in cgElements)
            {
                if (cgElement.UploadServerImagePath != null && cgElement.UploadServerImagePath.Length > 0)
                {
                    foreach (var path in _engine.Servers.Select(server => server.MediaFolder))
                    {
                        File.Copy(cgElement.UploadServerImagePath, Path.Combine(path, Path.GetFileName(cgElement.UploadServerImagePath)), true);
                    }
                }

                if (cgElement.UploadClientImagePath != null && cgElement.UploadClientImagePath.Length > 0)
                {
                    string configPath = FileUtils.ConfigurationPath;
                    switch (cgElement.CgType)
                    {
                        case Model.CgElement.Type.Parental:
                            configPath = Path.Combine(configPath, "Parentals");
                            break;

                        case Model.CgElement.Type.Aux:
                            configPath = Path.Combine(configPath, "Auxes");
                            break;

                        case Model.CgElement.Type.Crawl:
                            configPath = Path.Combine(configPath, "Crawls");
                            break;

                        case Model.CgElement.Type.Logo:
                            configPath = Path.Combine(configPath, "Logos");
                            break;
                    }

                    if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), configPath)))
                        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), configPath));

                    var clientPath = Path.Combine(Directory.GetCurrentDirectory(), configPath, Path.GetFileName(cgElement.UploadClientImagePath));
                    File.Copy(cgElement.UploadClientImagePath, clientPath, true);
                }
            }            
        }

        private void LocalSave(object obj)
        {
            if (_cgElementsController == null)
                _cgElementsController = new Model.CgElementsController();

            _cgElementsController.Auxes = _auxes;
            _cgElementsController.Crawls = _crawls;
            _cgElementsController.Logos = _logos;
            _cgElementsController.Parentals = _parentals;
            _cgElementsController.IsEnabled = _isEnabled;
            _cgElementsController.Startup = _startup;
            IsModified = false;
        }

        private void CgElementWizardClosed(object sender, EventArgs e)
        {
            if (!(sender is OkCancelViewModelBase okCancelVm))
                return;

            if (okCancelVm.DialogResult)
            {
                if (_newElement != null)
                {
                    _newElement.Id = (byte)_cgElements.Count();
                    _cgElements.Add(_newElement);
                    _newElement = null;
                    IsModified = true;
                }
            }

            CgElementViewModel = null;
            CgElements.Refresh();
        }

        public OkCancelViewModelBase CgElementViewModel
        {
            get => _currentViewModel;
            set
            {
                var old = _currentViewModel;

                if (_currentViewModel == value)
                    return;
                _currentViewModel = value;

                if (old != null)
                    old.Closing -= CgElementWizardClosed;

                if (value != null)
                    _currentViewModel.Closing += CgElementWizardClosed;

                NotifyPropertyChanged();
            }
        }

        public string SelectedElementType
        {
            get => Enum.GetName(typeof(Model.CgElement.Type), _selectedElementType);
            set
            {
                var temp = (Model.CgElement.Type)Enum.Parse(typeof(Model.CgElement.Type), value);
                if (temp == _selectedElementType)
                    return;

                _newElement = null;
                CgElementViewModel = null;

                _selectedElementType = temp;
                switch (_selectedElementType)
                {
                    case Model.CgElement.Type.Crawl:
                        _cgElements = _crawls;
                        break;
                    case Model.CgElement.Type.Logo:
                        _cgElements = _logos;
                        break;
                    case Model.CgElement.Type.Aux:
                        _cgElements = _auxes;
                        break;
                    case Model.CgElement.Type.Parental:
                        _cgElements = _parentals;
                        break;
                }

                CgElements = CollectionViewSource.GetDefaultView(_cgElements);
                CgElements.SortDescriptions.Add(new SortDescription(nameof(Model.CgElement.Id), ListSortDirection.Ascending));

                NotifyPropertyChanged(nameof(CgElements));
                NotifyPropertyChanged();
            }
        }

        public Model.CgElement SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (value == _selectedElement)
                    return;

                _selectedElement = value;
                _newElement = null;

                NotifyPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        public UiCommand AddCgElementCommand { get; private set; }
        public UiCommand MoveCgElementUpCommand { get; private set; }
        public UiCommand MoveCgElementDownCommand { get; private set; }
        public UiCommand EditElementCommand { get; private set; }
        public UiCommand DeleteElementCommand { get; private set; }
        public UiCommand SaveCommand { get; private set; }
        public UiCommand UndoCommand { get; private set; }

        public ICollectionView CgElements { get; private set; }
        public ICollectionView ElementTypes { get; private set; }

        public string PluginName => "CgElementsController";

        public List<string> Startup { get => _startup; set => SetField(ref _startup, value); }
        
        protected override void OnDispose()
        {
            //
        }

        public void Initialize()
        {
            UiServices.AddDataTemplate(typeof(CgElementsControllerPluginManager), typeof(CgElementsControllerPluginManagerView));
        }        
    }
}
