using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Client.Common.Plugin;

namespace UiPluginExample
{
    [Export(typeof(IUiPlugin))]
    public class UIPlugin : IUiPlugin
    {
        public UIPlugin()
        {
            Debug.WriteLine("Plugin");
        }
        public bool Engine { get; set; }
        
        public string Header { get { return System.Reflection.Assembly.GetExecutingAssembly().FullName; } }

        public IEnumerable<IUiMenuItem> Items { get { return null; } }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}
