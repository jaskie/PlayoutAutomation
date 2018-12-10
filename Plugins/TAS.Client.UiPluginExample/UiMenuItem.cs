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
            var e = Owner.Context.SelectedEvent;
            return e?.EventType == TEventType.Rundown;
        }

        public override void Execute(object parameter)
        {
            Owner.Context.Engine.Start(Owner.Context.SelectedEvent);
        }
    }
}
