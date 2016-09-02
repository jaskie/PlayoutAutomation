using System;

namespace TAS.Server.Interfaces
{
    public interface IGpi
    {
        int Crawl { get; set; }
        bool CrawlVisible { get; set; }
        int Logo { get; set; }
        int Parental { get; set; }
        int[] VisibleAuxes { get; }
        bool IsMaster { get; }
        void ShowAux(int auxNr);
        void HideAux(int auxNr);
        event Action Started;
    }
}
