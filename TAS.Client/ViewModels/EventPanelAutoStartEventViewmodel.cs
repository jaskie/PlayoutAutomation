using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelAutoStartEventViewmodel:EventPanelRundownElementViewmodelBase
    {
        public enum TAutoStartPlayState
        {
            ScheduledFuture,
            ScheduledPast,
            Playing,
            Played,
            Disabled
        }


        public EventPanelAutoStartEventViewmodel(IEvent ev):base(ev, null) { }

        public string ScheduledDate
        {
            get { return _event.ScheduledTime.ToLocalTime().ToString("d"); }
        }

        public TAutoStartPlayState AutoStartPlayState
        {
            get
            {
                if (!_event.IsEnabled)
                    return TAutoStartPlayState.Disabled;
                if (_event.PlayState == TPlayState.Playing)
                    return TAutoStartPlayState.Playing;
                if (_event.PlayState == TPlayState.Played)
                    return TAutoStartPlayState.Played;
                if (_event.PlayState == TPlayState.Scheduled)
                    if (_engine.CurrentTime > _event.ScheduledTime)
                        return TAutoStartPlayState.ScheduledPast;
                    else
                        return TAutoStartPlayState.ScheduledFuture;
                return TAutoStartPlayState.Disabled;
            }
        }
        

        protected override void NotifyPropertyChanged(string propertyName)
        {
            base.NotifyPropertyChanged(propertyName);
            if (propertyName == "ScheduledTime")
               NotifyPropertyChanged("ScheduledDate");
            if (propertyName == "IsEnabled" || propertyName == "PlayState" || propertyName == "ScheduledTime")
                NotifyPropertyChanged("AutoStartPlayState");
        }

    }
}
