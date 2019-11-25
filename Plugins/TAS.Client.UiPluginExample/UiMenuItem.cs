using System.Collections.Generic;
using TAS.Client.Common.Plugin;
using TAS.Common;

namespace TAS.Client.UiPluginExample
{
    public class UiMenuItem: UiMenuItemBase
    {
        public UiMenuItem(IUiPlugin owner) : base(owner)
        {
            Items = new List<IUiMenuItem>();
        }

        public override bool CanExecute(object parameter)
        {
            if (!(Owner.Context is IUiEngine engine))
                return false;
            var e = engine.SelectedEvent;
            return e?.EventType == TEventType.Rundown;
        }

        public override void Execute(object parameter)
        {
            if (!(Owner.Context is IUiEngine engine))
                return;
            engine.Engine.Start(engine.SelectedEvent);
        }
    }
}
