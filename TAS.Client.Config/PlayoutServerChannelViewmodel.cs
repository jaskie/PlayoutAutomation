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

        public string ChannelName { get { return _channelName; } set { SetField(ref _channelName, value); } }

        public int Id { get { return _id; } set { SetField(ref _id, value); } }

        public double MasterVolume { get { return _masterVolume; } set { SetField(ref _masterVolume, value); } }

        public string LiveDevice { get { return _liveDevice; } set { SetField(ref _liveDevice, value); } }

        public string PreviewUrl { get { return _previewUrl; } set { SetField(ref _previewUrl, value); } }

        protected override void OnDispose() { }

    }
}
