using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigEngine : IEnginePersistent
    {       
        List<IGpi> Gpis { get; set; }        
        ICGElementsController CGElementsController { get; set; }
        IRouter Router { get; set; }
        List<IConfigCasparServer> Servers { get; set; }
    }
}
