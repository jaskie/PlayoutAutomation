using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigCasparServer : IPlayoutServerProperties
    {
        List<IConfigCasparChannel> Channels { get; set; }
        List<IConfigRecorder> Recorders { get; set; }
    }
}
