using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Server.Interfaces;

namespace TAS.Client.Config
{
    public class PlayoutServerChannelViewmodel:EditViewmodelBase<IPlayoutServerChannel>
    {
        private string _channelName;
        private int _channelNumber;
        private decimal _masterVolume;
        private string _liveDevice;
        protected override void OnDispose() { }
        public PlayoutServerChannelViewmodel(IPlayoutServerChannel channel): base(channel, new PlayoutServerChannelView())
        {

        }
        public string ChannelName { get { return _channelName; } set { SetField(ref _channelName, value, "ChannelName"); } }
        public int ChannelNumber { get { return _channelNumber; } set { SetField(ref _channelNumber, value, "ChannelNumber"); } }
        public decimal MasterVolume { get { return _masterVolume; } set { SetField(ref _masterVolume, value, "MasterVolume"); } }
        public string LiveDevice { get { return _liveDevice; } set { SetField(ref _liveDevice, value, "LiveDevice"); } }
    }
}
