using NLog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Advantech.Configurator
{
    [Export(typeof(IPluginConfigurator))]
    public class GpiViewModel : ModifyableViewModelBase, IPluginConfigurator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Model.Gpi _gpi = new Model.Gpi();
        private Model.Gpi _testGpi = new Model.Gpi();
        private ObservableCollection<Model.GpiBinding> _gpiBindings = new ObservableCollection<Model.GpiBinding>();
        private GpiBindingViewModel _gpiBindingViewModel;
        private bool _isEnabled;
        
        public GpiViewModel()
        {            
            AddGpiBindingCommand = new UiCommand(AddGpiBinding, CanAddGpiBinding);
            DeleteGpiBindingCommand = new UiCommand(DeleteGpiBinding);
            SaveCommand = new UiCommand(LocalSave, CanLocalSave);
            UndoCommand = new UiCommand(Undo, CanUndo);
            GpiBindings = CollectionViewSource.GetDefaultView(_gpiBindings);
        }        

        private bool CanAddGpiBinding(object obj)
        {
            if (_gpiBindingViewModel == null)
                return true;
            return false;
        }

        private void Undo(object obj)
        {
            Init();
        }

        private bool CanUndo(object obj)
        {
            return IsModified;
        }

        private bool CanLocalSave(object obj)
        {
            return IsModified;
        }
        
        private void Init()
        {            
            if (_gpi == null)
                return;
            
            _gpiBindings = _gpi.Bindings;
            GpiBindings = CollectionViewSource.GetDefaultView(_gpiBindings);
            IsEnabled = _gpi.IsEnabled;
            
            if (_testGpi != null)
            {
                _testGpi.Dispose();
                _testGpi = new Model.Gpi();
            }

            IsModified = false;
        }

        private void DeleteGpiBinding(object obj)
        {
            if (!(obj is Model.GpiBinding gpiBinding))
                return;

            _gpiBindings.Remove(gpiBinding);
            _testGpi.Bindings.Remove(gpiBinding);
            GpiBindings.Refresh();
            IsModified = true;
        }

        private void AddGpiBinding(object obj)
        {                        
            GpiBindingViewModel = new GpiBindingViewModel();            
        }

        public event EventHandler PluginChanged;               

        public object GetModel()
        {
            return _gpi;
        }

        public void Initialize(object model)
        {
            UiServices.AddDataTemplate(typeof(GpiViewModel), typeof(GpiView));
            _gpi = model as Model.Gpi;
            Init();
        }

        public void Save()
        {
           
        }

        private void LocalSave(object obj)
        {
            _gpi = new Model.Gpi()
            {
                Bindings = _gpiBindings,
                IsEnabled = _isEnabled
            };

            PluginChanged?.Invoke(this, EventArgs.Empty);
            IsModified = false;
        }     

        public ICollectionView GpiBindings { get; private set; }
        public UiCommand AddGpiBindingCommand { get; }
        public UiCommand DeleteGpiBindingCommand { get; }
        public UiCommand SaveCommand { get; }
        public UiCommand UndoCommand { get; }

        public string PluginName => "Gpi Advantech";

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (!SetField(ref _isEnabled, value))
                    return;

                if (_gpi != null)
                    _gpi.IsEnabled = value;

                PluginChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public GpiBindingViewModel GpiBindingViewModel
        {
            get => _gpiBindingViewModel; 
            set
            {
                var old = _gpiBindingViewModel;

                if (_gpiBindingViewModel == value)
                    return;

                if (old != null)
                    old.Closing -= GpiBindingViewModel_Closing;

                _gpiBindingViewModel = value;

                if (value != null)
                    _gpiBindingViewModel.Closing += GpiBindingViewModel_Closing;
                
                NotifyPropertyChanged();
            }
        }

        private void GpiBindingViewModel_Closing(object sender, EventArgs e)
        {
            if (!(sender is GpiBindingViewModel gpiBindingVm))
                return;

            if (gpiBindingVm.DialogResult)
            {
                _gpiBindings.Add(gpiBindingVm.GpiBinding);
                _testGpi.Bindings.Add(gpiBindingVm.GpiBinding);
                IsModified = true;
            }            
            
            gpiBindingVm.Dispose();
            GpiBindingViewModel = null;
        }

        protected override void OnDispose()
        {
            
        }
    }
}
