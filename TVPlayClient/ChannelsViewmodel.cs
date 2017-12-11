using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.ViewModels;

namespace TVPlayClient
{
    public class ChannelsViewmodel : ViewmodelBase
    {
        private readonly List<ChannelWrapperViewmodel> _channels;

        public ChannelsViewmodel(List<ChannelWrapperViewmodel> channels)
        {
            _channels = channels;
            channels.ForEach(c => c.Initialize());
        }

        protected override void OnDispose()
        {
            _channels?.ForEach(c => c.Dispose());
        }

        public List<ChannelWrapperViewmodel> Channels { get { return _channels; }  }


    }
}
