using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class CgElementsController : Remoting.Server.DtoBase, ICGElementsController, IEnginePlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IEngine _engine;
        internal IEngine Engine
        {
            get => _engine;
            set
            {
                _engine = value;
                _engine.EngineOperation += Engine_EngineOperation;
            }
        }

        private bool _isCgEnabled = true;
        private bool _isWideScreen = true;
        private byte _logo;
        private byte _crawl;
        private byte _parental;
        private bool _isStartupExecuted = false;

        [XmlAttribute]
        public string EngineName { get; set; }

        [JsonProperty]
        public byte DefaultCrawl { get; set; } = 1;

        [JsonProperty]
        public byte DefaultLogo { get; set; } = 1;

        [JsonProperty]
        public virtual bool IsConnected => true;


        [JsonProperty]
        [XmlIgnore]
        public bool IsCGEnabled
        {
            get => _isCgEnabled;
            set => SetField(ref _isCgEnabled, value);
        }

        [JsonProperty]
        public bool IsMaster => true;

        [JsonProperty]
        [XmlIgnore]
        public bool IsWideScreen
        {
            get => _isWideScreen;
            set => SetField(ref _isWideScreen, value);
        }

        [JsonProperty]
        [XmlIgnore]
        public byte Crawl
        {
            get => _crawl;
            set
            {
                if (SetField(ref _crawl, value))
                    Engine.Execute(_crawls[value].Command);
            }
        }

        [JsonProperty(nameof(Crawls), ItemTypeNameHandling = TypeNameHandling.Objects)]
        [XmlArray(nameof(Crawls)), XmlArrayItem(nameof(Crawl))]
        public CGElement[] _crawls { get; set; } = new CGElement[0];

        public IEnumerable<ICGElement> Crawls => _crawls;

        [XmlIgnore]
        public byte Logo
        {
            get => _logo;
            set
            {
                if (SetField(ref _logo, value))
                    Engine.Execute(_logos[value].Command);
            }
        }

        [JsonProperty(nameof(Logos), ItemTypeNameHandling = TypeNameHandling.Objects)]
        [XmlArray(nameof(Logos)), XmlArrayItem(nameof(Logo))]
        public CGElement[] _logos { get; set; } = new CGElement[0];

        public IEnumerable<ICGElement> Logos => _logos;

        [JsonProperty]
        [XmlIgnore]
        public byte Parental
        {
            get => _parental;
            set
            {
                if (SetField(ref _parental, value))
                    Engine.Execute(_parentals[value].Command);
            }
        }

        [JsonProperty(nameof(Parentals), ItemTypeNameHandling = TypeNameHandling.Objects)]
        [XmlArray(nameof(Parentals)), XmlArrayItem(nameof(Parental))]
        public CGElement[] _parentals { get; set; } = new CGElement[0];

        public IEnumerable<ICGElement> Parentals => _parentals;

        [JsonProperty("Startup", ItemTypeNameHandling = TypeNameHandling.Objects)]
        [XmlArray("Startup"), XmlArrayItem("Item")]
        public CGElement[] _startup { get; set; } = new CGElement[0];

        public IEnumerable<ICGElement> Startup => _startup;

        public event EventHandler Started;

        public CgElementsController()
        {

        }

        public void SetState(ICGElementsState state)
        {
            try
            {
                if (!_isCgEnabled || !state.IsCGEnabled)
                    return;
                Logo = state.Logo;
                Crawl = state.Crawl;
                Parental = state.Parental;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void Clear()
        {
            try
            {
                if (!_isCgEnabled)
                    return;
                Logo = 0;
                Crawl = 0;
                Parental = 0;
                _isStartupExecuted = false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }


        private void Engine_EngineOperation(object sender, Common.EngineOperationEventArgs e)
        {
            if (
                (e.Operation == Common.TEngineOperation.Load || e.Operation == Common.TEngineOperation.Play)
                && !_isStartupExecuted
            )
            {
                ExecuteStartupItems();
            }
        }

        private void ExecuteStartupItems()
        {
            try
            {
                if (!_isCgEnabled)
                    return;


                Logger.Info("Executing startup items");
                foreach (CGElement el in this._startup)
                {
                    Logger.Info(el.Command);
                    Engine.Execute(el.Command);
                }

                _isStartupExecuted = true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
