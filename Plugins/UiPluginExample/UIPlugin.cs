using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using TAS.Client.Common.Plugin;
using TAS.Common;

namespace UiPluginExample
{
    [Export(typeof(IUiPlugin))]
    public class UiPlugin : IUiPlugin
    {
        public UiPlugin()
        {
            Debug.WriteLine(this, "Plugin created");
        }
        private PluginExecuteContext _executionContext()
        {
            var h = ExecutionContext;
            return h?.Invoke() ?? new PluginExecuteContext();
        }
        
        public string Header => "Play";

        public IEnumerable<IUiMenuItem> Items => null;

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
