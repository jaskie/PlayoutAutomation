using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Common.Interfaces.Security;

namespace TAS.Common.Interfaces
{
    public interface IDatabase
    {
        ConnectionStateRedundant ConnectionState { get; }
        string ConnectionStringPrimary { get; }
        string ConnectionStringSecondary { get; }

        event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;

        void AsRunLogWrite(IEventPesistent e);
        void CloneDatabase(string connectionStringSource, string connectionStringDestination);
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
        List<T> FindArchivedStaleMedia<T>(IArchiveDirectory dir) where T : IArchiveMedia, new();
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
        T ArchiveMediaFind<T>(IArchiveDirectory dir, Guid mediaGuid) where T : IArchiveMedia, new();
        MediaDeleteResult DbMediaInUse(IEngine engine, IServerMedia serverMedia);
        T DbMediaSegmentsRead<T>(IPersistentMedia media) where T : IMediaSegments;
        List<IAclRight> DbReadEngineAclList<TEngineAcl>(IPersistent engine, IAuthenticationServicePersitency authenticationService) where TEngineAcl : IAclRight, IPersistent, new();
        IEvent DbReadEvent(IEngine engine, ulong idRundownEvent);
        List<IAclRight> DbReadEventAclList<TEventAcl>(IEventPesistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl : IAclRight, IPersistent, new();
        IEvent DbReadNext(IEngine engine, IEventPesistent aEvent);
        void DbReadRootEvents(IEngine engine);
        List<IEvent> DbReadSubEvents(IEngine engine, IEventPesistent eventOwner);
        ulong DbSaveMediaSegment(IMediaSegment mediaSegment);
        List<T> ArchiveMediaSearch<T>(IArchiveDirectoryServerSide dir, TMediaCategory? mediaCategory, string search) where T : IArchiveMedia, new();
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
        void LoadAnimationDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IAnimatedMedia, new();
        void LoadServerDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IServerMedia, new();
        IArchiveDirectory LoadArchiveDirectory<T>(IMediaManager manager, ulong idArchive) where T : IArchiveDirectory, new();
        void Open(string connectionStringPrimary = null, string connectionStringSecondary = null);
        void TestConnect(string connectionString);
        bool UpdateDb();
        bool UpdateRequired();
        IDictionary<string, int> ServerMediaFieldLengths { get; }
        IDictionary<string, int> ArchiveMediaFieldLengths { get; }
        IDictionary<string, int> EventFieldLengths { get; }
        IDictionary<string, int> SecurityObjectFieldLengths { get; }
        IDictionary<string, int> MediaSegmentFieldLengths { get; }
        IDictionary<string, int> EngineFieldLengths { get; }
        IDictionary<string, int> ServerFieldLengths { get; }
    }
}