using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class CgElementsController : Remoting.Server.DtoBase, ICGElementsController
    {
        private const string ElementsFileName = "CgElements.xml";

        private bool _isCgEnabled;
        private bool _isWideScreen;
        private byte _logo;
        private byte _crawl;
        private byte _parental;

        public CgElementsController(IEngine engine)
        {
            _engine = engine;
        }

        public void Initialize()
        {
            ReadElements(Path.Combine(FileUtils.ConfigurationPath, ElementsFileName));
            IsCGEnabled = true;
            IsWideScreen = true;
        }

        private readonly IEngine _engine;

        internal void ReadElements(string xmlFile)
        {
            using (var reader = XmlReader.Create(xmlFile))
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "CgElements")
                {
                    if (reader.HasAttributes && reader.GetAttribute("Engine") == _engine.EngineName)
                        while (reader.Read())
                        {
                            switch (reader.Name)
                            {
                                case nameof(Crawls):
                                    _crawls = _deserializeList(reader, nameof(Crawls), nameof(Crawl));
                                    break;
                                case nameof(Logos):
                                    _logos = _deserializeList(reader, nameof(Logos), nameof(Logo));
                                    break;
                                case nameof(Parentals):
                                    _parentals = _deserializeList(reader, nameof(Parentals), nameof(Parental));
                                    break;
                            }
                        }
                }
                reader.Close();
            }
        }

        private CGElement[] _deserializeList(XmlReader reader, string rootElementName, string childElementName)
        {
            var overrides = new XmlAttributeOverrides();
            var rootAttribs = new XmlAttributes { XmlRoot = new XmlRootAttribute(rootElementName) };
            var elementAttribs = new XmlAttributes { XmlType = new XmlTypeAttribute(childElementName) };
            overrides.Add(typeof(List<CGElement>), rootAttribs);
            overrides.Add(typeof(CGElement), elementAttribs);
            var elements = (List<CGElement>)(new XmlSerializer(typeof(List<CGElement>), overrides).Deserialize(reader.ReadSubtree()));
            return elements.ToArray();
        }

        [JsonProperty]
        public byte DefaultCrawl => 1;

        public byte DefaultLogo => 1;

        [JsonProperty]
        public virtual bool IsConnected => true;

        
        [JsonProperty]
        public bool IsCGEnabled
        {
            get => _isCgEnabled;
            set => SetField(ref _isCgEnabled, value);
        }

        [JsonProperty]
        public bool IsMaster  => true;

        [JsonProperty]
        public bool IsWideScreen
        {
            get => _isWideScreen;
            set => SetField(ref _isWideScreen, value);
        }

        [JsonProperty]
        public byte Crawl
        {
            get => _crawl;
            set
            {
                if (SetField(ref _crawl, value))
                    _engine.Execute(_crawls[value].Command);
            }
        }

        [JsonProperty(nameof(Crawls), ItemTypeNameHandling = TypeNameHandling.Objects)]
        private CGElement[] _crawls = new CGElement[0];

        public IEnumerable<ICGElement> Crawls => _crawls;

        public byte Logo
        {
            get => _logo;
            set
            {
                if (SetField(ref _logo, value))
                    _engine.Execute(_logos[value].Command);
            }
        }

        [JsonProperty(nameof(Logos), ItemTypeNameHandling = TypeNameHandling.Objects)]
        private CGElement[] _logos = new CGElement[0];

        public IEnumerable<ICGElement> Logos => _logos;

        [JsonProperty]
        public byte Parental
        {
            get => _parental;
            set
            {
                if (SetField(ref _parental, value))
                    _engine.Execute(_parentals[value].Command);
            }
        }

        [JsonProperty(nameof(Parentals), ItemTypeNameHandling = TypeNameHandling.Objects)]
        private CGElement[] _parentals = new CGElement[0];

        public IEnumerable<ICGElement> Parentals => _parentals;

        public event EventHandler Started;

        public void SetState(ICGElementsState state)
        {
            if (!_isCgEnabled || !state.IsCGEnabled)
                return;
            Logo = state.Logo;
            Crawl = state.Crawl;
            Parental = state.Parental;
        }

        public void Clear()
        {
            if (!_isCgEnabled)
                return;
            Logo = 0;
            Crawl = 0;
            Parental = 0;
        }
    }
}
