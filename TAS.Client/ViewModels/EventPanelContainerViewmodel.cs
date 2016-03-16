using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelContainerViewmodel: EventPanelViewmodelBase
    {
        public EventPanelContainerViewmodel(IEvent ev, EventPanelViewmodelBase parent): base(ev, parent) {
            if (ev.EventType != TEventType.Container)
                throw new ApplicationException(string.Format("Invalid panel type:{0} for event type:{1}", this.GetType(), ev.EventType));
        }

        public UICommand CommandHide { get; private set; }
        public UICommand CommandShow { get; private set; }
        protected override void _createCommands()
        {
            CommandHide = new UICommand()
            {
                ExecuteDelegate = o =>
                {
                    _event.IsEnabled = false;
                    _event.Save();
                },
                CanExecuteDelegate = o => _event.IsEnabled == true
            };
            CommandShow = new UICommand()
            {
                ExecuteDelegate = o =>
                {
                    _event.IsEnabled = true;
                    _event.Save();
                },
                CanExecuteDelegate = o => _event.IsEnabled == false
            };
        }

        public bool IsVisible
        {
            get { return (_event == null) ? true : _event.IsEnabled || _event.EventType != TEventType.Container; }
        }

    }
}
