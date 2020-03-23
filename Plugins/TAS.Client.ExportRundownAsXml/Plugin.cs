using System.Diagnostics;
using TAS.Client.Common.Plugin;

namespace TAS.Client.UiPluginExample
{
    public class Plugin : IUiPlugin
    {
        private readonly MenuItem _menu;

        public Plugin(IUiPluginContext context)
        {
            Context = context;
            _menu = new MenuItem(this) {Header = "Export selected rundown as XML"};
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
