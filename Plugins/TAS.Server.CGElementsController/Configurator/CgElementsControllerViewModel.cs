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
    public class CgElementsControllerViewModel : ModifyableViewModelBase, IPluginConfigurator
    {                
        private readonly IConfigEngine _engine;
        private Model.CgElementsController _cgElementsController = new Model.CgElementsController();

        private List<Model.CgElement> _cgElements;
        private List<Model.CgElement> _crawls;
        private List<Model.CgElement> _logos;
        private List<Model.CgElement> _auxes;
        private List<Model.CgElement> _parentals;
        private Model.CgElement _selectedElement;
        private Model.CgElement.Type _selectedElementType;
        private Model.CgElement _selectedDefaultCrawl;
        private Model.CgElement _selectedDefaultLogo;
        private int _selectedStartupId;
        private OkCancelViewModelBase _currentViewModel;
        private List<string> _elementTypes;
        private List<Model.CgElement> _startups;
        private bool _isEnabled;        
        private Model.CgElement _newElement;

        public event EventHandler PluginChanged;

        [ImportingConstructor]
        public CgElementsControllerViewModel([Import("Engine")]IConfigEngine engine)
        {
            _engine = engine;
            LoadCommands();

            _elementTypes = Enum.GetNames(typeof(Model.CgElement.Type)).ToList();
            ElementTypes = CollectionViewSource.GetDefaultView(_elementTypes);            
            CgElements = CollectionViewSource.GetDefaultView(_cgElements);           
        }

        private void LoadCommands()
        {
            AddCgElementCommand = new UiCommand(AddCgElement, CanAddCgElement);
            MoveCgElementUpCommand = new UiCommand(MoveCgElementUp, CanMoveCgElementUp);
            MoveCgElementDownCommand = new UiCommand(MoveCgElementDown, CanMoveCgElementDown);
            EditCgElementCommand = new UiCommand(EditElement);
            DeleteCgElementCommand = new UiCommand(DeleteElement);
            AddStartupCommand = new UiCommand(AddStartup);
            MoveStartupUpCommand = new UiCommand(MoveStartupUp, CanMoveStartupUp);
            MoveStartupDownCommand = new UiCommand(MoveStartupDown, CanMoveStartupDown);            
            DeleteStartupCommand = new UiCommand(DeleteStartup);
            SaveCommand = new UiCommand(LocalSave, CanSave);
            UndoCommand = new UiCommand(Undo, CanUndo);
        }

        private void DeleteStartup(object obj)
        {
            if (!(obj is Model.CgElement startup))
                return;

            _startups.Remove(startup);
            IsModified = true;
            Startups.Refresh();
        }

        private bool CanMoveStartupDown(object obj)
        {
            if (_selectedStartupId > -1 &&  _selectedStartupId < (_startups.Count() - 1))
                return true;

            return false;
        }

        private void MoveStartupDown(object obj)
        {
            ((IEditableCollectionView)Startups).CommitEdit();
            var swapElement = _startups[_selectedStartupId];
            _startups[_selectedStartupId] = _startups[_selectedStartupId + 1];
            _startups[_selectedStartupId + 1] = swapElement;

            Startups.Refresh();
            IsModified = true;
        }

        private bool CanMoveStartupUp(object obj)
        {
            if (_selectedStartupId > 0)
                return true;

            return false;
        }

        private void MoveStartupUp(object obj)
        {
            ((IEditableCollectionView)Startups).CommitEdit();
            var swapElement = _startups[_selectedStartupId];
            _startups[_selectedStartupId] = _startups[_selectedStartupId - 1];
            _startups[_selectedStartupId - 1] = swapElement;

            Startups.Refresh();
            IsModified = true;
        }

        private void AddStartup(object obj)
        {
            ((IEditableCollectionView)Startups).CommitEdit();
            _startups.Add(new Model.CgElement());
            Startups.Refresh();
            IsModified = true;
        }

        private void Init()
        {
            _cgElements = new List<Model.CgElement>();            
            _crawls = new List<Model.CgElement>();
            _logos = new List<Model.CgElement>();
            _auxes = new List<Model.CgElement>();
            _parentals = new List<Model.CgElement>();
            _startups = new List<Model.CgElement>();

            Logos = CollectionViewSource.GetDefaultView(_logos);
            Crawls = CollectionViewSource.GetDefaultView(_crawls);
            Startups = CollectionViewSource.GetDefaultView(_startups);

            SelectedElementType = _elementTypes.LastOrDefault();

            if (_cgElementsController == null)
            {
                _crawls.Add(new Model.CgElement { Id = 0, Name = "Off", Command = "PLAY CG3 EMPTY MIX 25" });
                _logos.Add(new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG4 EMPTY MIX 25" });
                _parentals.Add(new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG5 EMPTY MIX 25" });
                return;
            }
                

            foreach (var crawl in _cgElementsController.Crawls.ToList())
            {
                var cgElement = crawl as Model.CgElement;
                cgElement.CgType = Model.CgElement.Type.Crawl;
                _crawls.Add(cgElement);
            }
            foreach (var logo in _cgElementsController.Logos.ToList())
            {
                var cgElement = logo as Model.CgElement;
                cgElement.CgType = Model.CgElement.Type.Logo;
                _logos.Add(cgElement);
            }
            foreach (var aux in _cgElementsController.Auxes.ToList())
            {
                var cgElement = aux as Model.CgElement;
                cgElement.CgType = Model.CgElement.Type.Aux;
                _auxes.Add(cgElement);
            }
            foreach (var parental in _cgElementsController.Parentals.ToList())
            {
                var cgElement = parental as Model.CgElement;
                cgElement.CgType = Model.CgElement.Type.Parental;
                _parentals.Add(cgElement);
            }

            foreach (var startup in _cgElementsController.Startups)
                _startups.Add(new Model.CgElement { Command = startup });

            SelectedDefaultCrawl = _crawls.FirstOrDefault(c => c.Id == _cgElementsController.DefaultCrawl);
            SelectedDefaultLogo = _logos.FirstOrDefault(c => c.Id == _cgElementsController.DefaultLogo);
            Startups = CollectionViewSource.GetDefaultView(_startups);
            IsEnabled = _cgElementsController.IsEnabled;            
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
            if (!(obj is Model.CgElement element))
                return;

            _cgElements.Remove(element);

            for (byte i = 0; i < _cgElements.Count; ++i)
                _cgElements[i].Id = i;

            IsModified = true;
            CgElements.Refresh();            
        }

        private void EditElement(object obj)
        {
            if (!(obj is Model.CgElement element))
                return;

            CgElementViewModel = new CgElementViewModel(element, "Edit");
        }

        private bool CanMoveCgElementDown(object obj)
        {
            if (_selectedElement != null && _selectedElement.Id < (_cgElements.Count()-1))
                return true;

            return false;
        }

        private void MoveCgElementDown(object obj)
        {
            var swapElement = _cgElements.FirstOrDefault(c => c.Id == _selectedElement.Id + 1);

            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id += 1;

            CgElements.Refresh();
            IsModified = true;
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
            IsModified = true;
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
            var cgElements = _auxes.Concat(_crawls).Concat(_logos).Concat(_parentals);
            foreach (var cgElement in cgElements)
            {
                if (cgElement.UploadServerImagePath != null && cgElement.UploadServerImagePath.Length > 0)
                {
                    foreach (var path in _engine.Servers.Select(server => server.MediaFolder))
                    {
                        File.Copy(cgElement.UploadServerImagePath, Path.Combine(path, Path.GetFileName(cgElement.UploadServerImagePath)), true);
                        cgElement.ServerImagePath = Path.Combine(path, Path.GetFileName(cgElement.UploadServerImagePath));
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
                    cgElement.ClientImagePath = clientPath;
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
            _cgElementsController.Startups = _startups.Select(s => s.Command).ToList();
            _cgElementsController.DefaultCrawl = SelectedDefaultCrawl?.Id ?? 1;
            _cgElementsController.DefaultLogo = SelectedDefaultLogo?.Id ?? 1;
            IsModified = false;

            PluginChanged?.Invoke(this, EventArgs.Empty);
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
                    if (_newElement.CgType == Model.CgElement.Type.Crawl)
                        Crawls.Refresh();
                    else if (_newElement.CgType == Model.CgElement.Type.Logo)
                        Logos.Refresh();
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
                _currentViewModel = null;

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
                //CgElements.Refresh();
                CgElements = CollectionViewSource.GetDefaultView(_cgElements);
                CgElements.SortDescriptions.Add(new SortDescription(nameof(Model.CgElement.Id), ListSortDirection.Ascending));
                
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CgElements));
                NotifyPropertyChanged(nameof(CgElementViewModel));
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
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;

                if (_cgElementsController != null)                     
                    _cgElementsController.IsEnabled = value;

                NotifyPropertyChanged();
                PluginChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public UiCommand AddCgElementCommand { get; private set; }
        public UiCommand MoveCgElementUpCommand { get; private set; }
        public UiCommand MoveCgElementDownCommand { get; private set; }
        public UiCommand EditCgElementCommand { get; private set; }
        public UiCommand DeleteCgElementCommand { get; private set; }
        public UiCommand AddStartupCommand { get; private set; }
        public UiCommand MoveStartupUpCommand { get; private set; }
        public UiCommand MoveStartupDownCommand { get; private set; }        
        public UiCommand DeleteStartupCommand { get; private set; }
        public UiCommand SaveCommand { get; private set; }
        public UiCommand UndoCommand { get; private set; }

        public ICollectionView CgElements { get; private set; }
        public ICollectionView ElementTypes { get; private set; }
        public ICollectionView Logos { get; private set; }
        public ICollectionView Crawls { get; private set; }

        public string PluginName => "CgElementsController";

        public ICollectionView Startups { get; private set; }
        public int SelectedStartupId { get => _selectedStartupId; set => SetField(ref _selectedStartupId, value); }
        public Model.CgElement SelectedDefaultCrawl { get => _selectedDefaultCrawl; set => SetField(ref _selectedDefaultCrawl, value); }
        public Model.CgElement SelectedDefaultLogo { get => _selectedDefaultLogo; set => SetField(ref _selectedDefaultLogo, value); }
        

        protected override void OnDispose()
        {
            //
        }

        public void Initialize(object model)
        {           
            _cgElementsController = (ICGElementsController)model as Model.CgElementsController;

            UiServices.AddDataTemplate(typeof(CgElementsControllerViewModel), typeof(CgElementsControllerPluginManagerView));
            Init();
        }

        public object GetModel()
        {
            return _cgElementsController;
        }
    }
}
