using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

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

        private Model.CgElementsController _cgElementsController;

        private readonly ObservableCollection<CgElementViewModel> _crawls    = new ObservableCollection<CgElementViewModel>();
        private readonly ObservableCollection<CgElementViewModel> _logos     = new ObservableCollection<CgElementViewModel>();
        private readonly ObservableCollection<CgElementViewModel> _auxes     = new ObservableCollection<CgElementViewModel>();
        private readonly ObservableCollection<CgElementViewModel> _parentals = new ObservableCollection<CgElementViewModel>();
        private readonly ObservableCollection<string> _startups = new ObservableCollection<string>();
        private readonly IEngineProperties _engine;
        private CgElementViewModel _selectedElement;
        private ElementType _selectedElementType;
        private CgElementViewModel _selectedDefaultCrawl;
        private CgElementViewModel _selectedDefaultLogo;
        private int _selectedStartupId;
        private bool _isEnabled;

        public event EventHandler PluginChanged;

        public CgElementsControllerViewModel(IEngineProperties engine)
        {
            AddElementCommand = new UiCommand(AddElement, CanAddElement);
            MoveElementUpCommand = new UiCommand(MoveElementUp, CanMoveElementUp);
            MoveElementDownCommand = new UiCommand(MoveCgElementDown, CanMoveElementDown);
            DeleteElementCommand = new UiCommand(DeleteElement, CanDeleteElement);
            AddStartupCommand = new UiCommand(AddStartup);
            MoveStartupUpCommand = new UiCommand(MoveStartupUp, CanMoveStartupUp);
            MoveStartupDownCommand = new UiCommand(MoveStartupDown, CanMoveStartupDown);
            DeleteStartupCommand = new UiCommand(DeleteStartup);
            _engine = engine;
            _cgElementsController = engine.CGElementsController as Model.CgElementsController
                ?? new Model.CgElementsController
                {
                    Crawls = new[] { new Model.CgElement { Id = 0, Name = "Off", Command = "PLAY CG3 EMPTY MIX 25" } },
                    Logos = new[] { new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG4 EMPTY MIX 25" } },
                    Parentals = new[] { new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG5 EMPTY MIX 25" } }
                };
            Load();
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

        public void Load()
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
                    _crawls.Add(AddElementViewModel(element));
                foreach (Model.CgElement element in _cgElementsController.Logos)
                    _logos.Add(AddElementViewModel(element));
                foreach (Model.CgElement element in _cgElementsController.Auxes)
                    _auxes.Add(AddElementViewModel(element));
                foreach (Model.CgElement element in _cgElementsController.Parentals)
                    _parentals.Add(AddElementViewModel(element));
                foreach (var startupCommand in _cgElementsController.StartupsCommands)
                    _startups.Add(startupCommand);

                SelectedDefaultCrawl = _crawls.FirstOrDefault(c => c.Id == _cgElementsController.DefaultCrawl);
                SelectedDefaultLogo = _logos.FirstOrDefault(c => c.Id == _cgElementsController.DefaultLogo);
                IsEnabled = _cgElementsController.IsEnabled;
            }
            finally
            {
                IsLoading = false;
            }
            IsModified = false;
        }

        private CgElementViewModel AddElementViewModel(Model.CgElement element)
        {
            var vm = new CgElementViewModel(element);
            vm.ModifiedChanged += CgElement_ModifiedChanged;
            IsModified = true;
            return vm;
        }



        private bool CanDeleteElement(object o)
        {
            return o is CgElementViewModel;
        }

        private void DeleteElement(object o)
        {
            var vm = o as CgElementViewModel ?? throw new ArgumentException(nameof(o));
            if (Elements.Remove(vm))
                vm.ModifiedChanged -= CgElement_ModifiedChanged;
            IsModified = true;
        }


        private bool CanMoveElementDown(object obj)
        {
            if (_selectedElement != null && _selectedElement.Id < (Elements.Count() - 1))
                return true;
            return false;
        }

        private void MoveCgElementDown(object obj)
        {
            var swapElement = Elements.FirstOrDefault(c => c.Id == _selectedElement.Id + 1);

            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id += 1;
            IsModified = true;
        }

        private bool CanMoveElementUp(object obj)
        {
            if (_selectedElement != null && _selectedElement.Id > 0)
                return true;
            return false;
        }

        private void MoveElementUp(object obj)
        {
            var swapElement = Elements.FirstOrDefault(c => c.Id == _selectedElement.Id - 1);

            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id -= 1;
            IsModified = true;
        }

        private bool CanAddElement(object obj)
        {
            return true;
        }

        private void AddElement(object obj)
        {
            var newElement = new Model.CgElement();
            var newVm = AddElementViewModel(newElement);
            Elements.Add(newVm);
            SelectedElement = newVm;
        }

        public void Save()
        {
            foreach (var e in _auxes
                .Union(_crawls)
                .Union(_logos)
                .Union(_parentals))
                e.Update();
            _cgElementsController.Auxes = _auxes.Select(e => e.Element).ToArray();
            _cgElementsController.Crawls = _crawls.Select(e => e.Element).ToArray();
            _cgElementsController.Logos = _logos.Select(e => e.Element).ToArray();
            _cgElementsController.Parentals = _parentals.Select(e => e.Element).ToArray();
            _cgElementsController.IsEnabled = _isEnabled;
            _cgElementsController.StartupsCommands = _startups.ToList();
            _cgElementsController.DefaultCrawl = SelectedDefaultCrawl?.Id ?? 1;
            _cgElementsController.DefaultLogo = SelectedDefaultLogo?.Id ?? 1;
            _engine.CGElementsController = _cgElementsController;
            IsModified = false;
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public CgElementViewModel SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (value == _selectedElement)
                    return;
                _selectedElement = value;
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

        public ICommand AddElementCommand { get; }
        public ICommand MoveElementUpCommand { get; }
        public ICommand MoveElementDownCommand { get; }
        public ICommand DeleteElementCommand { get; }
        public ICommand AddStartupCommand { get; }
        public ICommand MoveStartupUpCommand { get; }
        public ICommand MoveStartupDownCommand { get; }
        public ICommand DeleteStartupCommand { get; }

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
                NotifyPropertyChanged(nameof(Elements));
            }
        }

        public IList<CgElementViewModel> Elements
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
                    default:
                        throw new InvalidOperationException("Invalid SelectedElementType");
                }
            }
        }

        public string PluginName => "CG elements controller";

        public IPlugin Model => _cgElementsController;

        public List<string> Startups { get; } = new List<string>();
        public int SelectedStartupId { get => _selectedStartupId; set => SetField(ref _selectedStartupId, value); }
        public CgElementViewModel SelectedDefaultCrawl { get => _selectedDefaultCrawl; set => SetField(ref _selectedDefaultCrawl, value); }
        public CgElementViewModel SelectedDefaultLogo { get => _selectedDefaultLogo; set => SetField(ref _selectedDefaultLogo, value); }


        protected override void OnDispose() { }

        public object GetModel()
        {
            return _cgElementsController;
        }
        
        private void CgElement_ModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }
    }
}
