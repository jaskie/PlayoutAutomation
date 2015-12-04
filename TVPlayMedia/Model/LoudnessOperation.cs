using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    class LoudnessOperation : FileOperation, ILoudnessOperation
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
        protected override void OnEventNotification(WebSocketMessageEventArgs e)
        {
            if (e.Message.MemberName == "AudioVolumeMeasured")
            {
                var h = _audioVolumeMeasured;
                if (h != null)
                    h(this, ConvertEventArgs<AudioVolumeEventArgs>(e));
            }
            else
                base.OnEventNotification(e);
        }
        
        public TimeSpan MeasureDuration { get { return Get<TimeSpan>(); }  set { Set(value); } }
        public TimeSpan MeasureStart { get { return Get<TimeSpan>(); } set { Set(value); } }
    }
}
