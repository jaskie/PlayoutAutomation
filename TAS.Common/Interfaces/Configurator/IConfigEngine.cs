using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigEngine : IEnginePersistent
    {       
        List<IConfigCasparServer> Servers { get; set; }
    }
}
