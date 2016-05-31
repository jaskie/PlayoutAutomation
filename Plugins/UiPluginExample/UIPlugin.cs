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
        public bool Engine
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Header
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<IUiMenuItem> Items
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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
