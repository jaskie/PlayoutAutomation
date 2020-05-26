using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Playout
{
    public class PlayoutRecorderViewModel : OkCancelViewModelBase
    {
        private int _id;
        private string _recorderName;
        private int _defaultChannel;
        private CasparRecorder _casparRecorder;

        public PlayoutRecorderViewModel(CasparRecorder r)
        {
            _casparRecorder = r;
            _id = r.Id;
            _recorderName = r.RecorderName;
        }

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string RecorderName
        {
            get => _recorderName;
            set => SetField(ref _recorderName, value);
        }

        public int DefaultChannel
        {
            get => _defaultChannel;
            set => SetField(ref _defaultChannel, value);
        }

        public CasparRecorder CasparRecorder => _casparRecorder;

        public override bool Ok(object obj = null)
        {
            _casparRecorder.DefaultChannel = DefaultChannel;
            _casparRecorder.Id = Id;
            _casparRecorder.RecorderName = RecorderName;
            return true;
        }

        public void Save()
        {
            Ok();   
        }
    }
}
