using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class PluginExamplefactory : IEnginePluginFactory
    {
        private readonly Dictionary<IEngine, CGElementsController> _plugins = new Dictionary<IEngine, CGElementsController>();
        private readonly object _pluginLock = new object();
        public object CreateEnginePlugin(IEngine engine, Type type)
        {
            if (type.IsAssignableFrom(typeof(CGElementsController)))
            {
                CGElementsController plugin;
                lock (_pluginLock)
                {
                    if (!_plugins.TryGetValue(engine, out plugin))
                    {
                        plugin = new CGElementsController(engine);
                        plugin.Initialize();
                        _plugins.Add(engine, plugin);
                    }
                }
                return plugin;
            }
            return null;
        }

        public IEnumerable<Type> Types()
        {
            return new[] { typeof(CGElementsController) };
        }
    }


    public class CGElementsController : Remoting.Server.DtoBase, ICGElementsController
    {
        const string ELEMENTS = "Elements.xml";
        public CGElementsController(IEngine engine)
        {
            _engine = engine;
        }

        public void Initialize()
        {
            ReadElements(Path.Combine(FileUtils.ConfigurationPath, ELEMENTS));
            IsCGEnabled = true;
            IsWideScreen = true;
        }

        private readonly IEngine _engine;

        internal void ReadElements(string xmlFile)
        {
            using (XmlReader reader = XmlReader.Create(xmlFile))
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "Elements")
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
                                case nameof(Auxes):
                                    _auxes = _deserializeList(reader, nameof(Auxes), "Aux");
                                    break;
                            }
                        }
                }
                reader.Close();
            }
        }

        private CGElement[] _deserializeList(XmlReader reader, string rootElementName, string childElementName)
        {
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            XmlAttributes rootAttribs = new XmlAttributes { XmlRoot = new XmlRootAttribute(rootElementName) };
            XmlAttributes elementAttribs = new XmlAttributes { XmlType = new XmlTypeAttribute(childElementName) };
            overrides.Add(typeof(List<CGElement>), rootAttribs);
            overrides.Add(typeof(CGElement), elementAttribs);
            List<CGElement> elements = (List<CGElement>)(new XmlSerializer(typeof(List<CGElement>), overrides).Deserialize(reader.ReadSubtree()));
            return elements.ToArray();
        }

        [JsonProperty]
        public byte DefaultCrawl => 1;

        public byte DefaultLogo => 1;

        [JsonProperty]
        public virtual bool IsConnected => true;

        private bool _isCGEnabled;
        [JsonProperty]
        public bool IsCGEnabled { get { return _isCGEnabled; } set { SetField(ref _isCGEnabled, value); } }

        [JsonProperty]
        public bool IsMaster  => true;

        bool _isWideScreen;
        [JsonProperty]
        public bool IsWideScreen
        {
            get { return _isWideScreen; }
            set { SetField(ref _isWideScreen, value); }
        }

        private byte _crawl;
        [JsonProperty]
        public byte Crawl { get { return _crawl; } set { SetField(ref _crawl, value); } }

        [JsonProperty(nameof(Crawls), ItemTypeNameHandling = TypeNameHandling.Objects)]
        ICGElement[] _crawls = new ICGElement[0];
        public IEnumerable<ICGElement> Crawls => _crawls;

        byte _logo;
        public byte Logo
        {
            get { return _logo; }
            set
            {
                if (SetField(ref _logo, value))
                    _engine.Execute(_logos[value].Command);
            }
        }

        [JsonProperty(nameof(Logos), ItemTypeNameHandling = TypeNameHandling.Objects)]
        CGElement[] _logos = new CGElement[0];
        public IEnumerable<ICGElement> Logos => _logos;

        byte _parental;
        [JsonProperty]
        public byte Parental
        {
            get { return _parental; }
            set
            {
                if (SetField(ref _parental, value))
                    _engine.Execute(_parentals[value].Command);
            }
        }

        [JsonProperty(nameof(Parentals), ItemTypeNameHandling = TypeNameHandling.Objects)]
        CGElement[] _parentals = new CGElement[0];
        public IEnumerable<ICGElement> Parentals => _parentals;

        [JsonProperty(nameof(VisibleAuxes), ItemTypeNameHandling = TypeNameHandling.Objects)]
        byte[] _visibleAuxes = new byte[0];
        public byte[] VisibleAuxes => _visibleAuxes;

        [JsonProperty(nameof(Auxes), ItemTypeNameHandling = TypeNameHandling.Objects)]
        ICGElement[] _auxes = new ICGElement[0];
        public IEnumerable<ICGElement> Auxes => _auxes;

        public event EventHandler Started;

        public void HideAux(int auxNr)
        {
            throw new NotImplementedException();
        }


        public void ShowAux(int auxNr)
        {
            throw new NotImplementedException();
        }

        public void SetState(ICGElementsState state)
        {
            if (_isCGEnabled && state.IsCGEnabled)
            {
                Logo = state.Logo;
                Crawl = state.Crawl;
                Parental = state.Parental;
            }
        }
    }
}
