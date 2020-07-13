using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigEngine : IEnginePersistent
    {       
        IGpi Gpi { get; set; }
        List<IPlugin> Plugins { get; set; }
        ICGElementsController CGElementsController { get; set; }
        IRouter Router { get; set; }
        List<IConfigCasparServer> Servers { get; set; }
    }
}
