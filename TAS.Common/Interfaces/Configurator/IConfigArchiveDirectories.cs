using System.Collections.Generic;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigArchiveDirectories
    {
        List<IConfigArchiveDirectory> Directories { get; }
    }
}
