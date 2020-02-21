using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class RecordingInfoViewModel : EditViewmodelBase<RecordingInfo>
    {
        private readonly IEngine _engine;

        #region Model
        private int _serverId;
        public int ServerId 
        {
            get => _serverId; 
            set
            {
                if (_serverId == value)
                    return;

                _serverId = value;                
            }
        }
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
                    SelectedRecorderChannel = null;                    
                    SelectedRecorder = null;
                    Recorders = null;
                    return;
                }

                Recorders = _engine?.MediaManager.Recorders.ToArray();                
            }
        }        
        private int _channelId;
        public int ChannelId 
        { 
            get => _channelId;
            set
            {                
                if (_channelId == value)
                    return;
                _channelId = value;
               _selectedRecorderChannel = _selectedRecorder?.Channels.FirstOrDefault(c => c.Id == _channelId);
                NotifyPropertyChanged(nameof(SelectedRecorderChannel));
            }
        }
        private int _recorderId;
        public int RecorderId
        {
            get => _recorderId;
            set
            {                
                if (_recorderId == value)
                    return;
                _recorderId = value;
                _selectedRecorder = Recorders?.FirstOrDefault(r => r.Id == _recorderId && r.ServerId == _serverId);
                NotifyPropertyChanged(nameof(SelectedRecorder));
            }
        }
        #endregion
        
        private IPlayoutServerChannel _selectedRecorderChannel;
        private IRecorder _selectedRecorder;                

        public RecordingInfoViewModel(IEngine engine, RecordingInfo recordingInfo) : base(recordingInfo)
        {
            _engine = engine;
            Recorders = _engine.MediaManager.Recorders.ToArray();
            _selectedRecorder = Recorders.FirstOrDefault(r => r.Id == _recorderId && r.ServerId == _serverId);
            _selectedRecorderChannel = _selectedRecorder?.Channels.FirstOrDefault(c=> c.Id == _channelId);
        }

        public void Save()
        {
            Update();
        }

        public void UndoEdit()
        {
            Load();
        }

        protected override void OnDispose()
        {
            //
        }

        public IRecorder[] Recorders { get; private set; }        
        public IRecorder SelectedRecorder
        {
            get => _selectedRecorder;
            set
            {
                if (!SetField(ref _selectedRecorder, value))
                    return;

                if (value == null)
                {
                    _recorderId = default(int);
                    _serverId = default(int);
                    return;
                }

                _recorderId = value.Id;
                _serverId = _selectedRecorder.ServerId;                                              
            }
        }       
        public IPlayoutServerChannel SelectedRecorderChannel
        {
            get => _selectedRecorderChannel;
            set
            {
                if (!SetField(ref _selectedRecorderChannel, value))
                    return;

                if (value == null)
                {
                    _channelId = default(int);
                    return;
                }

                _channelId = _selectedRecorderChannel.Id;                
            }
        }                 
    }
}
