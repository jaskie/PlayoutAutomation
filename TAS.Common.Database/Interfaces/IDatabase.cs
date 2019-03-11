using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;
using TAS.Common.Database.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Common.Interfaces.Security;

namespace TAS.Common.Database.Interfaces
{
    public interface IDatabase
    {
        ConnectionStateRedundant ConnectionState { get; }
        string ConnectionStringPrimary { get; }
        string ConnectionStringSecondary { get; }

        event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;

        void AsRunLogWrite(IEvent e);
        void CloneDatabase(string connectionStringSource, string connectionStringDestination);
        void Close();
        bool CreateEmptyDatabase(string connectionString, string collate);
        bool ArchiveContainsMedia(IArchiveDirectoryProperties dir, IPersistentMedia media);
        void DeleteArchiveDirectory(IArchiveDirectoryProperties dir);
        void DeleteEngine(IEnginePersistent engine);
        bool DeleteEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DeleteEvent(IEventPesistent aEvent);
        bool DeleteEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DeleteMedia(IAnimatedMedia animatedMedia);
        bool DeleteMedia(IArchiveMedia archiveMedia);
        bool DeleteMedia(IServerMedia serverMedia);
        void DeleteMediaSegment(IMediaSegment mediaSegment);
        void DeleteSecurityObject(ISecurityObject aco);
        void DeleteServer(IPlayoutServerProperties server);
        List<T> FindArchivedStaleMedia<T>(IArchiveDirectoryServerSide dir) where T : IArchiveMedia, new();
        void InsertArchiveDirectory(IArchiveDirectoryProperties dir);
        void InsertEngine(IEnginePersistent engine);
        bool InsertEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool InsertEvent(IEventPesistent aEvent);
        bool InsertEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool InsertMedia(IAnimatedMedia animatedMedia, ulong serverId);
        bool InsertMedia(IArchiveMedia archiveMedia, ulong serverid);
        bool InsertMedia(IServerMedia serverMedia, ulong serverId);
        void InsertSecurityObject(ISecurityObject aco);
        void InsertServer(IPlayoutServerProperties server);
        List<T> Load<T>() where T : ISecurityObject;
        List<T> LoadArchiveDirectories<T>() where T : IArchiveDirectoryProperties, new();
        List<T> LoadEngines<T>(ulong? instance = null) where T : IEnginePersistent;
        List<T> LoadServers<T>() where T : IPlayoutServerProperties;
        T ArchiveMediaFind<T>(IArchiveDirectoryServerSide dir, Guid mediaGuid) where T : IArchiveMedia, new();
        MediaDeleteResult MediaInUse(IEngine engine, IServerMedia serverMedia);
        T MediaSegmentsRead<T>(IPersistentMedia media) where T : IMediaSegments;
        List<IAclRight> ReadEngineAclList<TEngineAcl>(IPersistent engine, IAuthenticationServicePersitency authenticationService) where TEngineAcl : IAclRight, IPersistent, new();
        IEvent ReadEvent(IEngine engine, ulong idRundownEvent);
        List<IAclRight> ReadEventAclList<TEventAcl>(IEventPesistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl : IAclRight, IPersistent, new();
        IEvent ReadNext(IEngine engine, IEventPesistent aEvent);
        void ReadRootEvents(IEngine engine);
        List<IEvent> ReadSubEvents(IEngine engine, IEventPesistent eventOwner);
        ulong SaveMediaSegment(IMediaSegment mediaSegment);
        List<T> ArchiveMediaSearch<T>(IArchiveDirectoryServerSide dir, TMediaCategory? mediaCategory, string search) where T : IArchiveMedia, new();
        void SearchMissing(IEngine engine);
        List<IEvent> SearchPlaying(IEngine engine);
        void UpdateArchiveDirectory(IArchiveDirectoryProperties dir);
        void UpdateEngine(IEnginePersistent engine);
        bool UpdateEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool UpdateEvent<TEvent>(TEvent aEvent) where TEvent : IEventPesistent;
        bool UpdateEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        void UpdateMedia(IAnimatedMedia animatedMedia, ulong serverId);
        void UpdateMedia(IArchiveMedia archiveMedia, ulong serverId);
        void UpdateMedia(IServerMedia serverMedia, ulong serverId);
        void UpdateSecurityObject(ISecurityObject aco);
        void UpdateServer(IPlayoutServerProperties server);
        bool DropDatabase(string connectionString);
        void LoadAnimationDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IAnimatedMedia, new();
        void LoadServerDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IServerMedia, new();
        T LoadArchiveDirectory<T>(IMediaManager manager, ulong idArchive) where T : IArchiveDirectoryServerSide, new();
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