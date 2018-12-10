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
        }

        public IUiPluginContext Context { get; }

        public void NotifyExecuteChanged()
        {
            _menu.NotifyExecuteChanged();
        }

        public IUiMenuItem Menu => _menu;

        
    }
}
