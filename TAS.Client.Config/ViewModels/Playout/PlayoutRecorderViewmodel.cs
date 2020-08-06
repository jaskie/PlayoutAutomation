using TAS.Client.Common;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Playout
{
    public class PlayoutRecorderViewModel : OkCancelViewModelBase
    {
        private int _id;
        private string _recorderName;
        private int _defaultChannel;
        private IConfigRecorder _casparRecorder;

        public PlayoutRecorderViewModel(IConfigRecorder r)
        {
            _casparRecorder = r;
            _id = r.Id;
            _recorderName = r.RecorderName;
            _defaultChannel = r.DefaultChannel;
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

        public IConfigRecorder CasparRecorder => _casparRecorder;

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
