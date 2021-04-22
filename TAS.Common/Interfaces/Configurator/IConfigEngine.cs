using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigEngine : IEnginePersistent
    {       
        List<IStartGpi> Gpis { get; set; }        
        ICGElementsController CGElementsController { get; set; }
        IVideoSwitch Router { get; set; }
        List<IConfigCasparServer> Servers { get; set; }
    }
}
