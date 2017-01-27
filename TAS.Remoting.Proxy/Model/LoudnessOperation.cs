using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class LoudnessOperation : FileOperation, ILoudnessOperation
    {
        EventHandler<AudioVolumeEventArgs> _audioVolumeMeasured;
        public event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured
        {
            add
            {
                EventAdd(_audioVolumeMeasured);
                _audioVolumeMeasured += value;
            }

            remove
            {
                _audioVolumeMeasured -= value;
                EventRemove(_audioVolumeMeasured);
            }
        }
        protected override void OnEventNotification(WebSocketMessage e)
        {
            if (e.MemberName == nameof(AudioVolumeMeasured))
                _audioVolumeMeasured?.Invoke(this, ConvertEventArgs<AudioVolumeEventArgs>(e));
            else
                base.OnEventNotification(e);
        }
        
        public TimeSpan MeasureDuration { get { return Get<TimeSpan>(); }  set { Set(value); } }
        public TimeSpan MeasureStart { get { return Get<TimeSpan>(); } set { Set(value); } }
    }
}
