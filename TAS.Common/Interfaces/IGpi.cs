using System;

namespace TAS.Common.Interfaces
{
    public interface IGpi: IPlugin
    {        
        event EventHandler Started;
    }
}
