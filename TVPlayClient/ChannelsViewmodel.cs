using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;

namespace TVPlayClient
{
    public class ChannelsViewmodel : ViewModelBase
    {
        private readonly List<ChannelWrapperViewmodel> _channels;

        public ChannelsViewmodel(List<ConfigurationChannel> channels)
        {
            _channels = channels.Select(c => new ChannelWrapperViewmodel(c)).ToList();
            _channels.ForEach(c => c.Initialize());
        }

        public List<ChannelWrapperViewmodel> Channels => _channels;

        protected override void OnDispose()
        {
            _channels?.ForEach(c => c.Dispose());
        }

    }
}
