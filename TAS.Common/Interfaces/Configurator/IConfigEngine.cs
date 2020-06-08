using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigEngine : IEnginePersistent
    {       
        ICGElementsController CGElementsController { get; set; }
        List<IConfigCasparServer> Servers { get; set; }
    }
}
