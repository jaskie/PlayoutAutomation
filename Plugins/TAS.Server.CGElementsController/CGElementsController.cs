using System;
using System.Collections.Generic;
using System.Linq;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using jNet.RPC;
using System.ComponentModel.Composition;
using TAS.Database.Common;

namespace TAS.Server.CgElementsController
{    
    [Export(typeof(IPlugin))]
    public class CgElementsController : ServerObjectBase, ICGElementsController
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

        [Hibernate]
        public bool IsEnabled { get; set; }

        [DtoMember, Hibernate]
        public byte DefaultCrawl { get; set; }

        [DtoMember, Hibernate]
        public byte DefaultLogo { get; set; }

        [DtoMember]
        public virtual bool IsConnected => true;

        [DtoMember]        
        public bool IsCGEnabled
        {
            get => _isCgEnabled;
            set => SetField(ref _isCgEnabled, value);
        }

        [DtoMember]
        public bool IsMaster => true;

        [DtoMember]
        public bool IsWideScreen
        {
            get => _isWideScreen;
            set => SetField(ref _isWideScreen, value);
        }

        [DtoMember]        
        public byte Crawl
        {
            get => _crawl;
            set
            {
                if (!SetField(ref _crawl, value))
                    return;
                Engine.Execute(((Model.CGElement)Crawls.ElementAtOrDefault(value))?.Command);
            }
        }

        [DtoMember, Hibernate]        
        public IEnumerable<ICGElement> Crawls { get; set; }               

        [DtoMember]        
        public byte Logo
        {
            get => _logo;
            set
            {
                if (!SetField(ref _logo, value))
                    return;
                Engine.Execute(((Model.CGElement)Logos.ElementAtOrDefault(value))?.Command);
            }
        }

        [DtoMember, Hibernate]        
        public IEnumerable<ICGElement> Logos { get; set; }                

        [DtoMember]        
        public byte Parental
        {
            get => _parental;
            set
            {
                if (!SetField(ref _parental, value))
                    return;
                Engine.Execute(((Model.CGElement)Parentals.ElementAtOrDefault(value))?.Command);
            }
        }

        [DtoMember, Hibernate]        
        public IEnumerable<ICGElement> Parentals { get; set; }                                       
        
        [Hibernate]
        public IEnumerable<ICGElement> Auxes { get; }
        
        [Hibernate]
        public IEnumerable<string> Startups { get; }

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
                foreach (var command in Startups)
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
