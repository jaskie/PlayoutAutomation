using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class PluginExamplefactory : IEnginePluginFactory
    {
        static Dictionary<IEngine, CGElementsController> _plugins = new Dictionary<IEngine, CGElementsController>();
        static object pluginLock = new object();
        public object CreateEnginePlugin(IEngine engine, Type type)
        {
            if (type.IsAssignableFrom(typeof(CGElementsController)))
            {
                CGElementsController plugin;
                lock (pluginLock)
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
            else
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
            ReadElements(Path.Combine(FileUtils.CONFIGURATION_PATH, ELEMENTS));
            IsCGEnabled = true;
            IsWideScreen = true;
        }

        public string Address { get; set; }
        public int GraphicsStartDelay { get; set; }
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
                            }
                        }
                }
                reader.Close();
            }
        }

        private IEnumerable<ICGElement> _deserializeList(XmlReader reader, string rootElementName, string childElementName)
        {
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            XmlAttributes rootAttribs = new XmlAttributes { XmlRoot = new XmlRootAttribute(rootElementName) };
            XmlAttributes elementAttribs = new XmlAttributes { XmlType = new XmlTypeAttribute(childElementName) };
            overrides.Add(typeof(List<CGElement>), rootAttribs);
            overrides.Add(typeof(CGElement), elementAttribs);
            List<CGElement> elements = (List<CGElement>)(new XmlSerializer(typeof(List<CGElement>), overrides).Deserialize(reader.ReadSubtree()));
            return elements.Cast<ICGElement>();
        }

        private byte _crawl;
        public byte Crawl { get { return _crawl; } set { SetField(ref _crawl, value, nameof(Crawl)); } }

        IEnumerable<ICGElement> _crawls;
        public IEnumerable<ICGElement> Crawls { get { return _crawls; } }

        public virtual bool IsConnected { get { return true; } }

        private bool _isCGEnabled;
        public bool IsCGEnabled { get { return _isCGEnabled; } set { SetField(ref _isCGEnabled, value, nameof(IsCGEnabled)); } }

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

        IEnumerable<ICGElement> _logos;
        public IEnumerable<ICGElement> Logos { get { return _logos; } }

        byte _parental;
        public byte Parental { get { return _parental; } set { SetField(ref _parental, value, nameof(Parental)); } }

        IEnumerable<ICGElement> _parentals;
        public IEnumerable<ICGElement> Parentals { get { return _parentals; } }

        public byte[] VisibleAuxes
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


        public void ShowAux(int auxNr)
        {
            throw new NotImplementedException();
        }

        public void SetState(ICGElementsState state)
        {
            if (_isCGEnabled)
            {

            }
        }
    }
}
