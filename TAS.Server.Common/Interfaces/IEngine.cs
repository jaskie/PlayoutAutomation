using System;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IEngine
    {
        TAspectRatioControl AspectRatioControl { get; set; }
        decimal AudioVolume { get; set; }
        string EngineName { get; set; }
        ulong Id { get; set; }
        int TimeCorrection { get; set; }
        TVideoFormat VideoFormat { get; set; }
    }
}
