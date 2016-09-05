using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ICGElementsState
    {
        bool IsEnabled { get; set; }
        byte Crawl { get; set; }
        bool CrawlVisible { get; set; }
        byte Logo { get; set; }
        bool LogoVisible { get; set; }
        byte Parental { get; set; }
        bool ParentalVisible { get; set; }
    }
}
