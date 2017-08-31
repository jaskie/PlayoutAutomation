using System;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class LoudnessOperation : FileOperation, ILoudnessOperation
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(ILoudnessOperation.MeasureDuration))]
        private TimeSpan _measureDuration;

        [JsonProperty(nameof(ILoudnessOperation.MeasureStart))]
        private TimeSpan _measureStart;

        #pragma warning restore

        private event EventHandler<AudioVolumeEventArgs> _audioVolumeMeasured;

        public TimeSpan MeasureDuration { get { return Get<TimeSpan>(); } set { Set(value); } }
        public TimeSpan MeasureStart { get { return Get<TimeSpan>(); } set { Set(value); } }

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
    }
}
