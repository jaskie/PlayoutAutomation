using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using TAS.Client.Common.Plugin;

namespace UiPluginExample
{
    [Export(typeof(IUiPlugin))]
    public class UiPlugin : IUiPlugin
    {
        private readonly UiMenuItemBase _menu;

        public UiPlugin()
        {
            _menu = new UiMenuItemBase(this) {Header = "Play"};
            Debug.WriteLine(this, "Plugin created");
        }
        
        public void NotifyExecuteChanged()
        {
            _menu.NotifyExecuteChanged();
        }

        public IUiMenuItem Menu => _menu;

        [Import]
        public Func<PluginExecuteContext> ExecutionContext { get; set; }
    }
}
