using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Client.Common.Plugin;
using TAS.Server.Common;

namespace UiPluginExample
{
    [Export(typeof(IUiPlugin))]
    public class UIPlugin : IUiPlugin
    {
        public UIPlugin()
        {
            Debug.WriteLine(this, "Plugin created");
        }
        private PluginExecuteContext _executionContext()
        {
            var h = ExecutionContext;
            return h == null ? new PluginExecuteContext { } : h();
        }
        
        public string Header { get { return "Play"; } }

        public IEnumerable<IUiMenuItem> Items { get { return null; } }
        
        public event EventHandler CanExecuteChanged;
                
        public bool CanExecute(object parameter)
        {
            var ec = _executionContext();
            return ec.Event != null && ec.Event.EventType == TEventType.Rundown;
        }

        public void Execute(object parameter)
        {
            var ec = _executionContext();
            ec.Engine.Start(ec.Event);
        }

        public void NotifyExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        [Import]
        public Func<PluginExecuteContext> ExecutionContext { get; set; }
    }
}
