using System;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPlugin
    {
        IUiMenuItem Menu { get; }
        Func<PluginExecuteContext> ExecutionContext { get; }
        void NotifyExecuteChanged();
    }
}
