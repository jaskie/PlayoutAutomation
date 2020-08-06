using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common.Interfaces;

namespace TAS.Server.CgElementsControllerTests.Model
{
    public class MockCgElementsController : ICGElementsController
    {
        private IEnumerable<ICGElement> _crawls = new List<ICGElement>();
        private IEnumerable<ICGElement> _parentals = new List<ICGElement>();
        private IEnumerable<ICGElement> _auxes = new List<ICGElement>();
        private IEnumerable<ICGElement> _logos = new List<ICGElement>();
        private byte _deafultCrawl;
        private byte _deafultLogo;
        private bool _isMaster;

        public IEnumerable<ICGElement> Crawls => _crawls;

        public IEnumerable<ICGElement> Logos => _logos;

        public IEnumerable<ICGElement> Parentals => _parentals;

        public IEnumerable<ICGElement> Auxes => _auxes;

        public byte DefaultCrawl => _deafultCrawl;

        public byte DefaultLogo => _deafultLogo;

        public bool IsMaster => _isMaster;

        public bool IsConnected => throw new NotImplementedException();

        public bool IsCGEnabled { get; set; }
        public byte Crawl { get; set; }
        public byte Logo { get; set; }
        public byte Parental { get; set; }
        public bool IsWideScreen { get; set; }
        public bool IsEnabled { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Started;

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public void SetState(ICGElementsState state)
        {
            throw new NotImplementedException();
        }
    }
}
