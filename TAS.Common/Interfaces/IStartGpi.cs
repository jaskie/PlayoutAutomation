using System;

namespace TAS.Common.Interfaces
{
    public interface IStartGpi: IPlugin
    {        
        event EventHandler Started;
    }
}
