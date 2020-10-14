using System.Windows.Controls;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ChannelViewModel : ViewModelBase
    {
        private int _selectedTabIndex;

        public ChannelViewModel(IEngine engine, bool showEngine, bool showMedia)
        {
            DisplayName = engine.EngineName;
            if (showEngine)
                Engine = new EngineViewModel(engine, engine.Preview);
            if (showMedia)
                MediaManager = new MediaManagerViewModel(engine, engine.HaveRight(EngineRight.Preview) ? engine.Preview : null);
            CommandSwitchTab = new UiCommand(o => SelectedTabIndex = _selectedTabIndex == 0 ? 1 : 0, o => showEngine && showMedia);
            SelectedTabIndex = showEngine ? 0 : 1;
        }

        public ICommand CommandSwitchTab { get; }

        public string DisplayName { get; }

        public EngineViewModel Engine { get; }

        public MediaManagerViewModel MediaManager { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetField(ref _selectedTabIndex, value);
        }

        public TabItem SelectedItem
        {
            set
            {
                if (value?.DataContext is EngineViewModel engine)
                    OnIdle(() => engine.Focus());
            }
        }

        protected override void OnDispose()
        {
            Engine?.Dispose();
            MediaManager?.Dispose();
        }
    }
}
