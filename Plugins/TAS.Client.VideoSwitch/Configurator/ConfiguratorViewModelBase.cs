using System.Collections.ObjectModel;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public abstract class ConfiguratorViewModelBase<TVideoSwitch> : ModifyableViewModelBase, IPluginConfiguratorViewModel where TVideoSwitch : VideoSwitchBase,  new()
    {
        protected readonly TVideoSwitch VideoSwitch;
        protected readonly IEngineProperties Engine;
        private bool _isEnabled;
        private string _ipAddress;

        public ConfiguratorViewModelBase(IEngineProperties engine)
        {
            Engine = engine;
            VideoSwitch = engine.VideoSwitch as TVideoSwitch ?? new TVideoSwitch();
            CommandConnect = new UiCommand(_ => Connect(), _ => CanConnect());
            CommandDisconnect = new UiCommand(_ => Disconnect());
        }

        public IPlugin Model => VideoSwitch;

        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }

        protected abstract void Connect();
        protected abstract void Disconnect();
        protected abstract bool CanConnect();

        public virtual void Save()
        {
            VideoSwitch.IsEnabled = IsEnabled;
        }

        public abstract bool CanSave();

        public virtual bool CanUndo()
        {
            return IsModified;
        }

        public virtual void Load()
        {
            _isEnabled = VideoSwitch.IsEnabled;
        }

        public UiCommand CommandAddPort { get; }
        public UiCommand CommandDeletePort { get; }

        public ObservableCollection<OutputPortViewModel> OutputPorts { get; }

        public UiCommand CommandConnect { get; }
        public UiCommand CommandDisconnect { get; }

        public bool IsConnected => (VideoSwitch as IVideoSwitch)?.IsConnected ?? false;

        public bool IsEnabled { get => _isEnabled; set => SetField(ref _isEnabled, value); }

        public abstract string PluginName { get; }

        public abstract bool IsVideoSwitcher { get; }


    }
}
