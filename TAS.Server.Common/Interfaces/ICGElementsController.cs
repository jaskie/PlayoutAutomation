using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Server.Interfaces
{
    public interface ICGElementsController: ICGElementsState, ICGElementsConfig, INotifyPropertyChanged, IGpi, IDisposable
    {
        IEnumerable<ICGElement> Crawls { get; }
        IEnumerable<ICGElement> Logos { get; }
        IEnumerable<ICGElement> Parentals { get; }
        void SetState(ICGElementsState state);
        int[] VisibleAuxes { get; }
        bool IsMaster { get; }
        void ShowAux(int auxNr);
        void HideAux(int auxNr);

        bool IsConnected { get; }
    }
}
