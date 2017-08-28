using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElementsController : ProxyBase, ICGElementsController

    {
        [JsonProperty(nameof(ICGElementsController.Crawls))]
        private List<CGElement> _crawls { get { return Get<List<CGElement>>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<ICGElement> Crawls => _crawls;

        [JsonProperty(nameof(ICGElementsController.Logos))]
        private List<CGElement> _logos { get { return Get<List<CGElement>>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<ICGElement> Logos => _logos;

        [JsonProperty(nameof(ICGElementsController.Parentals))]
        private List<CGElement> _parentals { get { return Get<List<CGElement>>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<ICGElement> Parentals => _parentals;

        [JsonProperty(nameof(ICGElementsController.Auxes))]
        private List<CGElement> _auxes { get { return Get<List<CGElement>>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<ICGElement> Auxes => _auxes;

        public byte Crawl { get { return Get<byte>(); } set { Set(value); } }
        
        public byte DefaultCrawl { get { return Get<byte>(); } set { SetLocalValue(value); } }

        public bool IsCGEnabled { get { return Get<bool>(); } set { Set(value); } }

        public bool IsConnected { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public bool IsMaster { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public bool IsWideScreen { get { return Get<bool>(); } set { Set(value); } }

        public byte Logo { get { return Get<byte>(); } set { Set(value); } }
        public byte Parental { get { return Get<byte>(); } set { Set(value); }  }

        public byte[] VisibleAuxes { get { return Get<byte[]>(); } set { SetLocalValue(value); } }

        public event EventHandler Started;

        public void HideAux(int auxNr)
        {
            Invoke(parameters: new[] { auxNr });
        }

        public void SetState(ICGElementsState state)
        {
            Invoke(parameters: new object[] { state });
        }

        public void ShowAux(int auxNr)
        {
            Invoke(parameters: new[] { auxNr });
        }

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
