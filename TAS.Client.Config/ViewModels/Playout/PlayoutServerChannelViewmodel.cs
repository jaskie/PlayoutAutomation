using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Playout
{
    public class PlayoutServerChannelViewmodel : EditViewmodelBase<CasparServerChannel>, IPlayoutServerChannelProperties
    {
        private string _channelName;
        private int _id;
        private double _masterVolume;
        private string _liveDevice;
        private string _previewUrl;
        private int _audioChannelCount;

        public PlayoutServerChannelViewmodel(CasparServerChannel channel) : base(channel)
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

        public int AudioChannelCount
        {
            get => _audioChannelCount;
            set => SetField(ref _audioChannelCount, value);
        }

        protected override void OnDispose() { }

        public void Save()
        {
            Update(Model);
        }
    }
}
