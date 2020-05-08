using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using jNet.RPC;

namespace TAS.Server
{
    public class CgElementsController : ServerObjectBase, ICGElementsController, IEnginePlugin
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

        [DtoMember]
        public byte DefaultCrawl { get; set; } = 1;

        [DtoMember]
        public byte DefaultLogo { get; set; } = 1;

        [DtoMember]
        public virtual bool IsConnected => true;


        [DtoMember]
        [XmlIgnore]
        public bool IsCGEnabled
        {
            get => _isCgEnabled;
            set => SetField(ref _isCgEnabled, value);
        }

        [DtoMember]
        public bool IsMaster => true;

        [DtoMember]
        [XmlIgnore]
        public bool IsWideScreen
        {
            get => _isWideScreen;
            set => SetField(ref _isWideScreen, value);
        }

        [DtoMember]
        [XmlIgnore]
        public byte Crawl
        {
            get => _crawl;
            set
            {
                if (!SetField(ref _crawl, value))
                    return;
                Engine.Execute(_crawls.ElementAtOrDefault(value)?.Command);
            }
        }

        [DtoMember(nameof(Crawls))]
        [XmlArray(nameof(Crawls)), XmlArrayItem(nameof(Crawl))]
        public CGElement[] _crawls { get; set; } = new CGElement[0];

        public IEnumerable<ICGElement> Crawls => _crawls;

        [DtoMember]
        [XmlIgnore]
        public byte Logo
        {
            get => _logo;
            set
            {
                if (!SetField(ref _logo, value))
                    return;
                Engine.Execute(_logos.ElementAtOrDefault(value)?.Command);
            }
        }

        [DtoMember(nameof(Logos))]
        [XmlArray(nameof(Logos)), XmlArrayItem(nameof(Logo))]
        public CGElement[] _logos { get; set; } = new CGElement[0];

        public IEnumerable<ICGElement> Logos => _logos;

        [DtoMember]
        [XmlIgnore]
        public byte Parental
        {
            get => _parental;
            set
            {
                if (!SetField(ref _parental, value))
                    return;
                Engine.Execute(_parentals.ElementAtOrDefault(value)?.Command);
            }
        }

        [DtoMember(nameof(Parentals))]
        [XmlArray(nameof(Parentals)), XmlArrayItem(nameof(Parental))]
        public CGElement[] _parentals { get; set; } = new CGElement[0];

        public IEnumerable<ICGElement> Parentals => _parentals;

        [XmlArray("Startup"), XmlArrayItem("Command")]
        public string[] _startup { get; set; } = new string[0];

        public event EventHandler Started;

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
        [XmlAttribute]
        public bool IsEnabled { get; }
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
            if ((e.Operation == Common.TEngineOperation.Load || e.Operation == Common.TEngineOperation.Play)
                && !_isStartupExecuted)
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


                Logger.Debug("Executing startup items");
                foreach (var command in _startup)
                {
                    Logger.Trace("Executing startup command: {0}", command);
                    Engine.Execute(command);
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
