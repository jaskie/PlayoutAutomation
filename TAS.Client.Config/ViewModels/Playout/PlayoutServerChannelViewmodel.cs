using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Playout
{
    public class PlayoutServerChannelViewmodel : OkCancelViewModelBase
    {
        private string _channelName;
        private int _id;
        private double _masterVolume;
        private string _liveDevice;
        private string _previewUrl;
        private int _audioChannelCount;
        private CasparServerChannel _casparServerChannel;

        public PlayoutServerChannelViewmodel(CasparServerChannel channel)
        {
            _casparServerChannel = channel;
        }

        public CasparServerChannel CasparServerChannel => _casparServerChannel;

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

        public override bool Ok(object obj = null)
        {
            _casparServerChannel.AudioChannelCount = AudioChannelCount;
            _casparServerChannel.ChannelName = ChannelName;
            _casparServerChannel.Id = Id;
            _casparServerChannel.LiveDevice = LiveDevice;
            _casparServerChannel.MasterVolume = MasterVolume;
            _casparServerChannel.PreviewUrl = PreviewUrl;            
            return true;
        }

        public void Save()
        {
            Ok();
        }
    }
}
