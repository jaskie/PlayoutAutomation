using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class PlayoutServerChannelViewmodel:EditViewmodelBase<CasparServerChannel>
    {
        private string _channelName;
        private int _id;
        private decimal _masterVolume;
        private string _liveDevice;
        private string _previewUrl;

        protected override void OnDispose() { }
        public PlayoutServerChannelViewmodel(CasparServerChannel channel): base(channel, new PlayoutServerChannelView())
        {
        }
        public string ChannelName { get { return _channelName; } set { SetField(ref _channelName, value); } }
        public int Id { get { return _id; } set { SetField(ref _id, value); } }
        public decimal MasterVolume { get { return _masterVolume; } set { SetField(ref _masterVolume, value); } }
        public string LiveDevice { get { return _liveDevice; } set { SetField(ref _liveDevice, value); } }
        public string PreviewUrl { get { return _previewUrl; } set { SetField(ref _previewUrl, value); } }

    }
}
