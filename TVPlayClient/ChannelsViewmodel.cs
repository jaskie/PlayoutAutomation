using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;

namespace TVPlayClient
{
    public class ChannelsViewModel : ViewModelBase
    {
        public ChannelsViewModel(IEnumerable<ChannelConfiguration> channels)
        {
            Channels = channels.Select(c => new ChannelWrapperViewModel(c)).ToList();
            Channels.ForEach(c => c.Initialize());
        }

        public List<ChannelWrapperViewModel> Channels { get; }

        protected override void OnDispose()
        {
            Channels?.ForEach(c => c.Dispose());
        }

    }
}
