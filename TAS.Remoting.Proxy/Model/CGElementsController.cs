using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CgElementsController : ProxyObjectBase, ICGElementsController

    {
#pragma warning disable CS0649
        [DtoMember(nameof(ICGElementsController.IsEnabled))]
        private bool _isEnabled;

        [DtoMember(nameof(ICGElementsController.Crawls))]
        private List<CGElement> _crawls;

        [DtoMember(nameof(ICGElementsController.Logos))]
        private List<CGElement> _logos;

        [DtoMember(nameof(ICGElementsController.Auxes))]
        private List<CGElement> _auxes;

        [DtoMember(nameof(ICGElementsController.Parentals))]
        private List<CGElement> _parentals;

        [DtoMember(nameof(ICGElementsController.Crawl))]
        private byte _crawl;

        [DtoMember(nameof(ICGElementsController.DefaultCrawl))]
        private byte _defaultCrawl;

        [DtoMember(nameof(ICGElementsController.DefaultLogo))]
        private byte _defaultLogo;

        [DtoMember(nameof(ICGElementsController.IsCGEnabled))]
        private bool _isCgEnabled;

        [DtoMember(nameof(ICGElementsController.IsConnected))]
        private bool _isConnected;

        [DtoMember(nameof(ICGElementsController.IsMaster))]
        private bool _isMaster;

        [DtoMember(nameof(ICGElementsController.Logo))]
        private byte _logo;

        [DtoMember(nameof(ICGElementsController.Parental))]
        private byte _parental;
        [DtoMember(nameof(ICGElementsController.IsWideScreen))]
        private bool _isWideScreen;


#pragma warning restore
        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(value);
        }

        public IEnumerable<ICGElement> Crawls => _crawls;

        public IEnumerable<ICGElement> Logos => _logos;
        public IEnumerable<ICGElement> Auxes => _auxes;

        public IEnumerable<ICGElement> Parentals => _parentals;

        public byte Crawl { get { return _crawl; } set { Set(value); } }

        public byte DefaultCrawl => _defaultCrawl;

        public byte DefaultLogo => _defaultLogo;

        public bool IsCGEnabled { get { return _isCgEnabled; } set { Set(value); } }

        public bool IsConnected => _isConnected;

        public bool IsMaster => _isMaster;

        public byte Logo { get { return _logo; } set { Set(value); } }

        public byte Parental { get { return _parental; } set { Set(value); } }

        public bool IsWideScreen { get => _isWideScreen; set => Set(value); } 

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
