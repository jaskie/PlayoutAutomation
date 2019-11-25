using System;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPlugin
    {
        IUiMenuItem Menu { get; }

        IUiPluginContext Context { get; }

    }
}
