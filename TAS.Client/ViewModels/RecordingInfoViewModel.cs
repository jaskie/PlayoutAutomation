using System;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class RecordingInfoViewModel : ModifyableViewModelBase
    {
        private readonly IEngine _engine;
        private RecordingInfo _recordingInfo;
        private IPlayoutServerChannel _selectedRecorderChannel;
        private IRecorder _selectedRecorder;
        private bool _isRecordingScheduled;

        public bool IsRecordingScheduled
        {
            get => _isRecordingScheduled;
            set
            {
                if (!SetField(ref _isRecordingScheduled, value))
                    return;

                if (!value)
                {
                    _selectedRecorder = null;
                    _selectedRecorderChannel = null;
                }
                else
                {
                    _selectedRecorder = Recorders.FirstOrDefault();
                    _selectedRecorderChannel = SelectedRecorder.Channels.FirstOrDefault(c => c.Id == _selectedRecorder.DefaultChannel);
                }
                NotifyPropertyChanged(nameof(Recorders));
                NotifyPropertyChanged(nameof(SelectedRecorder));
                NotifyPropertyChanged(nameof(SelectedRecorderChannel));
            }
        }    
  
        

        public RecordingInfoViewModel(IEngine engine, RecordingInfo recordingInfo)
        {
            _recordingInfo = recordingInfo;
            _engine = engine;
            Recorders = _engine.MediaManager.Recorders.ToArray();
            Load();
        }

        public void Load()
        {
            _isRecordingScheduled = _recordingInfo != null;
            _selectedRecorder = _isRecordingScheduled
                ? Recorders.FirstOrDefault(r => r.Id == _recordingInfo.RecorderId && r.ServerId == _recordingInfo.ServerId)
                : null;
            _selectedRecorderChannel = _isRecordingScheduled
                ? SelectedRecorder.Channels.FirstOrDefault(c => c.Id == _recordingInfo.ChannelId)
                : null;
            NotifyPropertyChanged(nameof(IsRecordingScheduled));
            NotifyPropertyChanged(nameof(SelectedRecorder));
            NotifyPropertyChanged(nameof(SelectedRecorderChannel));
            IsModified = false;
        }

        protected override void OnDispose() { }

        public IRecorder[] Recorders { get; }        

        public IRecorder SelectedRecorder
        {
            get => _selectedRecorder;
            set => SetField(ref _selectedRecorder, value);

        }
        public IPlayoutServerChannel SelectedRecorderChannel
        {
            get => _selectedRecorderChannel;
            set => SetField(ref _selectedRecorderChannel, value);
        }

        internal void UpdateInfo(RecordingInfo recordingInfo)
        {
            OnUiThread(() =>
           {
               _recordingInfo = recordingInfo;
               Load();
           });
        }

        internal RecordingInfo GetRecordingInfo()
        {
            if (!IsRecordingScheduled)
                return null;
            if (!IsModified)
                return _recordingInfo;
            return new RecordingInfo
            {
                ChannelId = SelectedRecorderChannel.Id,
                RecorderId = SelectedRecorder.Id,
                ServerId = SelectedRecorder.ServerId
            };
                 
        }
    }
}
