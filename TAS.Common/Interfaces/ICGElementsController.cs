using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface ICGElementsController: ICGElementsState, INotifyPropertyChanged, IGpi, IDisposable
    {
        IEnumerable<ICGElement> Crawls { get; }
        IEnumerable<ICGElement> Logos { get; }
        IEnumerable<ICGElement> Parentals { get; }
        IEnumerable<ICGElement> Auxes { get; }
        void SetState(ICGElementsState state);
        byte[] VisibleAuxes { get; }
        byte DefaultCrawl { get; }
        bool IsMaster { get; }
        void ShowAux(int auxNr);
        void HideAux(int auxNr);
        bool IsConnected { get; }
    }
}
