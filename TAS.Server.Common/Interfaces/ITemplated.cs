using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ITemplated
    {
        Dictionary<string, string> Fields { get; set; }
    }
}
