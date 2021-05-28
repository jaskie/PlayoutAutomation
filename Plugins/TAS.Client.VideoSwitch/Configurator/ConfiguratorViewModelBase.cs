using System;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Server.VideoSwitch.Configurator
{
    public abstract class ConfiguratorViewModelBase : ModifyableViewModelBase, IPluginConfiguratorViewModel
    {
        protected readonly IEngineProperties Engine;
        private bool _isEnabled;

        public ConfiguratorViewModelBase(IEngineProperties engine)
        {
            CommandConnect = new UiCommand(_ => Connect(), _ => CanConnect());
            CommandDisconnect = new UiCommand(_ => Disconnect());
            Engine = engine;
        }

        public abstract IPlugin Model { get; }

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

        public UiCommand CommandConnect { get; }
        public UiCommand CommandDisconnect { get; }

        public bool IsConnected => (Model as IVideoSwitch)?.IsConnected ?? false;

        public bool IsEnabled { get => _isEnabled; set => SetField(ref _isEnabled, value); }

        public abstract string PluginName { get; }
    }
}
