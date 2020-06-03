using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common.Interfaces;

namespace TAS.Server.CgElementsController.Configurator.Model
{
    public class CgElementsController : ICGElementsController
    {
        public string EngineName { get; set; }
        public List<string> Startup { get; set; }
        public IEnumerable<ICGElement> Logos { get; set; }
        public IEnumerable<ICGElement> Parentals { get; set; }
        public IEnumerable<ICGElement> Crawls { get; set; }
        public IEnumerable<ICGElement> Auxes { get; set; }
        public bool IsEnabled { get; set; }

        #region ICGElementsController
        public byte DefaultCrawl => throw new NotImplementedException();

        public byte DefaultLogo => throw new NotImplementedException();

        public bool IsMaster => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public bool IsCGEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public byte Crawl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public byte Logo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public byte Parental { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsWideScreen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public void SetState(ICGElementsState state)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #endregion

        public CgElementsController()
        {
            Crawls = new List<CgElement>();
            Logos = new List<CgElement>();
            Parentals = new List<CgElement>();
            Auxes = new List<CgElement>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Started;
    }
}
