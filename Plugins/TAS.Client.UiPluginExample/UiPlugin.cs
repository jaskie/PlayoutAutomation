using System.Diagnostics;
using TAS.Client.Common.Plugin;

namespace TAS.Client.UiPluginExample
{
    public class UiPlugin : IUiPlugin
    {
        private readonly UiMenuItem _menu;

        public UiPlugin(IUiPluginContext context)
        {
            Context = context;
            _menu = new UiMenuItem(this) {Header = "Play"};
            Debug.WriteLine(this, "Plugin created");
            context.PropertyChanged += Context_PropertyChanged;
        }

        public IUiPluginContext Context { get; }


        public IUiMenuItem Menu => _menu;

        private void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IUiEngine.SelectedEvent))
                _menu.NotifyExecuteChanged();
        }

    }
}
