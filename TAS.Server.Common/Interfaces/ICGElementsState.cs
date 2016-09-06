using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ICGElementsState
    {
        bool IsEnabled { get; set; }
        byte Crawl { get; set; } // 0 - none
        byte Logo { get; set; }
        byte Parental { get; set; }
    }
}
