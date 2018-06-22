using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class PlayoutServerChannelViewmodel:EditViewmodelBase<CasparServerChannel>
    {
        private string _channelName;
        private int _id;
        private double _masterVolume;
        private string _liveDevice;
        private string _previewUrl;

        public PlayoutServerChannelViewmodel(CasparServerChannel channel): base(channel)
        {
        }

        public string ChannelName
        {
            get => _channelName;
            set => SetField(ref _channelName, value);
        }

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public double MasterVolume
        {
            get => _masterVolume;
            set => SetField(ref _masterVolume, value);
        }

        public string LiveDevice
        {
            get => _liveDevice;
            set => SetField(ref _liveDevice, value);
        }

        public string PreviewUrl
        {
            get => _previewUrl;
            set => SetField(ref _previewUrl, value);
        }

        protected override void OnDispose() { }

        public void Save()
        {
            Update(Model);
        }
    }
}
