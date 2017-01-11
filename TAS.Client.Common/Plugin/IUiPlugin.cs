using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPlugin : IUiMenuItem
    {
        Func<PluginExecuteContext> ExecutionContext { get; set; }
        void NotifyExecuteChanged();
    }

    public struct PluginExecuteContext
    {
        public IEngine Engine;
        public IEvent Event;
    }
}
