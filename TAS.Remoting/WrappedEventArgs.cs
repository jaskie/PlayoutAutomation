using System;

namespace TAS.Remoting
{
    public class WrappedEventArgs: EventArgs
    {
        public IDto Dto { get; }
        public EventArgs Args { get; }

        public WrappedEventArgs(IDto dto, EventArgs args)
        {
            Dto = dto;
            Args = args;
        }
    }
}
