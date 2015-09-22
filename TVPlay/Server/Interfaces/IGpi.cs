using System;

namespace TAS.Server
{
    interface IGpi
    {
        int Crawl { get; set; }
        bool CrawlVisible { get; set; }
        int Logo { get; set; }
        int Parental { get; set; }
        event Action Started;
    }
}
