﻿using System.ComponentModel;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.ViewModels
{
    public class EventPanelMovieViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelMovieViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent)
        {
        }

        public override IMedia Media
        {
            get => base.Media;
            protected set
            {
                base.Media = value;
                NotifyPropertyChanged(nameof(MediaErrorInfo));
            }
        }

        public TMediaErrorInfo MediaErrorInfo
        {
            get
            {
                if (Event.PlayState == TPlayState.Played)
                    return TMediaErrorInfo.NoError;
                var media = Media;
                if (media == null || media.MediaStatus != TMediaStatus.Available || !media.FileExists())
                    return TMediaErrorInfo.Missing;
                if (Event.ScheduledTc + Event.Duration > media.TcStart + media.Duration ||
                    Event.ScheduledTc < media.TcStart)
                    return TMediaErrorInfo.TooShort;
                if (media is IPersistentMedia persistentMedia &&
                    persistentMedia.KillDate != default &&
                    persistentMedia.KillDate < Event.ScheduledTime.Date)
                    return TMediaErrorInfo.Expired;
                return TMediaErrorInfo.NoError;
            }
        }

        protected override void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnEventPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(IEvent.Duration):
                case nameof(IEvent.ScheduledTc):
                case nameof(IEvent.ScheduledTime):
                case nameof(IEvent.PlayState):
                    NotifyPropertyChanged(nameof(MediaErrorInfo));
                    break;
            }
        }

        protected override void OnMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnMediaPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(IMedia.MediaStatus):
                case nameof(IMedia.TcStart):
                case nameof(IMedia.Duration):
                case nameof(IPersistentMedia.KillDate):
                    NotifyPropertyChanged(nameof(MediaErrorInfo));
                    break;
            }
        }

        protected override void OnDispose()
        {
            if (IsSelected)
            {
                var p = Prior;
                if (p != null)
                    p.IsSelected = true;
            }
            base.OnDispose();
        }
    }
}
