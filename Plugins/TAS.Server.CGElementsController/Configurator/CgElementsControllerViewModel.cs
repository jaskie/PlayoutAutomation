using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common.Interfaces;

namespace TAS.Server.CgElementsController.Configurator
{
    public class CgElementsControllerViewModel : ModifyableViewModelBase, IPluginConfiguratorViewModel
    {
        public enum ElementType
        {
            Crawl,
            Logo,
            Parental,
            Aux
        }

        private readonly IConfigEngine _engine;
        private Model.CgElementsController _cgElementsController = new Model.CgElementsController();

        private readonly ObservableCollection<Model.CgElement> _crawls = new ObservableCollection<Model.CgElement>();
        private readonly ObservableCollection<Model.CgElement> _logos = new ObservableCollection<Model.CgElement>();
        private readonly ObservableCollection<Model.CgElement> _auxes = new ObservableCollection<Model.CgElement>();
        private readonly ObservableCollection<Model.CgElement> _parentals = new ObservableCollection<Model.CgElement>();
        private readonly ObservableCollection<string> _startups = new ObservableCollection<string>();
        private Model.CgElement _selectedElement;
        private ElementType _selectedElementType;
        private Model.CgElement _selectedDefaultCrawl;
        private Model.CgElement _selectedDefaultLogo;
        private int _selectedStartupId;
        private CgElementViewModel _currentViewModel;
        private bool _isEnabled;
        private Model.CgElement _newElement;

        public event EventHandler PluginChanged;

        public CgElementsControllerViewModel(IConfigEngine engine)
        {
            _engine = engine;
            CreateCommands();
        }

        private void CreateCommands()
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
        }

        private void DeleteStartup(object obj)
        {
            if (!(obj is string startup))
                return;

            _startups.Remove(startup);
            IsModified = true;
        }

        private bool CanMoveStartupDown(object obj)
        {
            if (_selectedStartupId > -1 && _selectedStartupId < (_startups.Count() - 1))
                return true;

            return false;
        }

        private void MoveStartupDown(object obj)
        {
            ((IEditableCollectionView)Startups).CommitEdit();
            var swapElement = _startups[_selectedStartupId];
            _startups[_selectedStartupId] = _startups[_selectedStartupId + 1];
            _startups[_selectedStartupId + 1] = swapElement;
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

            IsModified = true;
        }

        private void AddStartup(object obj)
        {
            ((IEditableCollectionView)Startups).CommitEdit();
            _startups.Add("");
            IsModified = true;
        }

        private void Load()
        {
            IsLoading = true;
            try
            {
                _crawls.Clear();
                _logos.Clear();
                _auxes.Clear(); 
                _parentals.Clear();
                _startups.Clear();
   
                foreach (Model.CgElement element in _cgElementsController.Crawls)
                    _crawls.Add(element);
                foreach (Model.CgElement element in _cgElementsController.Logos)
                    _logos.Add(element);
                foreach (Model.CgElement element in _cgElementsController.Auxes)
                    _auxes.Add(element);
                foreach (Model.CgElement element in _cgElementsController.Parentals)
                    _parentals.Add(element);
                foreach (var startupCommand in _cgElementsController.StartupsCommands)
                    _startups.Add(startupCommand);

                SelectedDefaultCrawl = _crawls.FirstOrDefault(c => c.Id == _cgElementsController.DefaultCrawl);
                SelectedDefaultLogo = _logos.FirstOrDefault(c => c.Id == _cgElementsController.DefaultLogo);
                IsEnabled = _cgElementsController.IsEnabled;
            }
            finally
            {
                IsLoading = false;
                IsModified = false;
            }
        }

        private void DeleteElement(object obj)
        {
            if (!(obj is Model.CgElement element))
                return;

        }

        private void EditElement(object obj)
        {
            if (!(obj is Model.CgElement element))
                return;
            CgElementViewModel = new CgElementViewModel(element);
        }

        private bool CanMoveCgElementDown(object obj)
        {
            if (_selectedElement != null && _selectedElement.Id < (SelectedElementList.Count() - 1))
                return true;

            return false;
        }

        private void MoveCgElementDown(object obj)
        {
            var swapElement = SelectedElementList.FirstOrDefault(c => c.Id == _selectedElement.Id + 1);

            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id += 1;
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
            var swapElement = SelectedElementList.FirstOrDefault(c => c.Id == _selectedElement.Id - 1);

            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id -= 1;
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
            CgElementViewModel = new CgElementViewModel(_newElement);
        }

        public void Save()
        {
            _cgElementsController.Auxes = _auxes.ToArray();
            _cgElementsController.Crawls = _crawls.ToArray();
            _cgElementsController.Logos = _logos.ToArray();
            _cgElementsController.Parentals = _parentals.ToArray();
            _cgElementsController.IsEnabled = _isEnabled;
            _cgElementsController.StartupsCommands = _startups.ToList();
            _cgElementsController.DefaultCrawl = SelectedDefaultCrawl?.Id ?? 1;
            _cgElementsController.DefaultLogo = SelectedDefaultLogo?.Id ?? 1;
            IsModified = false;
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CgElementWizardClosed(object sender, EventArgs e)
        {
            if (!(sender is OkCancelViewModelBase okCancelVm))
                return;

            //if (okCancelVm.DialogResult)
            //{
            //    if (_newElement != null)
            //    {
            //        _newElement.Id = (byte)_cgElements.Count();
            //        _cgElements.Add(_newElement);
            //        if (_newElement.CgType == Model.CgElement.Type.Crawl)
            //            Crawls.Refresh();
            //        else if (_newElement.CgType == Model.CgElement.Type.Logo)
            //            Logos.Refresh();
            //    }

            //    IsModified = true;
            //}

            //_newElement = null;
            //CgElementViewModel = null;
            //CgElements.Refresh();

        }

        public CgElementViewModel CgElementViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel == value)
                    return;
                _currentViewModel = value;
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
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;

                if (_cgElementsController != null)
                {
                    _cgElementsController.IsEnabled = value;
                    PluginChanged?.Invoke(this, EventArgs.Empty);
                }
                NotifyPropertyChanged();
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

        public Array ElementTypes { get; } = Enum.GetValues(typeof(ElementType));
        public ElementType SelectedElementType
        {
            get => _selectedElementType; 
            set
            {
                if (_selectedElementType == value)
                    return;
                _selectedElementType = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(SelectedElementList));
            }
        }

        public IList<Model.CgElement> SelectedElementList
        {
            get
            {
                switch (SelectedElementType)
                {
                    case ElementType.Aux:
                        return _auxes;
                    case ElementType.Crawl:
                        return _crawls;
                    case ElementType.Logo:
                        return _logos;
                    case ElementType.Parental:
                        return _parentals;
                }
                throw new InvalidOperationException("Invalid SelectedElementType");
            }
        }

        public string PluginName => "CG elements controller";
        public List<string> Startups { get; } = new List<string>();
        public int SelectedStartupId { get => _selectedStartupId; set => SetField(ref _selectedStartupId, value); }
        public Model.CgElement SelectedDefaultCrawl { get => _selectedDefaultCrawl; set => SetField(ref _selectedDefaultCrawl, value); }
        public Model.CgElement SelectedDefaultLogo { get => _selectedDefaultLogo; set => SetField(ref _selectedDefaultLogo, value); }


        protected override void OnDispose() { }

        public void Initialize(IPlugin model)
        {
            _cgElementsController = model as Model.CgElementsController
                ?? new Model.CgElementsController
                {
                    Crawls = new[] { new Model.CgElement { Id = 0, Name = "Off", Command = "PLAY CG3 EMPTY MIX 25" } },
                    Logos = new[] { new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG4 EMPTY MIX 25" } },
                    Parentals = new[] { new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG5 EMPTY MIX 25" } }
                };
            Load();
        }

        public object GetModel()
        {
            return _cgElementsController;
        }
    }
}
