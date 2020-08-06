using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Database.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.Model
{
    public class Engine : IEnginePersistent, IConfigEngine
    {
        public ulong Id { get; set; }

        public ulong Instance { get; set; }

        public ulong IdServerPRI { get; set; }

        public int ServerChannelPRI { get; set; }

        public ulong IdServerSEC { get; set; }

        public int ServerChannelSEC { get; set; }

        public ulong IdServerPRV { get; set; }

        public int ServerChannelPRV { get; set; }

        public ulong IdArchive { get; set; }

        [Hibernate]
        public TAspectRatioControl AspectRatioControl { get; set; }

        [Hibernate]
        public string EngineName { get; set; }

        [Hibernate]
        public int TimeCorrection { get; set; }

        [Hibernate]
        public TVideoFormat VideoFormat { get; set; }

        [Hibernate]
        public RemoteHost Remote { get; set; }

        [Hibernate]
        public bool EnableCGElementsForNewEvents { get; set; }

        [Hibernate]
        public bool StudioMode { get; set; }

        [Hibernate]
        public TCrawlEnableBehavior CrawlEnableBehavior { get; set; }

        [Hibernate]
        public int CGStartDelay { get; set; }

        //note for JJ, what is it?
        public bool IsModified = false;

        public bool IsNew = true;
        [Hibernate]        
        public ICGElementsController CGElementsController { get; set; }
        [Hibernate]
        public IVideoSwitch Router { get; set; }        
        
        public IDictionary<string, int> FieldLengths { get; set; }          

        public List<IConfigCasparServer> Servers { get; set; }
        [Hibernate]
        public List<IGpi> Gpis { get; set; }

        public ArchiveDirectories ArchiveDirectories;       

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }
    }
}
