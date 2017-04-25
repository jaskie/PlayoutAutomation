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
        readonly string _channelName;
        public ICommand CommandSwitchTab { get; private set; }
        public ChannelViewmodel(IEngine engine, bool showEngine, bool showMedia, bool allowPlayControl)
        {
            _channelName = engine.EngineName;
            if (showEngine)
                Engine = new EngineViewmodel(engine, engine, allowPlayControl);
            if (showMedia)
                MediaManager = new MediaManagerViewmodel(engine.MediaManager, engine);
            CommandSwitchTab = new UICommand { ExecuteDelegate = o => SelectedTabIndex = _selectedTabIndex == 0 ? 1 : 0, CanExecuteDelegate = o => showEngine && showMedia };
            SelectedTabIndex = showEngine ? 0 : 1;
        }

        public string ChannelName { get { return _channelName; } }

        public EngineViewmodel Engine { get; private set; }
        public MediaManagerViewmodel MediaManager { get; private set; }

        private int  _selectedTabIndex;

        public int SelectedTabIndex { get { return _selectedTabIndex; } set { SetField(ref _selectedTabIndex, value); } }


        protected override void OnDispose()
        {
            Engine.Dispose();
            MediaManager.Dispose();
        }
    }
}
