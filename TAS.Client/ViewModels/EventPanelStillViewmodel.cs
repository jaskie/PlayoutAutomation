using System.ComponentModel;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.ViewModels
{
    public class EventPanelStillViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelStillViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) {}

        public override IMedia Media
        {
            get => base.Media;
            protected set
            {
                base.Media = value;
                NotifyPropertyChanged(nameof(MediaErrorInfo));
            }
        }

        protected override void OnMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnMediaPropertyChanged(sender, e);
            if (e.PropertyName == nameof(IMedia.MediaStatus))
                NotifyPropertyChanged(nameof(MediaErrorInfo));
        }

        public TMediaErrorInfo MediaErrorInfo
        {
            get
            {
                var media = Media;
                if (media == null || media.MediaStatus == TMediaStatus.Deleted || !media.FileExists())
                    return TMediaErrorInfo.Missing;
                return TMediaErrorInfo.NoError;
            }
        }

    }
}
