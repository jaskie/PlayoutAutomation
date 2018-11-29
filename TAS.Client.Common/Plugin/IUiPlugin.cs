using System;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPlugin : IUiMenuItem
    {
        Func<PluginExecuteContext> ExecutionContext { get; set; }
        void NotifyExecuteChanged();
    }
}
