using TAS.Common;
using TAS.Common.Interfaces;

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

        public string ScheduledDate => Event.ScheduledTime.ToLocalTime().ToString("d");

        public TAutoStartPlayState AutoStartPlayState
        {
            get
            {
                if (!Event.IsEnabled)
                    return TAutoStartPlayState.Disabled;
                if (Event.PlayState == TPlayState.Playing)
                    return TAutoStartPlayState.Playing;
                if (Event.PlayState == TPlayState.Played)
                    return TAutoStartPlayState.Played;
                if (Event.PlayState == TPlayState.Scheduled)
                    if (Engine.CurrentTime < Event.ScheduledTime || (Event.AutoStartFlags & AutoStartFlags.Daily )!= AutoStartFlags.None)
                        return TAutoStartPlayState.ScheduledFuture;
                else
                return TAutoStartPlayState.ScheduledPast;
                return TAutoStartPlayState.Disabled;
            }
        }
        
        protected override void NotifyPropertyChanged(string propertyName)
        {
            base.NotifyPropertyChanged(propertyName);
            if (propertyName == nameof(ScheduledTime))
               NotifyPropertyChanged(nameof(ScheduledDate));
            if (propertyName == nameof(IsEnabled) || propertyName == nameof(PlayState) || propertyName == nameof(ScheduledTime))
                NotifyPropertyChanged(nameof(AutoStartPlayState));
        }

    }
}
