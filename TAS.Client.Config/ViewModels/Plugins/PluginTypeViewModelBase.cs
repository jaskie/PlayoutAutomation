using System;
using TAS.Client.Common;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public abstract class PluginTypeViewModelBase : ViewModelBase
    {
        public event EventHandler PluginChanged;
        protected override void OnDispose() { }
        public string Name { get; protected set; }

        protected void RaisePluginChanged()
        {
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
