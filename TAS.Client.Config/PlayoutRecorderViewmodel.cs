using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;

namespace TAS.Client.Config
{
    public class PlayoutRecorderViewmodel: EditViewmodelBase<CasparRecorder>, IRecorderProperties
    {
        private int _id;
        private string _recorderName;
        private int _defaultChannel;
        private int _serverId;        

        public PlayoutRecorderViewmodel(CasparRecorder r): base(r)
        {
            _serverId = r.ServerId;
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

        public int ServerId
        {
            get => _serverId;
            set => SetField(ref _serverId, value);
        }

        protected override void OnDispose()
        {
            
        }

        public void Save()
        {
            Update(Model);
        }
    }
}
