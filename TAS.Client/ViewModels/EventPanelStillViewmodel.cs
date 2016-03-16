using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelStillViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelStillViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) { }

        public string MediaFileName
        {
            get
            {
                if (_event == null)
                    return string.Empty;
                IMedia media = _event.ServerMediaPRI;
                return (media == null) ? ((_event.EventType == TEventType.Movie || _event.EventType == TEventType.StillImage) ? _event.MediaGuid.ToString() : string.Empty) : media.FileName;
            }
        }

    }
}
