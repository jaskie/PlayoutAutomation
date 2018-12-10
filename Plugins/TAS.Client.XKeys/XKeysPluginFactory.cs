using System;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;

namespace TAS.Client.XKeys
{
    [Export(typeof(IUiPluginFactory))]
    public class XKeysPluginFactory: IUiPluginFactory
    {
        public object CreateNew(IUiPluginContext context)
        {
            return new XKeysPlugin(context);
        }

        public Type Type { get; } = typeof(XKeysPlugin);
    }
}
