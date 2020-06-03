using System;

namespace TAS.Common.Interfaces
{
    public interface IEnginePlugin : IDisposable
    {        
        bool IsEnabled { get; set; }              
    }
}
