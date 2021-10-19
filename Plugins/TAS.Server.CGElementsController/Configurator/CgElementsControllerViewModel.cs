using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Server.CgElementsController.Configurator
{
    internal class CgElementsControllerViewModel : ModifyableViewModelBase, IPluginConfiguratorViewModel
    {
        public enum ElementType
        {
            Logo,
            Parental,
            Crawl,
            Aux
        }

        private Model.CgElementsController _cgElementsController;

        private readonly ObservableCollection<CgElementViewModel> _crawls = new ObservableCollection<CgElementViewModel>();
        private readonly ObservableCollection<CgElementViewModel> _logos = new ObservableCollection<CgElementViewModel>();
        private readonly ObservableCollection<CgElementViewModel> _auxes = new ObservableCollection<CgElementViewModel>();
        private readonly ObservableCollection<CgElementViewModel> _parentals = new ObservableCollection<CgElementViewModel>();
        private readonly ICollectionView _startupCommandsCollectionView;
        private readonly IEngineProperties _engine;
        private CgElementViewModel _selectedElement;
        private ElementType _selectedElementType;
        private CgElementViewModel _selectedDefaultCrawl;
        private CgElementViewModel _selectedDefaultLogo;
        private bool _isEnabled;
        private StartupCommandViewModel _selectedStartupCommand;

        public event EventHandler PluginChanged;

        public CgElementsControllerViewModel(IEngineProperties engine)
        {
            AddElementCommand = new UiCommand(AddElement, CanAddElement);
            MoveElementUpCommand = new UiCommand(MoveElementUp, CanMoveElementUp);
            MoveElementDownCommand = new UiCommand(MoveElementDown, CanMoveElementDown);
            DeleteElementCommand = new UiCommand(DeleteElement, CanDeleteElement);
            SetDefaultElementCommand = new UiCommand(SetDefaultElement, CanSetDefaultElement);
            AddStartupCommandCommand = new UiCommand(AddStartupCommand);
            MoveStartupCommandUpCommand = new UiCommand(MoveStartupCommandUp, CanMoveStartupCommandUp);
            MoveStartupCommandDownCommand = new UiCommand(MoveStartupCommandDown, CanMoveStartupCommandDown);
            DeleteStartupCommandCommand = new UiCommand(DeleteStartupCommand, CanDeleteStartupCommand);
            _engine = engine;
            _cgElementsController = engine.CGElementsController as Model.CgElementsController
                ?? new Model.CgElementsController
                {
                    Crawls = new[] { new Model.CgElement { Id = 0, Name = "Off", Command = "PLAY CG3 EMPTY MIX 25" } },
                    Logos = new[] { new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG4 EMPTY MIX 25" } },
                    Parentals = new[] { new Model.CgElement { Id = 0, Name = "None", Command = "PLAY CG5 EMPTY MIX 25" } }
                };
            _startupCommandsCollectionView = CollectionViewSource.GetDefaultView(StartupCommands);
            Load();
        }

        private bool CanSetDefaultElement(object o)
        {
            return (SelectedElementType == ElementType.Logo || SelectedElementType == ElementType.Crawl) 
                && o is CgElementViewModel;
        }

        private void SetDefaultElement(object o)
        {
            var vm = o as CgElementViewModel ?? throw new ArgumentException(nameof(o));
            switch (SelectedElementType)
            {
                case ElementType.Logo:
                    SelectedDefaultLogo = vm;
                    break;
                case ElementType.Crawl:
                    SelectedDefaultCrawl = vm;
                    break;
                default:
                    throw new ApplicationException("Invalid list type to set default element");
            }
        }

        private bool CanDeleteStartupCommand(object o)
        {
            return o is StartupCommandViewModel;
        }

        private void DeleteStartupCommand(object o)
        {
            var vm = o as StartupCommandViewModel ?? throw new ArgumentException(nameof(o));
            if (StartupCommands.Remove(vm))
                vm.ModifiedChanged -= Item_ModifiedChanged;
            IsModified = true;
        }


        private bool CanMoveStartupCommandDown(object _)
        {
            if (SelectedStartupCommand != null && StartupCommands.IndexOf(SelectedStartupCommand) < (StartupCommands.Count() - 1))
                return true;
            return false;
        }

        private void MoveStartupCommandDown(object _)
        {
            ((IEditableCollectionView)_startupCommandsCollectionView).CommitEdit();
            var currentIndex = StartupCommands.IndexOf(SelectedStartupCommand);
            StartupCommands.Move(currentIndex, currentIndex + 1);
            IsModified = true;
        }

        private bool CanMoveStartupCommandUp(object _)
        {
            if (SelectedStartupCommand != null && StartupCommands.IndexOf(SelectedStartupCommand) > 0)
                return true;
            return false;
        }

        private void MoveStartupCommandUp(object obj)
        {
            ((IEditableCollectionView)_startupCommandsCollectionView).CommitEdit();
            ((IEditableCollectionView)_startupCommandsCollectionView).CommitEdit();
            var currentIndex = StartupCommands.IndexOf(SelectedStartupCommand);
            StartupCommands.Move(currentIndex, currentIndex - 1);

            IsModified = true;
        }

        private void AddStartupCommand(object obj)
        {
            ((IEditableCollectionView)_startupCommandsCollectionView).CommitEdit();
            StartupCommands.Add(AddStartupCommandViewModel(string.Empty));
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
                StartupCommands.Clear();

                foreach (Model.CgElement element in _cgElementsController.Crawls)
                    _crawls.Add(AddElementViewModel(element));
                foreach (Model.CgElement element in _cgElementsController.Logos)
                    _logos.Add(AddElementViewModel(element));
                foreach (Model.CgElement element in _cgElementsController.Auxes)
                    _auxes.Add(AddElementViewModel(element));
                foreach (Model.CgElement element in _cgElementsController.Parentals)
                    _parentals.Add(AddElementViewModel(element));
                foreach (var startupCommand in _cgElementsController.StartupsCommands)
                    StartupCommands.Add(AddStartupCommandViewModel(startupCommand));

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
            vm.ModifiedChanged += Item_ModifiedChanged;
            IsModified = true;
            return vm;
        }

        private StartupCommandViewModel AddStartupCommandViewModel(string command)
        {
            var vm = new StartupCommandViewModel(command);
            vm.ModifiedChanged += Item_ModifiedChanged;
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
            {
                vm.ModifiedChanged -= Item_ModifiedChanged;
                if (SelectedElementType == ElementType.Logo && SelectedDefaultLogo == vm)
                    SelectedDefaultLogo = Logos.FirstOrDefault();
                if (SelectedElementType == ElementType.Crawl && SelectedDefaultCrawl == vm)
                    SelectedDefaultCrawl = Crawls.FirstOrDefault(); ;
            }
            IsModified = true;
        }

        private bool CanMoveElementDown(object obj)
        {
            if (_selectedElement != null && Elements.IndexOf(_selectedElement) < (Elements.Count() - 1))
                return true;
            return false;
        }

        private void MoveElementDown(object obj)
        {
            var currentIndex = Elements.IndexOf(SelectedElement);
            var swapElement = Elements.ElementAt(currentIndex + 1);
            Elements.Move(currentIndex, currentIndex + 1);
            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id += 1;
        }

        private bool CanMoveElementUp(object obj)
        {
            if (SelectedElement != null && Elements.IndexOf(SelectedElement) > 0)
                return true;
            return false;
        }

        private void MoveElementUp(object obj)
        {
            var currentIndex = Elements.IndexOf(SelectedElement);
            var swapElement = Elements.ElementAt(currentIndex - 1);
            Elements.Move(currentIndex, currentIndex - 1);
            swapElement.Id = _selectedElement.Id;
            _selectedElement.Id -= 1;
        }

        private bool CanAddElement(object obj)
        {
            if (Elements.Max(e => e.Id) >= 255) // only byte length
                return false;
            return true;
        }

        private void AddElement(object obj)
        {
            var newElement = new Model.CgElement { Id = (byte)(Elements.Any() ? (Elements.Max(e => e.Id) + 1) : 0) };
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
            _cgElementsController.StartupsCommands = StartupCommands.Select(s => s.Command).ToList();
            _cgElementsController.DefaultCrawl = SelectedDefaultCrawl?.Id ?? 1;
            _cgElementsController.DefaultLogo = SelectedDefaultLogo?.Id ?? 1;
            _engine.CGElementsController = _cgElementsController;
            IsModified = false;
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public CgElementViewModel SelectedElement
        {
            get => _selectedElement;
            set => SetFieldNoModify(ref _selectedElement, value);
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
        public ICommand SetDefaultElementCommand { get; }
        public ICommand AddStartupCommandCommand { get; }
        public ICommand MoveStartupCommandUpCommand { get; }
        public ICommand MoveStartupCommandDownCommand { get; }
        public ICommand DeleteStartupCommandCommand { get; }

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

        public ObservableCollection<CgElementViewModel> Elements
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

        public ObservableCollection<CgElementViewModel> Crawls => _crawls;

        public ObservableCollection<CgElementViewModel> Logos => _logos;

        public string PluginName => "CG elements controller";

        public IPlugin Model => _cgElementsController;

        public ObservableCollection<StartupCommandViewModel> StartupCommands { get; } = new ObservableCollection<StartupCommandViewModel>();

        public StartupCommandViewModel SelectedStartupCommand { get => _selectedStartupCommand; set => SetFieldNoModify(ref _selectedStartupCommand, value); }

        public CgElementViewModel SelectedDefaultCrawl { get => _selectedDefaultCrawl; set => SetField(ref _selectedDefaultCrawl, value); }

        public CgElementViewModel SelectedDefaultLogo { get => _selectedDefaultLogo; set => SetField(ref _selectedDefaultLogo, value); }


        protected override void OnDispose() { }

        public object GetModel()
        {
            return _cgElementsController;
        }

        private void Item_ModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }
    }
}
