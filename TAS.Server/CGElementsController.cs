using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class CGElementsController : Remoting.Server.DtoBase, ICGElementsController, IInitializable
    {
        public CGElementsController()
        {
            ReadElements("Configuration\\Elements.xml");
        }
        public string Address { get; set; }
        public int GraphicsStartDelay { get; set; }

        internal void ReadElements(string xmlFile)
        {
            using (XmlReader reader = XmlReader.Create(xmlFile))
            {
                _crawls = _deserialize(reader, nameof(Crawls), nameof(Crawl));
                reader.Close();
            }
        }

        private IEnumerable<ICGElement> _deserialize(XmlReader reader, string rootElementName, string childElementName)
        {
            if (reader.ReadToDescendant(rootElementName))
            {
                List<CGElement> elements = (List<CGElement>)(new XmlSerializer(typeof(List<CGElement>), new XmlRootAttribute(rootElementName)).Deserialize(reader.ReadSubtree()));
                return elements.Cast<ICGElement>();
            }
            else
                return null;
        }

        private byte _crawl;
        public byte Crawl { get { return _crawl; } set { SetField(ref _crawl, value, nameof(Crawl)); } }

        IEnumerable<ICGElement> _crawls;
        public IEnumerable<ICGElement> Crawls { get { return _crawls; } }

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
                return false;
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
            
        }

        public void HideAux(int auxNr)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            
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
