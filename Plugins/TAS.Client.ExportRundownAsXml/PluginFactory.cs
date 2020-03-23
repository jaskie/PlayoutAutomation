using System;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;

namespace TAS.Client.UiPluginExample
{
    [Export(typeof(IUiPluginFactory))]
    public class PluginFactory: IUiPluginFactory
    {
        public object[] Create(IUiPluginContext context)
        {
            return new object[] { new Plugin(context) };
        }

        public Type Type { get; } = typeof(Plugin);
    }
}
