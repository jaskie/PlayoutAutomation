using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Views;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ChannelViewmodel : ViewmodelBase
    {
        readonly ChannelView _channelView;
        readonly EngineViewmodel _engineViewModel;
        readonly MediaManagerViewmodel _mediaManagerViewmodel;
        readonly EngineStateViewmodel _engineStateViewmodel;
        readonly string _channelName;
        public ICommand CommandSwitchTab { get; private set; }
        public ChannelViewmodel(IEngine engine)
        {
            _channelName = engine.EngineName;
            _engineViewModel = new EngineViewmodel(engine, engine);
            _mediaManagerViewmodel = new MediaManagerViewmodel(engine.MediaManager, engine);
            _engineStateViewmodel = new EngineStateViewmodel(engine);
            _channelView = new ChannelView { DataContext = this };
            CommandSwitchTab = new UICommand { ExecuteDelegate = o => SelectedTab = _selectedTab == 0 ? 1 : 0 };
        }

        public EngineViewmodel EngineViewmodel { get { return _engineViewModel; } }
        public MediaManagerViewmodel MediaManagerViewmodel { get { return _mediaManagerViewmodel; } }
        public EngineStateViewmodel EngineStateViewmodel { get { return _engineStateViewmodel; } }

        public ChannelView View { get { return _channelView; } }
        public string ChannelName { get { return _channelName; } }

        private int _selectedTab;
        public int SelectedTab { get { return _selectedTab; } set { SetField(ref _selectedTab, value, nameof(SelectedTab)); } }

        protected override void OnDispose()
        {
            _engineViewModel.Dispose();
            _mediaManagerViewmodel.Dispose();
        }
    }
}
