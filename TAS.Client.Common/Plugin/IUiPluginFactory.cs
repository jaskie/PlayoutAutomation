using System;
using System.Collections;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPluginFactory
    {
        object[] Create(IUiPluginContext context);
        Type Type { get; }
    }
}
