using System.Collections.Generic;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigArchiveDirectories
    {
        List<IConfigArchiveDirectory> Directories { get; }
    }
}
