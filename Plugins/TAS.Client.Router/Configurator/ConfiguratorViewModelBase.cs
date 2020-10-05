using System.Collections.Generic;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public abstract class ConfiguratorViewModelBase : ModifyableViewModelBase
    {
        private bool _isEnabled;
        
        public ConfiguratorViewModelBase(RouterBase router)
        {
            Router = router;
            CommandConnect = new UiCommand(Connect, CanConnect);
            CommandDisconnect = new UiCommand(Disconnect, CanDisconnect);

            IsEnabled = Router?.IsEnabled ?? false;
            Init();
        }

        private bool CanDisconnect(object obj)
        {
            if (TestRouter != null)
                return true;

            return false;
        }

        protected virtual void Disconnect(object obj)
        {
            TestRouter.Dispose();
            TestRouter = null;

            NotifyPropertyChanged(nameof(IsConnected));
        }

        protected abstract void Connect(object obj);
        protected abstract bool CanConnect(object obj);
        protected abstract void Init();
        public abstract void Save();
        public abstract bool CanSave();
        public virtual void Undo()
        {
            Init();
            IsModified = false;
        }

        public virtual bool CanUndo()
        {
            return IsModified;
        }
        public RouterBase GetModel()
        {
            return Router;
        }

        protected RouterBase Router;
        protected RouterBase TestRouter;

        public UiCommand CommandConnect { get; }
        public UiCommand CommandDisconnect { get; }
               
        public bool IsConnected => TestRouter?.IsConnected ?? false;    
        public bool IsEnabled 
        { 
            get => _isEnabled;
            set
            {
                if (value == _isEnabled)
                    return;
                _isEnabled = value;
                if (Router != null)
                    Router.IsEnabled = value;
                NotifyPropertyChanged();                
            }
        }
        public IList<IVideoSwitchPort> TestSources => TestRouter?.Sources;
          
    }
}
