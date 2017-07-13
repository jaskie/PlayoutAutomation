using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Server.Common.Interfaces
{
    /// <summary>
    /// Access Control Object - base interface for User, Group and Role
    /// </summary>
    public interface IAco
    {
        TAco AcoType { get; }
        string Name { get; set; }
    }
}
