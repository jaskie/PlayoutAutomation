using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Common.Interfaces
{
    public interface IDatabase
    {
        ConnectionStateRedundant ConnectionState { get; }
        string ConnectionStringPrimary { get; }
        string ConnectionStringSecondary { get; }

        event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;

        void AsRunLogWrite(IEventPesistent e);
        bool CloneDatabase(string connectionStringSource, string connectionStringDestination);
        void Close();
        bool CreateEmptyDatabase(string connectionString, string collate);
        bool DbArchiveContainsMedia(IArchiveDirectory dir, IMediaProperties media);
        void DbDeleteArchiveDirectory(IArchiveDirectoryProperties dir);
        void DbDeleteEngine(IEnginePersistent engine);
        bool DbDeleteEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DbDeleteEvent(IEventPesistent aEvent);
        bool DbDeleteEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DbDeleteMedia(IAnimatedMedia animatedMedia);
        bool DbDeleteMedia(IArchiveMedia archiveMedia);
        bool DbDeleteMedia(IServerMedia serverMedia);
        void DbDeleteMediaSegment(IMediaSegment mediaSegment);
        void DbDeleteSecurityObject(ISecurityObject aco);
        void DbDeleteServer(IPlayoutServerProperties server);
        IEnumerable<IArchiveMedia> DbFindStaleMedia<T>(IArchiveDirectory dir) where T : IArchiveMedia;
        void DbInsertArchiveDirectory(IArchiveDirectoryProperties dir);
        void DbInsertEngine(IEnginePersistent engine);
        bool DbInsertEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DbInsertEvent(IEventPesistent aEvent);
        bool DbInsertEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DbInsertMedia(IAnimatedMedia animatedMedia, ulong serverId);
        bool DbInsertMedia(IArchiveMedia archiveMedia, ulong serverid);
        bool DbInsertMedia(IServerMedia serverMedia, ulong serverId);
        void DbInsertSecurityObject(ISecurityObject aco);
        void DbInsertServer(IPlayoutServerProperties server);
        List<T> DbLoad<T>() where T : ISecurityObject;
        List<T> DbLoadArchiveDirectories<T>() where T : IArchiveDirectoryProperties, new();
        List<T> DbLoadEngines<T>(ulong? instance = null) where T : IEnginePersistent;
        List<T> DbLoadServers<T>() where T : IPlayoutServerProperties;
        T DbMediaFind<T>(IArchiveDirectory dir, IMediaProperties media) where T : IArchiveMedia;
        MediaDeleteResult DbMediaInUse(IEngine engine, IServerMedia serverMedia);
        T DbMediaSegmentsRead<T>(IPersistentMedia media) where T : IMediaSegments;
        List<IAclRight> DbReadEngineAclList<TEngineAcl>(IPersistent engine, IAuthenticationServicePersitency authenticationService) where TEngineAcl : IAclRight, IPersistent, new();
        IEvent DbReadEvent(IEngine engine, ulong idRundownEvent);
        List<IAclRight> DbReadEventAclList<TEventAcl>(IEventPesistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl : IAclRight, IPersistent, new();
        IEvent DbReadNext(IEngine engine, IEventPesistent aEvent);
        void DbReadRootEvents(IEngine engine);
        List<IEvent> DbReadSubEvents(IEngine engine, IEventPesistent eventOwner);
        ulong DbSaveMediaSegment(IMediaSegment mediaSegment);
        void DbSearch<T>(IArchiveDirectory dir) where T : IArchiveMedia;
        void DbSearchMissing(IEngine engine);
        List<IEvent> DbSearchPlaying(IEngine engine);
        void DbUpdateArchiveDirectory(IArchiveDirectoryProperties dir);
        void DbUpdateEngine(IEnginePersistent engine);
        bool DbUpdateEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DbUpdateEvent<TEvent>(TEvent aEvent) where TEvent : IEventPesistent;
        bool DbUpdateEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        void DbUpdateMedia(IAnimatedMedia animatedMedia, ulong serverId);
        void DbUpdateMedia(IArchiveMedia archiveMedia, ulong serverId);
        void DbUpdateMedia(IServerMedia serverMedia, ulong serverId);
        void DbUpdateSecurityObject(ISecurityObject aco);
        void DbUpdateServer(IPlayoutServerProperties server);
        bool DropDatabase(string connectionString);
        void Load<T>(IAnimationDirectory directory, ulong serverId) where T : IAnimatedMedia;
        void Load<T>(IServerDirectory directory, IArchiveDirectory archiveDirectory, ulong serverId) where T : IServerMedia;
        IArchiveDirectory LoadArchiveDirectory<T>(IMediaManager manager, ulong idArchive) where T : IArchiveDirectory;
        void Open(string connectionStringPrimary = null, string connectionStringSecondary = null);
        bool TestConnect(string connectionString);
        bool UpdateDb();
        bool UpdateRequired();
    }
}