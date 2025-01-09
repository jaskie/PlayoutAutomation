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

        [DtoMember(nameof(ICGElementsController.Crawls))]
        private ICGElement[] _crawls;
        [DtoMember(nameof(ICGElementsController.Logos))]
        private ICGElement[] _logos;

        [DtoMember(nameof(ICGElementsController.Parentals))]
        private ICGElement[] _parentals;

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

        [DtoMember(nameof(ICGElementsController.IsWideScreen))]
        private bool _isWideScreen;

        [DtoMember(nameof(ICGElementsController.Logo))]
        private byte _logo;

        [DtoMember(nameof(ICGElementsController.Parental))]
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

#pragma warning disable CS0067
        public event EventHandler Started;
#pragma warning restore

        public void SetState(ICGElementsState state)
        {
            Invoke(parameters: new object[] { state });
        }

        public void Clear()
        {
            Invoke();
        }

    }
}
