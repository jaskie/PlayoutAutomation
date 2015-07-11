using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
using System.Windows.Threading;
using System.ComponentModel;

namespace TAS.Client.ViewModels
{
    public class PlayingEventViewmodel: ViewmodelBase
    {

        private readonly Engine _engine;

        private Event _playingEvent;

        public PlayingEventViewmodel(Engine engine)
        {
            _engine = engine;
            _engine.EngineOperation += _onEngineOperation;
            _engine.EngineTick += _onEngineTick;
            PlayingEvent = _engine.PlayingEvent();
            _sliderTimer = new DispatcherTimer();
            _sliderTimer.Interval = TimeSpan.FromMilliseconds(200);
            _sliderTimer.Tick += SliderPositionDelayedUpdate;
        }

        protected override void OnDispose()
        {
            _engine.EngineOperation -= _onEngineOperation;
            _engine.EngineTick -= _onEngineTick;
            _sliderTimer.Tick -= SliderPositionDelayedUpdate;
        }

        private void _onEngineOperation(object sender, EngineOperationEventArgs e)
        {
            if (e.Operation == TEngineOperation.Clear)
                PlayingEvent = null;
            else
                if (e.Event != null && e.Event.Layer == VideoLayer.Program
                    && (e.Operation == TEngineOperation.Load || e.Operation == TEngineOperation.Play || e.Operation == TEngineOperation.Start || e.Operation == TEngineOperation.Stop) )
                {
                    PlayingEvent = _engine.PlayingEvent();
                }
        }

        private void _onEngineTick(object sender, EventArgs e)
        {
            var ev = _playingEvent;
            if (ev != null)
            {
                if (!_sliderTimer.IsEnabled)
                    _sliderPosition = ev.Position;
                NotifyPropertyChanged("Position");
                NotifyPropertyChanged("SliderPosition");
            }

        }

        private void _onEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        public Event PlayingEvent
        {
            get { return _playingEvent; }
            set
            {
                lock (this)
                {
                    var pe = _playingEvent;
                    if (value != pe)
                    {
                        if (pe != null)
                            pe.PropertyChanged -= _onEventPropertyChanged;
                        _playingEvent = value;
                        if (value != null)
                            value.PropertyChanged += _onEventPropertyChanged;
                        NotifyPropertyChanged(null);
                    }
                }
            }
        }


        private long _sliderPosition;
        
        public long SliderPosition
        {
            get { return _sliderPosition; }
            set
            {
                Event ev = _playingEvent;
                if (value != _sliderPosition)
                {
                    _sliderPosition = value;
                    _sliderTimer.IsEnabled = false;
                    _sliderTimer.Start();
                }
            }
        }

        private readonly DispatcherTimer _sliderTimer;

        private void SliderPositionDelayedUpdate(object o, EventArgs e)
        {
            Event ev = _playingEvent;
            if (ev == _engine.PlayingEvent())
                _engine.Seek(ev, _sliderPosition);
            _sliderTimer.Stop();
        }

        public TimeSpan Position
        {
            get
            {
                Event ev = _playingEvent;
                return (ev == null) ? TimeSpan.Zero : ev.ScheduledTC + TimeSpan.FromTicks(ev.Position * _engine.FrameTicks);
            }
            set
            {
                Event ev = _playingEvent;
                if (!value.Equals(ev.ScheduledTC + TimeSpan.FromTicks(ev.Position * _engine.FrameTicks)))
                {
                    ev.Position = (value - ev.ScheduledTC).Ticks / _engine.FrameTicks;
                    _engine.Seek(ev, ev.Position);
                }
            }
        }

        public string FileName
        {
            get
            {
                Event ev = _playingEvent;
                if (ev != null)
                {
                    Media m = ev.Media;
                    return (m == null) ? "" : ev.Media.FileName;
                }
                return "";
            }
        }

        public string EventName
        {
            get
            {
                Event ev = _playingEvent;
                return ev == null ? "" : ev.EventName;
            }
        }

        public long LengthInFrames
        {
            get
            {
                Event ev = _playingEvent;
                return ev == null ? 0 : ev.LengthInFrames;
            }
        }

        public string NextEventName
        {
            get
            {
                Event pe = _playingEvent;
                if (pe != null)
                {
                    Event su = pe.Successor;
                    if (su != null)
                        return su.EventName;
                }
                return Properties.Resources._title_RundownEnd;
            }
        }

    }
}
