using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElementsController : ProxyBase, ICGElementsController

    {
        #pragma warning disable CS0649 

        [JsonProperty(nameof(ICGElementsController.Crawls))]
        private List<CGElement> _crawls;
        [JsonProperty(nameof(ICGElementsController.Logos))]
        private List<CGElement> _logos;

        [JsonProperty(nameof(ICGElementsController.Parentals))]
        private List<CGElement> _parentals;

        [JsonProperty(nameof(ICGElementsController.Auxes))]
        private List<CGElement> _auxes;

        [JsonProperty(nameof(ICGElementsController.Crawl))]
        private byte _crawl;

        [JsonProperty(nameof(ICGElementsController.DefaultCrawl))]
        private byte _defaultCrawl;

        [JsonProperty(nameof(ICGElementsController.IsCGEnabled))]
        private bool _isCgEnabled;

        [JsonProperty(nameof(ICGElementsController.IsConnected))]
        private bool _isConnected;

        [JsonProperty(nameof(ICGElementsController.IsMaster))]
        private bool _isMaster;

        [JsonProperty(nameof(ICGElementsController.IsWideScreen))]
        private bool _isWideScreen;

        [JsonProperty(nameof(ICGElementsController.Logo))]
        private byte _logo;

        [JsonProperty(nameof(ICGElementsController.Parental))]
        private byte _parental;

        [JsonProperty(nameof(ICGElementsController.VisibleAuxes))]
        private byte[] _visibleAuxes;

        #pragma warning restore

        public IEnumerable<ICGElement> Crawls => _crawls;

        public IEnumerable<ICGElement> Logos => _logos;

        public IEnumerable<ICGElement> Parentals => _parentals;

        public IEnumerable<ICGElement> Auxes => _auxes;

        public byte Crawl { get { return _crawl; } set { Set(value); } }

        public byte DefaultCrawl => _defaultCrawl;

        public bool IsCGEnabled { get { return _isCgEnabled; } set { Set(value); } }

        public bool IsConnected => _isConnected;

        public bool IsMaster => _isMaster;

        public bool IsWideScreen { get { return _isWideScreen; } set { Set(value); } }

        public byte Logo { get { return _logo; } set { Set(value); } }

        public byte Parental { get { return _parental; } set { Set(value); }  }

        public byte[] VisibleAuxes => _visibleAuxes;

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

        protected override void OnEventNotification(WebSocketMessage message) { }

    }
}
