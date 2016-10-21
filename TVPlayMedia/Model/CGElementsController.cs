using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class CGElementsController : ProxyBase, ICGElementsController

    {
        public IEnumerable<ICGElement> Auxes { get { return Get<List<CGElement>>(); } }

        public byte Crawl { get { return Get<byte>(); } set { Set(value); } }

        public IEnumerable<ICGElement> Crawls { get { return Get<List<CGElement>>(); } }

        public byte DefaultCrawl { get { return Get<byte>(); } }

        public bool IsCGEnabled { get { return Get<bool>(); } set { Set(value); } }

        public bool IsConnected { get { return Get<bool>(); } }

        public bool IsMaster { get { return Get<bool>(); } }

        public bool IsWideScreen { get { return Get<bool>(); } set { Set(value); } }

        public byte Logo { get { return Get<byte>(); } set { Set(value); } }

        public IEnumerable<ICGElement> Logos { get { return Get<List<CGElement>>(); } }

        public byte Parental { get { return Get<byte>(); } set { Set(value); } }

        public IEnumerable<ICGElement> Parentals { get { return Get<List<CGElement>>(); } }
     
        public byte[] VisibleAuxes { get { return Get<byte[]>(); } }

        public event EventHandler Started;

        public void Dispose()
        {
            
        }

        public void HideAux(int auxNr)
        {
            Invoke(parameters: new[] { auxNr });
        }

        public void SetState(ICGElementsState state)
        {
            Invoke(parameters: new[] { state });
        }

        public void ShowAux(int auxNr)
        {
            Invoke(parameters: new[] { auxNr });
        }
    }
}
