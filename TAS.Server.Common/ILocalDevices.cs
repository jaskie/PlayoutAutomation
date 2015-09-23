using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ILocalDevices: IInitializable
    {
        IGpi Select(UInt64 engineId);
    }
}
