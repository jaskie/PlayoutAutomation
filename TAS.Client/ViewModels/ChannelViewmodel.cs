using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ChannelViewmodel : ViewModelBase
    {
        private int _selectedTabIndex;

        public ChannelViewmodel(IEngine engine, bool showEngine, bool showMedia)
        {
            DisplayName = engine.EngineName;
            if (showEngine)
                Engine = new EngineViewmodel(engine, engine);
            if (showMedia)
                MediaManager = new MediaManagerViewmodel(engine, engine.HaveRight(EngineRight.Preview) ? engine : null);
            CommandSwitchTab = new UiCommand(o => SelectedTabIndex = _selectedTabIndex == 0 ? 1 : 0, o => showEngine && showMedia);
            SelectedTabIndex = showEngine ? 0 : 1;
        }

        public ICommand CommandSwitchTab { get; }

        public string DisplayName { get; }

        public EngineViewmodel Engine { get; }

        public MediaManagerViewmodel MediaManager { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetField(ref _selectedTabIndex, value);
        }

        protected override void OnDispose()
        {
            Engine?.Dispose();
            MediaManager?.Dispose();
        }
    }
}
