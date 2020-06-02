using System;

namespace TAS.Common.Interfaces
{
    public interface IEnginePlugin : IDisposable
    {
        string EngineName { get; }
        bool IsEnabled { get; set; }
        void Initialize();
    }
}
