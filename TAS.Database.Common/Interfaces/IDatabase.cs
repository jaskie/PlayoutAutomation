using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;
using TAS.Database.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Common.Interfaces.Security;
using System.Configuration;
using System.Collections.ObjectModel;
using TAS.Common;

namespace TAS.Database.Common.Interfaces
{
    public interface IDatabase
    {
        DatabaseType DatabaseType { get; }
        ConnectionStateRedundant ConnectionState { get; }
        event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;
        void Open(ConnectionStringSettingsCollection connectionStringSettingsCollection);
        void InitializeFieldLengths();
        void Close();
        void AsRunLogWrite(ulong idEngine, IEvent e);
        void DeleteArchiveDirectory(IArchiveDirectoryProperties dir);
        void DeleteEngine(IEnginePersistent engine);
        bool DeleteEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool DeleteEvent(IEventPersistent aEvent);
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
        bool InsertEvent(IEventPersistent aEvent);
        bool InsertEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        void InsertMedia(IAnimatedMedia animatedMedia, ulong serverId);
        void InsertMedia(IArchiveMedia archiveMedia, ulong serverid);
        void InsertMedia(IServerMedia serverMedia, ulong serverId);
        void InsertSecurityObject(ISecurityObject aco);
        void InsertServer(IPlayoutServerProperties server);
        List<T> LoadSecurityObject<T>() where T : ISecurityObject;
        ReadOnlyCollection<T> LoadArchiveDirectories<T>() where T : IArchiveDirectoryProperties, new();
        ReadOnlyCollection<T> LoadEngines<T>(ulong? instance = null) where T : IEnginePersistent;
        ReadOnlyCollection<T> LoadServers<T>() where T : IPlayoutServerProperties;
        bool ArchiveContainsMedia(IArchiveDirectoryProperties dir, Guid mediaGuid);
        T ArchiveMediaFind<T>(IArchiveDirectoryServerSide dir, Guid mediaGuid) where T : IArchiveMedia, new();
        MediaDeleteResult MediaInUse(IEngine engine, IServerMedia serverMedia);
        T MediaSegmentsRead<T>(IPersistentMedia media) where T : IMediaSegments;
        List<IAclRight> ReadEngineAclList<TEngineAcl>(IPersistent engine, IAuthenticationServicePersitency authenticationService) where TEngineAcl : IAclRight, IPersistent, new();
        IEvent ReadEvent(IEngine engine, ulong idRundownEvent);
        List<IAclRight> ReadEventAclList<TEventAcl>(IEventPersistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl : IAclRight, IPersistent, new();
        IEvent ReadNext(IEngine engine, IEventPersistent aEvent);
        void ReadRootEvents(IEngine engine);
        List<IEvent> ReadSubEvents(IEngine engine, IEventPersistent eventOwner);
        ulong SaveMediaSegment(IMediaSegment mediaSegment);
        List<T> ArchiveMediaSearch<T>(IArchiveDirectoryServerSide dir, TMediaCategory? mediaCategory, string search) where T : IArchiveMedia, new();
        void SearchMissing(IEngine engine);
        List<IEvent> SearchPlaying(IEngine engine);
        void UpdateArchiveDirectory(IArchiveDirectoryProperties dir);
        void UpdateEngine(IEnginePersistent engine);
        bool UpdateEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        bool UpdateEvent<TEvent>(TEvent aEvent) where TEvent : IEventPersistent;
        bool UpdateEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent;
        void UpdateMedia(IAnimatedMedia animatedMedia, ulong serverId);
        void UpdateMedia(IArchiveMedia archiveMedia, ulong serverId);
        void UpdateMedia(IServerMedia serverMedia, ulong serverId);
        void UpdateSecurityObject(ISecurityObject aco);
        void UpdateServer(IPlayoutServerProperties server);
        void LoadAnimationDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IAnimatedMedia, new();
        void LoadServerDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IServerMedia, new();
        IDictionary<string, int> ServerMediaFieldLengths { get; }
        IDictionary<string, int> ArchiveMediaFieldLengths { get; }
        IDictionary<string, int> EventFieldLengths { get; }
        IDictionary<string, int> SecurityObjectFieldLengths { get; }
        IDictionary<string, int> MediaSegmentFieldLengths { get; }
        IDictionary<string, int> EngineFieldLengths { get; }
        IDictionary<string, int> ServerFieldLengths { get; }
        void UpdateDb();
        bool UpdateRequired();
    }
}