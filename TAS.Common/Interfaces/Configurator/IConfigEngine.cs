using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigEngine : IEnginePersistent
    {       
        IReadOnlyCollection<IConfigCasparServer> Servers { get; }
    }
}
