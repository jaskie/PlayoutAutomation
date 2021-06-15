using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public abstract class ConfiguratorViewModelBase : ModifyableViewModelBase, IPluginConfiguratorViewModel
    {
        protected readonly IEngineProperties Engine;
        private bool _isEnabled;
        private string _ipAddress;

        public ConfiguratorViewModelBase(IEngineProperties engine)
        {
            Engine = engine;
            CommandConnect = new UiCommand(_ => Connect(), _ => CanConnect());
            CommandDisconnect = new UiCommand(_ => Disconnect());
            CommandAddPort = new UiCommand(AddOutputPort);
            CommandDeletePort = new UiCommand(DeleteOutputPort);
        }

        public abstract IPlugin Model { get; }

        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }

        protected abstract void Connect();
        protected abstract void Disconnect();
        protected abstract bool CanConnect();

        public virtual void Save()
        {
            Model.IsEnabled = IsEnabled;
        }

        public abstract bool CanSave();

        public virtual bool CanUndo()
        {
            return IsModified;
        }

        public virtual void Load()
        {
            _isEnabled = Model.IsEnabled;
        }

        public UiCommand CommandAddPort { get; }
        public UiCommand CommandDeletePort { get; }

        public ObservableCollection<PortInfo> Ports { get; } = new ObservableCollection<PortInfo>();

        public UiCommand CommandConnect { get; }
        public UiCommand CommandDisconnect { get; }

        public bool IsConnected => (Model as IVideoSwitch)?.IsConnected ?? false;

        public bool IsEnabled { get => _isEnabled; set => SetField(ref _isEnabled, value); }

        public abstract string PluginName { get; }

        private void DeleteOutputPort(object obj)
        {
            if (!(obj is PortInfo port))
                return;
            Ports.Remove(port);
        }

        private void AddOutputPort(object obj)
        {
            var lastItem = Ports.LastOrDefault();
            Ports.Add(new PortInfo((short)(lastItem == null ? 0 : lastItem.Id + 1), string.Empty));
            IsModified = true;
        }

    }
}
