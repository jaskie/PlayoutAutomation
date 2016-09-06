using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ICGElement
    {
        byte Id { get; }
        string Name { get; }
    }
}
