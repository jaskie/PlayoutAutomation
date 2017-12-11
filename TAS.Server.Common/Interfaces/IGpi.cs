using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IGpi
    {
        bool IsWideScreen { get; set; }
        event EventHandler Started;
    }
}
