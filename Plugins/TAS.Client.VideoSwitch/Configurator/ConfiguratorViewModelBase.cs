using System;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Server.VideoSwitch.Configurator
{
    public abstract class ConfiguratorViewModelBase : ModifyableViewModelBase, IPluginConfiguratorViewModel
    {
        private bool _isEnabled;
        private bool _isConnected;
        protected readonly IEngineProperties Engine;

        public ConfiguratorViewModelBase(IEngineProperties engine)
        {
            CommandConnect = new UiCommand(Connect, CanConnect);
            CommandDisconnect = new UiCommand(Disconnect);

            IsEnabled = engine.VideoSwitch?.IsEnabled ?? false;
            Engine = engine;
        }

        public abstract IPlugin Model { get; }


        protected virtual void Disconnect(object obj)
        {
            NotifyPropertyChanged(nameof(IsConnected));
        }

        protected abstract void Connect(object obj);
        protected abstract bool CanConnect(object obj);
        
        public virtual void Save()
        {
            Model.IsEnabled = IsEnabled;
        }

        public abstract bool CanSave();

        public virtual bool CanUndo()
        {
            return IsModified;
        }

        public abstract void Load();

        public UiCommand CommandConnect { get; }
        public UiCommand CommandDisconnect { get; }

        public bool IsConnected { get => _isConnected; protected set => _isConnected = value; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        public abstract string PluginName { get; }
    }
}
