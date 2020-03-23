using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CGElementsController : ProxyObjectBase, ICGElementsController

    {
        #pragma warning disable CS0649 

        [DtoField(nameof(ICGElementsController.Crawls))]
        private List<CGElement> _crawls;
        [DtoField(nameof(ICGElementsController.Logos))]
        private List<CGElement> _logos;

        [DtoField(nameof(ICGElementsController.Parentals))]
        private List<CGElement> _parentals;

        [DtoField(nameof(ICGElementsController.Crawl))]
        private byte _crawl;

        [DtoField(nameof(ICGElementsController.DefaultCrawl))]
        private byte _defaultCrawl;

        [DtoField(nameof(ICGElementsController.DefaultLogo))]
        private byte _defaultLogo;

        [DtoField(nameof(ICGElementsController.IsCGEnabled))]
        private bool _isCgEnabled;

        [DtoField(nameof(ICGElementsController.IsConnected))]
        private bool _isConnected;

        [DtoField(nameof(ICGElementsController.IsMaster))]
        private bool _isMaster;

        [DtoField(nameof(ICGElementsController.IsWideScreen))]
        private bool _isWideScreen;

        [DtoField(nameof(ICGElementsController.Logo))]
        private byte _logo;

        [DtoField(nameof(ICGElementsController.Parental))]
        private byte _parental;


        #pragma warning restore

        public IEnumerable<ICGElement> Crawls => _crawls;

        public IEnumerable<ICGElement> Logos => _logos;

        public IEnumerable<ICGElement> Parentals => _parentals;

        public byte Crawl { get { return _crawl; } set { Set(value); } }

        public byte DefaultCrawl => _defaultCrawl;

        public byte DefaultLogo => _defaultLogo;

        public bool IsCGEnabled { get { return _isCgEnabled; } set { Set(value); } }

        public bool IsConnected => _isConnected;

        public bool IsMaster => _isMaster;

        public bool IsWideScreen { get { return _isWideScreen; } set { Set(value); } }

        public byte Logo { get { return _logo; } set { Set(value); } }

        public byte Parental { get { return _parental; } set { Set(value); }  }

        public event EventHandler Started;

        public void SetState(ICGElementsState state)
        {
            Invoke(parameters: new object[] { state });
        }

        public void Clear()
        {
            Invoke();
        }

        protected override void OnEventNotification(SocketMessage message) { }

    }
}
