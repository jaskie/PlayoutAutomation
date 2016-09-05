using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class CGElementsController : Remoting.Server.DtoBase, ICGElementsController, IInitializable
    {
        public string Address { get; set; }
        public int GraphicsStartDelay { get; set; }

        private byte _crawl;
        public byte Crawl { get { return _crawl; } set { SetField(ref _crawl, value, nameof(Crawl)); } }

        public IEnumerable<ICGElement> Crawls
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _crawlVisible;
        public bool CrawlVisible { get { return _crawlVisible; } set { SetField(ref _crawlVisible, value, nameof(CrawlVisible)); } }

        public bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { SetField(ref _isEnabled, value, nameof(IsEnabled)); } }

        public bool IsMaster
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _isWideScreen;
        public bool IsWideScreen { get { return _isWideScreen; } set { SetField(ref _isWideScreen, value, nameof(IsWideScreen)); } }

        byte _logo;
        public byte Logo { get { return _logo; } set { SetField(ref _logo, value, nameof(Logo)); } }

        public IEnumerable<ICGElement> Logos
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _logoVisible;
        public bool LogoVisible { get { return _logoVisible; } set { SetField(ref _logoVisible, value, nameof(LogoVisible)); } }

        byte _parental;
        public byte Parental { get { return _parental; } set { SetField(ref _parental, value, nameof(Parental)); } }

        public IEnumerable<ICGElement> Parentals
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _parentalVisible;
        public bool ParentalVisible { get { return _parentalVisible; } set { SetField(ref _parentalVisible, value, nameof(ParentalVisible)); } }

        public int[] VisibleAuxes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler Started;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void HideAux(int auxNr)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void UnInitialize()
        {
            throw new NotImplementedException();
        }

        public void ShowAux(int auxNr)
        {
            throw new NotImplementedException();
        }

        public void SetState(ICGElementsState state)
        {
            if (_isEnabled)
            {

            }
        }
    }
}
