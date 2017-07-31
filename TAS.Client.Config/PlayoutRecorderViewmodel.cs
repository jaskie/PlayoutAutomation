using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class PlayoutRecorderViewmodel: EditViewmodelBase<CasparRecorder>
    {
        private int _id;
        private string _recorderName;

        public PlayoutRecorderViewmodel(CasparRecorder r): base(r)
        {
            _id = r.Id;
            _recorderName = r.RecorderName;
        }

        public int Id { get { return _id; } set { SetField(ref _id, value); } }

        public string RecorderName { get { return _recorderName; } set { SetField(ref _recorderName, value); } }

        protected override void OnDispose()
        {
            
        }
    }
}
