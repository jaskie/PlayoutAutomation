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
        readonly string _channelName;
        public ICommand CommandSwitchTab { get; private set; }
        public ChannelViewmodel(IEngine engine, bool showEngine, bool showMedia, bool allowPlayControl)
        {
            _channelName = engine.EngineName;
            _tabs = new List<ViewmodelBase>();
            if (showEngine)
                _tabs.Add(new EngineViewmodel(engine, engine, allowPlayControl));
            if (showMedia)
                _tabs.Add(new MediaManagerViewmodel(engine.MediaManager, engine));
            //_engineStateViewmodel = new EngineStateViewmodel(engine);
            _channelView = new ChannelView { DataContext = this };
            CommandSwitchTab = new UICommand { ExecuteDelegate = o => SelectedTab = _selectedTab == _tabs[0] ? _tabs[1] : _tabs[0], CanExecuteDelegate = o => _tabs.Count > 1 };
            if (_tabs.Count > 0)
                _selectedTab = _tabs[0];
        }


        public ChannelView View { get { return _channelView; } }
        public string ChannelName { get { return _channelName; } }

        private ViewmodelBase _selectedTab;
        private readonly List<ViewmodelBase> _tabs;

        public ViewmodelBase SelectedTab { get { return _selectedTab; } set { SetField(ref _selectedTab, value, nameof(SelectedTab)); } }

        public List<ViewmodelBase> Tabs { get { return _tabs; } }

        protected override void OnDispose()
        {
            _tabs.ForEach(t => t.Dispose());
        }
    }
}
