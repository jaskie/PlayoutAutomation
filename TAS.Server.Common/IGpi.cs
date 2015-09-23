using System;

namespace TAS.Server.Interfaces
{
    public interface IGpi
    {
        int Crawl { get; set; }
        bool CrawlVisible { get; set; }
        int Logo { get; set; }
        int Parental { get; set; }
        event Action Started;
    }
}
