using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Database.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class Engine : IEnginePersistent
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

        [Hibernate]
        public bool TryContinueRundownAfterEngineRestart { get; set; }

        public bool IsModified = false;

        public bool IsNew = true;

        public IDictionary<string, int> FieldLengths { get; set; }

        public void Save() => throw new NotImplementedException();

        public void Delete() => throw new NotImplementedException();

        internal IReadOnlyCollection<CasparServer> Servers;

        internal ArchiveDirectories ArchiveDirectories;

    }
}
