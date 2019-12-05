using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;

namespace TVPlayClient
{
    public class ChannelsViewmodel : ViewModelBase
    {
        public ChannelsViewmodel(IEnumerable<ChannelConfiguration> channels)
        {
            Channels = channels.Select(c => new ChannelWrapperViewmodel(c)).ToList();
            Channels.ForEach(c => c.Initialize());
        }

        public List<ChannelWrapperViewmodel> Channels { get; }

        protected override void OnDispose()
        {
            Channels?.ForEach(c => c.Dispose());
        }

    }
}
