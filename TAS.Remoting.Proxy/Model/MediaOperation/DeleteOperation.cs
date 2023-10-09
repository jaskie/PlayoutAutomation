using System;
using jNet.RPC;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model.MediaOperation
{
    public class DeleteOperation : FileOperationBase, IDeleteOperation
    {
        #pragma warning disable CS0649, CS0169

        [DtoMember(nameof(ILoudnessOperation.MeasureDuration))]
        private TimeSpan _measureDuration;

        [DtoMember(nameof(ILoudnessOperation.MeasureStart))]
        private TimeSpan _measureStart;

        [DtoMember(nameof(ILoudnessOperation.Source))]
        private MediaBase _source;

        #pragma warning restore

        private event EventHandler<AudioVolumeEventArgs> _audioVolumeMeasured;
        
        public IMedia Source { get => _source; set => Set(value); }

        public TimeSpan MeasureDuration { get => _measureDuration; set => Set(value); }

        public TimeSpan MeasureStart { get => _measureStart; set => Set(value); }

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

        protected override void OnEventNotification(SocketMessage message)
        {
            if (message.MemberName == nameof(AudioVolumeMeasured))
                _audioVolumeMeasured?.Invoke(this, DeserializeEventArgs<AudioVolumeEventArgs>(message));
            else
                base.OnEventNotification(message);
        }
    }
}
