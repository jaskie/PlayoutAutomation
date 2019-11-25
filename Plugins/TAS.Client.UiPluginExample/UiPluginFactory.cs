using System;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;

namespace TAS.Client.UiPluginExample
{
    [Export(typeof(IUiPluginFactory))]
    public class UiPluginFactory: IUiPluginFactory
    {
        public object CreateNew(IUiPluginContext context)
        {
            return new UiPlugin(context);
        }

        public Type Type { get; } = typeof(UiPlugin);
    }
}
