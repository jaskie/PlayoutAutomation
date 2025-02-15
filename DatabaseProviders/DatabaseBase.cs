//#undef DEBUG
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
#if SQLITE
using System.Data.SQLite;
#endif
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TAS.Common;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Database.Common.Interfaces.Media;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Common.Interfaces.Security;

#if MYSQL
namespace TAS.Database.MySqlRedundant
#elif SQLITE
namespace TAS.Database.SQLite
#endif
{
    public abstract class DatabaseBase : IDatabase
    {

#if MYSQL

        private static readonly DateTime MinMySqlDate = new DateTime(1000, 01, 01);
        private static readonly DateTime MaxMySqlDate = new DateTime(9999, 12, 31, 23, 59, 59);

        protected DbConnectionRedundant Connection;

        public DatabaseType DatabaseType { get; } = DatabaseType.MySQL;

        private DbCommandRedundant CreateCommand(string sql) => new DbCommandRedundant(sql, Connection);
#elif SQLITE
        protected SQLiteConnection Connection;

        public DatabaseType DatabaseType { get; } = DatabaseType.SQLite;
        protected SQLiteCommand CreateCommand(string sql) => new SQLiteCommand(sql, Connection);

#endif
        protected abstract string TrimText(string tableName, string columnName, string value);

        private static readonly Newtonsoft.Json.JsonSerializerSettings HibernationSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
        {
            ContractResolver = new HibernationContractResolver(),
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
        };

        public abstract void Open(ConnectionStringSettingsCollection connectionStringSettingsCollection);

        public abstract void InitializeFieldLengths();

        public event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;

        public abstract void Close();

        public abstract ConnectionStateRedundant ConnectionState { get; }

        public abstract bool UpdateRequired();
        
        public abstract void UpdateDb();

        protected void Connection_StateRedundantChange(object sender, RedundantConnectionStateEventArgs e)
        {
            ConnectionStateChanged?.Invoke(sender, e);
        }        

        public IDictionary<string, int> ServerMediaFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> ArchiveMediaFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> EventFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> MediaSegmentFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> EngineFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> ServerFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> SecurityObjectFieldLengths { get; } = new Dictionary<string, int>();

        #region IPlayoutServer


        public ReadOnlyCollection<T> LoadServers<T>() where T : IPlayoutServerProperties
        {
            var servers = new List<T>();
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT * FROM server;"))
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var server = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(dataReader.GetString("Config"), HibernationSerializerSettings);
                        server.Id = dataReader.GetUInt64("idServer");
                        servers.Add(server);
                    }
                    dataReader.Close();
                }
            }
            return servers.AsReadOnly();
        }

        public void InsertServer(IPlayoutServerProperties server) 
        {
            lock (Connection)
            {
                {
                    using (var cmd = CreateCommand(@"INSERT INTO server (typServer, Config) VALUES (0, @Config)"))
                    {
                        cmd.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(server, HibernationSerializerSettings));
                        cmd.ExecuteNonQuery();
                        server.Id = (ulong)cmd.LastInsertedId();
                    }
                }
            }
        }

        public void UpdateServer(IPlayoutServerProperties server) 
        {
            lock (Connection)
            {

                using (var cmd = CreateCommand("UPDATE server SET Config=@Config WHERE idServer=@idServer;"))
                {
                    cmd.Parameters.AddWithValue("@idServer", server.Id);
                    cmd.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(server, HibernationSerializerSettings));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteServer(IPlayoutServerProperties server) 
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("DELETE FROM server WHERE idServer=@idServer;"))
                {
                    cmd.Parameters.AddWithValue("@idServer", server.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion //IPlayoutServer

        #region IEngine

        public ReadOnlyCollection<T> LoadEngines<T>(ulong? instance = null) where T : IEnginePersistent
        {
            var engines = new List<T>();
            lock (Connection)
            {
                using (var cmd = instance == null
                    ? CreateCommand("SELECT * FROM engine;")
                    : CreateCommand("SELECT * FROM engine WHERE Instance=@Instance;")
                    )
                {
                    if (instance != null)
                        cmd.Parameters.AddWithValue("@Instance", instance);
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var engine = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(dataReader.GetString("Config"), HibernationSerializerSettings);
                            engine.Id = dataReader.GetUInt64("idEngine");
                            engine.IdServerPRI = dataReader.GetUInt64("idServerPRI");
                            engine.ServerChannelPRI = dataReader.GetInt32("ServerChannelPRI");
                            engine.IdServerSEC = dataReader.GetUInt64("idServerSEC");
                            engine.ServerChannelSEC = dataReader.GetInt32("ServerChannelSEC");
                            engine.IdServerPRV = dataReader.GetUInt64("idServerPRV");
                            engine.ServerChannelPRV = dataReader.GetInt32("ServerChannelPRV");
                            engine.IdArchive = dataReader.GetUInt64("idArchive");
                            engine.Instance = dataReader.GetUInt64("Instance");
                            engines.Add(engine);
                        }
                        dataReader.Close();
                        return engines.AsReadOnly();
                    }
                }
            }
        }

        public void InsertEngine(IEnginePersistent engine) 
        {
            lock (Connection)
            {
                {
                    using (var cmd = CreateCommand(@"INSERT INTO engine (Instance, idServerPRI, ServerChannelPRI, idServerSEC, ServerChannelSEC, idServerPRV,  ServerChannelPRV, idArchive,  Config) VALUES (@Instance, @idServerPRI, @ServerChannelPRI, @idServerSEC, @ServerChannelSEC, @idServerPRV, @ServerChannelPRV, @idArchive, @Config);"))
                    {
                        cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                        cmd.Parameters.AddWithValue("@idServerPRI", engine.IdServerPRI);
                        cmd.Parameters.AddWithValue("@ServerChannelPRI", engine.ServerChannelPRI);
                        cmd.Parameters.AddWithValue("@idServerSEC", engine.IdServerSEC);
                        cmd.Parameters.AddWithValue("@ServerChannelSEC", engine.ServerChannelSEC);
                        cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                        cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                        cmd.Parameters.AddWithValue("@idArchive", engine.IdArchive);
                        cmd.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(engine, HibernationSerializerSettings));
                        cmd.ExecuteNonQuery();
                        engine.Id = (ulong)cmd.LastInsertedId();
                    }
                }
            }
        }

        public void UpdateEngine(IEnginePersistent engine)
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand(@"UPDATE engine SET Instance=@Instance, idServerPRI=@idServerPRI, ServerChannelPRI=@ServerChannelPRI, idServerSEC=@idServerSEC, ServerChannelSEC=@ServerChannelSEC, idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config WHERE idEngine=@idEngine"))
                {
                    cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                    cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                    cmd.Parameters.AddWithValue("@idServerPRI", engine.IdServerPRI);
                    cmd.Parameters.AddWithValue("@ServerChannelPRI", engine.ServerChannelPRI);
                    cmd.Parameters.AddWithValue("@idServerSEC", engine.IdServerSEC);
                    cmd.Parameters.AddWithValue("@ServerChannelSEC", engine.ServerChannelSEC);
                    cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                    cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                    cmd.Parameters.AddWithValue("@idArchive", engine.IdArchive);
                    cmd.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(engine, HibernationSerializerSettings));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteEngine(IEnginePersistent engine) 
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("DELETE FROM engine WHERE idEngine=@idEngine;"))
                {
                    cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ReadRootEvents(IEngine engine)
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT * FROM rundownevent WHERE typStart in (@StartTypeManual, @StartTypeOnFixedTime, @StartTypeNone) AND idEventBinding=0 AND idEngine=@idEngine ORDER BY ScheduledTime, EventName"))
                {
                    cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                    cmd.Parameters.AddWithValue("@StartTypeManual", (byte)TStartType.Manual);
                    cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", (byte)TStartType.OnFixedTime);
                    cmd.Parameters.AddWithValue("@StartTypeNone", (byte)TStartType.None);
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var newEvent = InternalEventRead(engine, dataReader);
                            engine.AddRootEvent(newEvent);
                        }
                        dataReader.Close();
                    }
                }
                Debug.WriteLine(engine, "EventReadRootEvents read");
            }
        }

        public void SearchMissing(IEngine engine) 
        {
            {
                lock (Connection)
                {
                    using (var cmd = CreateCommand("SELECT * FROM rundownevent m WHERE m.idEngine=@idEngine AND m.typStart <= @typStart AND (SELECT s.idRundownEvent FROM rundownevent s WHERE m.idEventBinding = s.idRundownEvent) IS NULL"))
                    {
                        cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                        cmd.Parameters.AddWithValue("@typStart", (int)TStartType.Manual);
                        var foundEvents = new List<IEvent>();
                        using (var dataReader = cmd.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                if (engine.GetRootEvents().Any(e => (e as IEventPersistent)?.Id == dataReader.GetUInt64("idRundownEvent")))
                                    continue;
                                var newEvent = InternalEventRead(engine, dataReader);
                                foundEvents.Add(newEvent);
                            }
                            dataReader.Close();
                        }
                        foreach (var e in foundEvents)
                        {
                            if (e is ITemplated et && e is IEventPersistent ep)
                                ReadAnimatedEvent(ep.Id, et);
                            if (e.EventType == TEventType.Container)
                                continue;
                            if (e.EventType != TEventType.Rundown)
                            {
                                var cont = engine.CreateNewEvent(
                                    eventType: TEventType.Rundown,
                                    eventName: $"Rundown for {e.EventName}",
                                    scheduledTime: e.ScheduledTime,
                                    startType: TStartType.Manual
                                    );
                                engine.AddRootEvent(cont);
                                cont.Save();
                                cont.InsertUnder(e, false);
                            }
                            else
                            {
                                e.StartType = TStartType.Manual;
                                engine.AddRootEvent(e);
                                e.Save();
                            }
                        }
                    }
                }
            }
        }

        public List<IEvent> SearchPlaying(IEngine engine)
        {
            {
                lock (Connection)
                {
                    using (var cmd = CreateCommand("SELECT * FROM rundownevent WHERE idEngine=@idEngine AND PlayState=@PlayState"))
                    {
                        cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                        cmd.Parameters.AddWithValue("@PlayState", TPlayState.Playing);
                        var foundEvents = new List<IEvent>();
                        using (var dataReader = cmd.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                var newEvent = InternalEventRead(engine, dataReader);
                                foundEvents.Add(newEvent);
                            }
                            dataReader.Close();
                        }
                        foreach (var ev in foundEvents)
                            if (ev is ITemplated && ev is IEventPersistent persistent)
                            {
                                ReadAnimatedEvent(persistent.Id, ev as ITemplated);
                                persistent.IsModified = false;
                            }
                        return foundEvents;
                    }
                }
            }
        }

        public MediaDeleteResult MediaInUse(IEngine engine, IServerMedia serverMedia)
        {
            var reason = MediaDeleteResult.NoDeny;
            lock (Connection)
            {
#if MYSQL
                using (var cmd = CreateCommand("SELECT * from rundownevent WHERE MediaGuid=@MediaGuid AND ADDTIME(ScheduledTime, Duration) > UTC_TIMESTAMP();"))
#elif SQLITE
                using (var cmd = CreateCommand("SELECT * from rundownevent WHERE MediaGuid=@MediaGuid AND (ScheduledTime + Duration) >  datetime('now', 'utc');"))
#endif
                {
                    cmd.Parameters.AddWithValue("@MediaGuid", serverMedia.MediaGuid);
                    IEvent futureScheduled = null;
                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read())
                            futureScheduled = InternalEventRead(engine, reader);
                    if (futureScheduled is ITemplated && futureScheduled is IEventPersistent persistent)
                    {
                        ReadAnimatedEvent(persistent.Id, futureScheduled as ITemplated);
                        persistent.IsModified = false;
                    }
                    if (futureScheduled != null)
                        return new MediaDeleteResult { Result = MediaDeleteResult.MediaDeleteResultEnum.InSchedule, Media = serverMedia, Event = futureScheduled };
                }
            }
            return reason;
        }

#endregion //IEngine

#region ArchiveDirectory
        public ReadOnlyCollection<T> LoadArchiveDirectories<T>() where T : IArchiveDirectoryProperties, new()
        {
            var directories = new List<T>();
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT * FROM archive;"))
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dir = new T
                        {
                            IdArchive = dataReader.GetUInt64("idArchive"),
                            Folder = dataReader.GetString("Folder")
                        };
                        directories.Add(dir);
                    }
                    dataReader.Close();
                }
            }
            return directories.AsReadOnly();
        }

        public void InsertArchiveDirectory(IArchiveDirectoryProperties dir) 
        {
            lock (Connection)
            {
                {
                    using (var cmd = CreateCommand(@"INSERT INTO archive (Folder) VALUES (@Folder);"))
                    {
                        cmd.Parameters.AddWithValue("@Folder", dir.Folder);
                        cmd.ExecuteNonQuery();
                        dir.IdArchive = (ulong)cmd.LastInsertedId();
                    }
                }
            }
        }

        public void UpdateArchiveDirectory(IArchiveDirectoryProperties dir)
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand(@"UPDATE archive SET Folder=@Folder WHERE idArchive=@idArchive"))
                {
                    cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                    cmd.Parameters.AddWithValue("@Folder", dir.Folder);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteArchiveDirectory(IArchiveDirectoryProperties dir) 
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("DELETE FROM archive WHERE idArchive=@idArchive;"))
                {
                    cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                    cmd.ExecuteNonQuery();
                }
            }
        }

#if MYSQL
        private T ReadArchiveMedia<T>(DbDataReaderRedundant dataReader) where T: IArchiveMedia, new()
#elif SQLITE
        private T ReadArchiveMedia<T>(SQLiteDataReader dataReader) where T : IArchiveMedia, new()
#endif
        {
            var media = new T
            {
                IdPersistentMedia = dataReader.GetUInt64("idArchiveMedia")
            };
            MediaReadFields(media, dataReader);
            return media;
        }

        public List<T> ArchiveMediaSearch<T>(IArchiveDirectoryServerSide dir, TMediaCategory? mediaCategory, string search) where T: IArchiveMedia, new()
        {
            lock (Connection)
            {
                var textSearches = (from text in search.ToLower().Split(' ').Where(s => !string.IsNullOrEmpty(s)) select "(LOWER(MediaName) LIKE \"%" + text + "%\" OR LOWER(FileName) LIKE \"%" + text + "%\")").ToArray();
                using (var cmd = mediaCategory == null
                    ? CreateCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive"
                                                + (textSearches.Length > 0 ? " and" + string.Join(" AND", textSearches) : string.Empty)
                                                + " ORDER BY idArchiveMedia DESC LIMIT 0, 1000;")
                    : CreateCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive AND ((flags >> 4) & 3)=@Category"
                                                + (textSearches.Length > 0 ? " and" + string.Join(" AND", textSearches) : string.Empty)
                                                + " ORDER BY idArchiveMedia DESC LIMIT 0, 1000;")
                    )
                {
                    if (mediaCategory != null)
                        cmd.Parameters.AddWithValue("@Category", (uint)mediaCategory);
                    cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        var result = new List<T>();
                        while (dataReader.Read())
                        {
                            var media = ReadArchiveMedia<T>(dataReader);
                            dir.AddMedia(media);
                            result.Add(media);
                        }
                        dataReader.Close();
                        return result;
                    }
                }
            }
        }
        
        public List<T> FindArchivedStaleMedia<T>(IArchiveDirectoryServerSide dir) where T : IArchiveMedia, new()
        {
            var returnList = new List<T>();
            lock (Connection)
            {
                using (var cmd = CreateCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive AND KillDate<CURRENT_DATE AND KillDate>'2000-01-01' LIMIT 0, 1000;"))
                {
                    cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                            returnList.Add(ReadArchiveMedia<T>(dataReader));
                        dataReader.Close();
                    }
                }
            }
            return returnList;
        }

        public T ArchiveMediaFind<T>(IArchiveDirectoryServerSide dir, Guid mediaGuid) where T: IArchiveMedia, new()
        {
            var result = default(T);
            if (mediaGuid == Guid.Empty)
                return result;
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT * FROM archivemedia WHERE idArchive=@idArchive AND MediaGuid=@MediaGuid;"))
                {
                    cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                    cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                    using (var dataReader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (dataReader.Read())
                        {
                            result = ReadArchiveMedia<T>(dataReader);
                        }
                        dataReader.Close();
                    }
                }
            }
            return result;
        }

        public bool ArchiveContainsMedia(IArchiveDirectoryProperties dir, Guid mediaGuid)
        {
            if (dir == null || mediaGuid == Guid.Empty)
                return false;
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT count(*) FROM archivemedia WHERE idArchive=@idArchive AND MediaGuid=@MediaGuid;"))
                {
                    cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                    cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                    var result = cmd.ExecuteScalar();
                    return result != null && (long)result > 0;
                }
            }
        }

#endregion // ArchiveDirectory

#region IEvent
        public List<IEvent> ReadSubEvents(IEngine engine, IEventPersistent eventOwner)
        {
            if (eventOwner == null)
                return null;
            lock (Connection)
            {
                using (var cmd = eventOwner.EventType == TEventType.Container
                    ? CreateCommand("SELECT * FROM rundownevent WHERE idEventBinding = @idEventBinding AND (typStart=@StartTypeManual OR typStart=@StartTypeOnFixedTime);")
                    : CreateCommand("SELECT * FROM rundownevent WHERE idEventBinding = @idEventBinding AND typStart IN (@StartTypeWithParent, @StartTypeWithParentFromEnd);"))
                {
                    if (eventOwner.EventType == TEventType.Container)
                    {
                        cmd.Parameters.AddWithValue("@StartTypeManual", TStartType.Manual);
                        cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", TStartType.OnFixedTime);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@StartTypeWithParent", TStartType.WithParent);
                        cmd.Parameters.AddWithValue("@StartTypeWithParentFromEnd", TStartType.WithParentFromEnd);
                    }
                    cmd.Parameters.AddWithValue("@idEventBinding", eventOwner.Id);
                    var subevents = new List<IEvent>();
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                            subevents.Add(InternalEventRead(engine, dataReader));
                    }
                    foreach (IEventPersistent e in subevents.Cast<IEventPersistent>())
                        if (e is ITemplated templated)
                        {
                            ReadAnimatedEvent(e.Id, templated);
                            e.IsModified = false;
                        }
                    return subevents;
                }
            }
        }

        public IEvent ReadNext(IEngine engine, IEventPersistent aEvent) 
        {
            if (aEvent == null)
                return null;
            lock (Connection)
            {
                IEvent next = null;
                using (var cmd = CreateCommand("SELECT * FROM rundownevent WHERE idEventBinding = @idEventBinding AND typStart=@StartType;"))
                {
                    cmd.Parameters.AddWithValue("@idEventBinding", aEvent.Id);
                    cmd.Parameters.AddWithValue("@StartType", TStartType.After);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            next = InternalEventRead(engine, reader);
                    }
                }
                if (!(next is ITemplated) || !(next is IEventPersistent))
                    return next;
                ReadAnimatedEvent(((IEventPersistent)next).Id, next as ITemplated);
                ((IEventPersistent)next).IsModified = false;
                return next;
            }
        }

        private void ReadAnimatedEvent(ulong id, ITemplated animatedEvent)
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT * FROM rundownevent_templated WHERE idrundownevent_templated = @id;"))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return;
                        animatedEvent.Method = (TemplateMethod)reader.GetByte("Method");
                        animatedEvent.TemplateLayer = reader.GetInt16("TemplateLayer");
                        var templateFields = reader.GetString("Fields");
                        if (string.IsNullOrWhiteSpace(templateFields))
                            return;
                        var fieldsDeserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(templateFields);
                        if (fieldsDeserialized != null)
                            animatedEvent.Fields = fieldsDeserialized;
                    }
                }
            }
        }

        public IEvent ReadEvent(IEngine engine, ulong idRundownEvent)
        {
            if (idRundownEvent <= 0)
                return null;
            lock (Connection)
            {
                IEvent result = null;
                using (var cmd = CreateCommand("SELECT * FROM rundownevent WHERE idRundownEvent = @idRundownEvent"))
                {
                    cmd.Parameters.AddWithValue("@idRundownEvent", idRundownEvent);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            result = InternalEventRead(engine, reader);
                    }
                }
                if (!(result is ITemplated) || !(result is IEventPersistent))
                    return result;
                ReadAnimatedEvent(((IEventPersistent)result).Id, result as ITemplated);
                ((IEventPersistent)result).IsModified = false;
                return result;
            }
        }

#if MYSQL
        private IEvent InternalEventRead(IEngine engine, DbDataReaderRedundant dataReader)
#elif SQLITE
        private IEvent InternalEventRead(IEngine engine, SQLiteDataReader dataReader)
#endif
        {
            var flags = dataReader.IsDBNull("flagsEvent") ? 0 : dataReader.GetUInt32("flagsEvent");
            var transitionType = dataReader.GetUInt16("typTransition");
            var eventType = (TEventType)dataReader.GetByte("typEvent");
            var newEvent = engine.CreateNewEvent(
                dataReader.GetUInt64("idRundownEvent"),
                dataReader.GetUInt64("idEventBinding"),
                (VideoLayer)dataReader.GetSByte("Layer"),
                eventType,
                (TStartType)dataReader.GetByte("typStart"),
                (TPlayState)dataReader.GetByte("PlayState"),
                dataReader.GetDateTime("ScheduledTime"),
                dataReader.GetTimeSpan("Duration"),
                dataReader.GetTimeSpan("ScheduledDelay"),
                dataReader.GetTimeSpan("ScheduledTC"),
                dataReader.GetGuid("MediaGuid"),
                dataReader.GetString("EventName"),
                dataReader.GetDateTime("StartTime"),
                dataReader.GetTimeSpan("StartTC"),
                dataReader.IsDBNull("RequestedStartTime") ? null : (TimeSpan?)dataReader.GetTimeSpan("RequestedStartTime"),
                dataReader.GetTimeSpan("TransitionTime"),
                dataReader.GetTimeSpan("TransitionPauseTime"),
                (TTransitionType)(transitionType & 0xFF),
                (TEasing)(transitionType >> 8),
                dataReader.IsDBNull("AudioVolume") ? null : (double?)dataReader.GetDouble("AudioVolume"),
                dataReader.GetUInt64("idProgramme"),
                dataReader.GetString("IdAux"),
                flags.IsEnabled(),
                flags.IsHold(),
                flags.IsLoop(),
                flags.IsCGEnabled(),
                flags.Crawl(),
                flags.Logo(),
                flags.Parental(),
                flags.AutoStartFlags(),
                dataReader.GetString("Commands"), 
                routerPort: dataReader.IsDBNull("RouterPort") ? (short)-1 : dataReader.GetInt16("RouterPort"),
                recordingInfo: dataReader.IsDBNull("RecordingInfo") ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<RecordingInfo>(dataReader.GetString("RecordingInfo"))
                );
            return newEvent;
        }


#if MYSQL
        private bool EventFillParamsAndExecute(DbCommandRedundant cmd, IEventPersistent aEvent)
#elif SQLITE
        private bool EventFillParamsAndExecute(SQLiteCommand cmd, IEventPersistent aEvent)
#endif
        {
            Debug.WriteLineIf(aEvent.Duration.Days > 1, aEvent, "Duration extremely long");
            cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)aEvent.Engine).Id);
            cmd.Parameters.AddWithValue("@idEventBinding", aEvent.IdEventBinding);
            cmd.Parameters.AddWithValue("@Layer", (sbyte)aEvent.Layer);
            cmd.Parameters.AddWithValue("@typEvent", aEvent.EventType);
            cmd.Parameters.AddWithValue("@typStart", aEvent.StartType);
#if MYSQL
            if (aEvent.ScheduledTime < MinMySqlDate || aEvent.ScheduledTime > MaxMySqlDate)
            {
                cmd.Parameters.AddWithValue("@ScheduledTime", DBNull.Value);
            }
            else
                cmd.Parameters.AddWithValue("@ScheduledTime", aEvent.ScheduledTime);
            cmd.Parameters.AddWithValue("@Duration", aEvent.Duration);
            cmd.Parameters.AddWithValue("@ScheduledDelay", aEvent.ScheduledDelay);
            if (aEvent.StartTime < MinMySqlDate || aEvent.StartTime > MaxMySqlDate)
                cmd.Parameters.AddWithValue("@StartTime", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@StartTime", aEvent.StartTime);
            cmd.Parameters.AddWithValue("@TransitionTime", aEvent.TransitionTime);
            cmd.Parameters.AddWithValue("@TransitionPauseTime", aEvent.TransitionPauseTime);
#elif SQLITE
            cmd.Parameters.AddWithValue("@ScheduledTime", aEvent.ScheduledTime.Ticks);
            cmd.Parameters.AddWithValue("@Duration", aEvent.Duration.Ticks);
            cmd.Parameters.AddWithValue("@ScheduledDelay", aEvent.ScheduledDelay.Ticks);
            if (aEvent.StartTime == default)
                cmd.Parameters.AddWithValue("@StartTime", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@StartTime", aEvent.StartTime.Ticks);
            cmd.Parameters.AddWithValue("@TransitionTime", aEvent.TransitionTime.Ticks);
            cmd.Parameters.AddWithValue("@TransitionPauseTime", aEvent.TransitionPauseTime.Ticks);
#endif
            if (aEvent.ScheduledTc == TimeSpan.Zero)
                cmd.Parameters.AddWithValue("@ScheduledTC", DBNull.Value);
            else
#if MYSQL
                cmd.Parameters.AddWithValue("@ScheduledTC", aEvent.ScheduledTc);
#elif SQLITE
                cmd.Parameters.AddWithValue("@ScheduledTC", aEvent.ScheduledTc.Ticks);
#endif
            if (aEvent.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", aEvent.MediaGuid);
            cmd.Parameters.AddWithValue("@EventName", TrimText("rundownevent", "EventName", aEvent.EventName));
            cmd.Parameters.AddWithValue("@PlayState", aEvent.PlayState);
            if (aEvent.StartTc == TimeSpan.Zero)
                cmd.Parameters.AddWithValue("@StartTC", DBNull.Value);
            else
#if MYSQL
                cmd.Parameters.AddWithValue("@StartTC", aEvent.StartTc);
#elif SQLITE
                cmd.Parameters.AddWithValue("@StartTC", aEvent.StartTc.Ticks);
#endif
            if (aEvent.RequestedStartTime == null)
                cmd.Parameters.AddWithValue("@RequestedStartTime", DBNull.Value);
            else
#if MYSQL
                cmd.Parameters.AddWithValue("@RequestedStartTime", aEvent.RequestedStartTime.Value);
#elif SQLITE
                cmd.Parameters.AddWithValue("@RequestedStartTime", aEvent.RequestedStartTime.Value.Ticks);
#endif
            cmd.Parameters.AddWithValue("@typTransition", (ushort)aEvent.TransitionType | ((ushort)aEvent.TransitionEasing)<<8);
            cmd.Parameters.AddWithValue("@idProgramme", aEvent.IdProgramme);
            if (aEvent.AudioVolume == null)
                cmd.Parameters.AddWithValue("@AudioVolume", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@AudioVolume", aEvent.AudioVolume);
            cmd.Parameters.AddWithValue("@flagsEvent", aEvent.ToFlags());
            cmd.Parameters.AddWithValue("@RouterPort", aEvent.RouterPort == -1 ? (object) DBNull.Value : aEvent.RouterPort);
            cmd.Parameters.AddWithValue("@RecordingInfo", aEvent.RecordingInfo == null ? (object)DBNull.Value : Newtonsoft.Json.JsonConvert.SerializeObject(aEvent.RecordingInfo));

            var command = aEvent.EventType == TEventType.CommandScript && aEvent is ICommandScript
                ? (object)((ICommandScript) aEvent).Command
                : DBNull.Value;
            cmd.Parameters.AddWithValue("@Commands", command);
            return cmd.ExecuteNonQuery() == 1;
        }

        private void EventAnimatedSave(ulong id,  ITemplated e, bool inserting)
        {
            var query = inserting ?
                @"INSERT INTO rundownevent_templated (idrundownevent_templated, Method, TemplateLayer, Fields) VALUES (@idrundownevent_templated, @Method, @TemplateLayer, @Fields);" :
                @"UPDATE rundownevent_templated SET  Method=@Method, TemplateLayer=@TemplateLayer, Fields=@Fields WHERE idrundownevent_templated=@idrundownevent_templated;";
            using (var cmd = CreateCommand(query))
            {
                cmd.Parameters.AddWithValue("@idrundownevent_templated", id);
                cmd.Parameters.AddWithValue("@Method", (byte)e.Method);
                cmd.Parameters.AddWithValue("@TemplateLayer", e.TemplateLayer);
                var fields = Newtonsoft.Json.JsonConvert.SerializeObject(e.Fields);
                cmd.Parameters.AddWithValue("@Fields", fields);
                cmd.ExecuteNonQuery();
            }
        }


        public bool InsertEvent(IEventPersistent aEvent)
        {
            lock (Connection)
            {
                using (var transaction = BeginTransaction())
                {
                    const string query = @"INSERT INTO rundownevent 
(idEngine, idEventBinding, Layer, typEvent, typStart, ScheduledTime, ScheduledDelay, Duration, ScheduledTC, MediaGuid, EventName, PlayState, StartTime, StartTC, RequestedStartTime, TransitionTime, TransitionPauseTime, typTransition, AudioVolume, idProgramme, flagsEvent, Commands, RouterPort, RecordingInfo) 
VALUES 
(@idEngine, @idEventBinding, @Layer, @typEvent, @typStart, @ScheduledTime, @ScheduledDelay, @Duration, @ScheduledTC, @MediaGuid, @EventName, @PlayState, @StartTime, @StartTC, @RequestedStartTime, @TransitionTime, @TransitionPauseTime, @typTransition, @AudioVolume, @idProgramme, @flagsEvent, @Commands, @RouterPort, @RecordingInfo);";
                    using (var cmd = CreateCommand(query))
                        if (EventFillParamsAndExecute(cmd, aEvent))
                        {
                            aEvent.Id = (ulong)cmd.LastInsertedId();
                            Debug.WriteLine("DbInsertEvent Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                            if (aEvent is ITemplated eventTemplated)
                                EventAnimatedSave(aEvent.Id, eventTemplated, true);
                            transaction.Commit();
                            return true;
                        }
                }
            }
            return false;
        }

        public bool UpdateEvent<TEvent>(TEvent aEvent) where  TEvent: IEventPersistent
        {
            lock (Connection)
            {
                using (var transaction = BeginTransaction())
                {
                    const string query = @"UPDATE rundownevent 
SET 
idEngine=@idEngine, 
idEventBinding=@idEventBinding, 
Layer=@Layer, 
typEvent=@typEvent, 
typStart=@typStart, 
ScheduledTime=@ScheduledTime, 
ScheduledDelay=@ScheduledDelay, 
ScheduledTC=@ScheduledTC,
Duration=@Duration, 
MediaGuid=@MediaGuid, 
EventName=@EventName, 
PlayState=@PlayState, 
StartTime=@StartTime, 
StartTC=@StartTC,
RequestedStartTime=@RequestedStartTime,
TransitionTime=@TransitionTime, 
TransitionPauseTime=@TransitionPauseTime, 
typTransition=@typTransition, 
AudioVolume=@AudioVolume, 
idProgramme=@idProgramme, 
flagsEvent=@flagsEvent,
Commands=@Commands,
RouterPort=@RouterPort,
RecordingInfo=@RecordingInfo
WHERE idRundownEvent=@idRundownEvent;";
                    using (var cmd = CreateCommand(query))
                    {
                        cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                        if (!EventFillParamsAndExecute(cmd, aEvent))
                            return false;
                        Debug.WriteLine("DbUpdateEvent Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                        if (aEvent is ITemplated eventTemplated)
                            EventAnimatedSave(aEvent.Id, eventTemplated, false);
                        transaction.Commit();
                        return true;
                    }
                }
            }
        }

        public bool DeleteEvent(IEventPersistent aEvent)
        {
            lock (Connection)
            {
                using (var transaction = BeginTransaction())
                {
                    using (var cmd = CreateCommand("DELETE FROM rundownevent WHERE idRundownEvent=@idRundownEvent;"))
                    {
                        cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                        cmd.ExecuteNonQuery();
                    }
                    Debug.WriteLine("DbDeleteEvent Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                    if (aEvent is ITemplated eventTemplated)
                        using (var cmd = CreateCommand("DELETE FROM rundownevent_templated WHERE idrundownevent_templated=@idRundownEvent;"))
                        {
                            cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                            cmd.ExecuteNonQuery();
                        }
                    transaction.Commit();
                    return true;
                }
            }
        }

        public void AsRunLogWrite(ulong idEngine, IEvent e)
        {
            try
            {
                lock (Connection)
                {
                    using (var cmd = CreateCommand(
@"INSERT INTO asrunlog (
idEngine,
ExecuteTime, 
MediaName, 
StartTC,
Duration,
idProgramme, 
idAuxMedia, 
idAuxRundown, 
SecEvents, 
typVideo, 
typAudio,
Flags
)
VALUES
(
@idEngine,
@ExecuteTime, 
@MediaName, 
@StartTC,
@Duration,
@idProgramme, 
@idAuxMedia, 
@idAuxRundown, 
@SecEvents, 
@typVideo, 
@typAudio,
@Flags
);"))
                    {
                        cmd.Parameters.AddWithValue("@idEngine", idEngine);
#if MYSQL
                        cmd.Parameters.AddWithValue("@ExecuteTime", e.StartTime);
                        cmd.Parameters.AddWithValue("@StartTC", e.StartTc);
                        cmd.Parameters.AddWithValue("@Duration", e.Duration);
#elif SQLITE
                        cmd.Parameters.AddWithValue("@ExecuteTime", e.StartTime.Ticks);
                        cmd.Parameters.AddWithValue("@StartTC", e.StartTc.Ticks);
                        cmd.Parameters.AddWithValue("@Duration", e.Duration.Ticks);
#endif
                        var media = e.Media;
                        if (media != null)
                        {
                            cmd.Parameters.AddWithValue("@MediaName", TrimText("asrunlog", "MediaName", media.MediaName));
                            if (media is IPersistentMedia)
                                cmd.Parameters.AddWithValue("@idAuxMedia", TrimText("asrunlog", "idAuxMedia", (media as IPersistentMedia).IdAux));
                            else
                                cmd.Parameters.AddWithValue("@idAuxMedia", DBNull.Value);
                            cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
                            cmd.Parameters.AddWithValue("@typAudio", (byte)media.AudioChannelMapping);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@MediaName", TrimText("asrunlog", "MediaName", e.EventName));
                            cmd.Parameters.AddWithValue("@idAuxMedia", DBNull.Value);
                            cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
                            cmd.Parameters.AddWithValue("@typAudio", DBNull.Value);
                        }
                        cmd.Parameters.AddWithValue("@idProgramme", e.IdProgramme);
                        cmd.Parameters.AddWithValue("@idAuxRundown", TrimText("asrunlog", "idAuxRundown", e.IdAux));
                        cmd.Parameters.AddWithValue("@SecEvents", TrimText("asrunlog", "SecEvents", string.Join(";", e.GetSubEvents().Select(se => se.EventName))));
                        cmd.Parameters.AddWithValue("@Flags", e.ToFlags());
                        cmd.ExecuteNonQuery();
                    }
                }
                Debug.WriteLine(e, "AsRunLog written for");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

#endregion // IEvent

#region ACL

        public List<IAclRight> ReadEventAclList<TEventAcl>(IEventPersistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl: IAclRight, IPersistent, new()
        {
            if (aEvent == null)
                return null;
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT * FROM rundownevent_acl WHERE idRundownEvent = @idRundownEvent;"))
                {
                    cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                    var acl = new List<IAclRight>();
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var item = new TEventAcl
                            {
                                Id = dataReader.GetUInt64("idRundownevent_ACL"),
                                Owner = aEvent,
                                SecurityObject = authenticationService.FindSecurityObject(dataReader.GetUInt64("idACO")),
                                Acl = dataReader.GetUInt64("ACL")
                            };
                            acl.Add(item);
                        }
                    }
                    return acl;
                }
            }
        }

        public bool InsertEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl: IAclRight, IPersistent
        {
            if (acl?.Owner == null)
                return false;
            lock (Connection)
            {
                using (var cmd =
                    CreateCommand("INSERT INTO rundownevent_acl (idRundownEvent, idACO, ACL) VALUES (@idRundownEvent, @idACO, @ACL);"))
                {
                    cmd.Parameters.AddWithValue("@idRundownEvent", acl.Owner.Id);
                    cmd.Parameters.AddWithValue("@idACO", acl.SecurityObject.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    if (cmd.ExecuteNonQuery() == 1)
                        acl.Id = (ulong) cmd.LastInsertedId();
                    return true;
                }
            }
        }

        public bool UpdateEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl: IAclRight, IPersistent
        {
            lock (Connection)
            {
                using (var cmd =
                    CreateCommand("UPDATE rundownevent_acl SET ACL=@ACL WHERE idRundownevent_ACL=@idRundownevent_ACL;"))
                {
                    cmd.Parameters.AddWithValue("@idRundownevent_ACL", acl.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        public bool DeleteEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (Connection)
            {
                var query = "DELETE FROM rundownevent_acl WHERE idRundownevent_ACL=@idRundownevent_ACL;";
                using (var cmd = CreateCommand(query))
                {
                    cmd.Parameters.AddWithValue("@idRundownevent_ACL", acl.Id);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }


        public List<IAclRight> ReadEngineAclList<TEngineAcl>(IPersistent engine, IAuthenticationServicePersitency authenticationService) where TEngineAcl : IAclRight, IPersistent, new()
        {
            lock (Connection)
            {
                var cmd =
                    CreateCommand("SELECT * FROM engine_acl WHERE idEngine=@idEngine;");
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                var acl = new List<IAclRight>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var item = new TEngineAcl
                        {
                            Id = dataReader.GetUInt64("idEngine_ACL"),
                            Owner = engine,
                            SecurityObject = authenticationService.FindSecurityObject(dataReader.GetUInt64("idACO")),
                            Acl = dataReader.GetUInt64("ACL")
                        };
                        acl.Add(item);
                    }
                }
                return acl;
            }
        }

        public bool InsertEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            if (acl?.Owner == null)
                return false;
            lock (Connection)
            {
                using (var cmd = CreateCommand("INSERT INTO engine_acl (idEngine, idACO, ACL) VALUES (@idEngine, @idACO, @ACL);"))
                {
                    cmd.Parameters.AddWithValue("@idEngine", acl.Owner.Id);
                    cmd.Parameters.AddWithValue("@idACO", acl.SecurityObject.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    if (cmd.ExecuteNonQuery() == 1)
                        acl.Id = (ulong)cmd.LastInsertedId();
                    return true;
                }
            }
        }

        public bool UpdateEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("UPDATE engine_acl SET ACL=@ACL WHERE idEngine_ACL=@idEngine_ACL;"))
                {
                    cmd.Parameters.AddWithValue("@idEngine_ACL", acl.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        public bool DeleteEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("DELETE FROM engine_acl WHERE idEngine_ACL=@idEngine_ACL;"))
                {
                    cmd.Parameters.AddWithValue("@idEngine_ACL", acl.Id);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

#endregion //ACL

#region Media
#if MYSQL
        private bool MediaFillParamsAndExecute(DbCommandRedundant cmd, string tableName, IPersistentMedia media, ulong serverId)
#elif SQLITE
        private bool MediaFillParamsAndExecute(SQLiteCommand cmd, string tableName, IPersistentMedia media, ulong serverId)
#endif
        {
            cmd.Parameters.AddWithValue("@idProgramme", media.IdProgramme);
            cmd.Parameters.AddWithValue("@idAux", TrimText(tableName, "idAux", media.IdAux));
            if (media.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
            if (media.KillDate == default)
                cmd.Parameters.AddWithValue("@KillDate", DBNull.Value);
            else
#if MYSQL
                cmd.Parameters.AddWithValue("@KillDate", media.KillDate);
#elif SQLITE
                cmd.Parameters.AddWithValue("@KillDate", media.KillDate.Ticks);
#endif
            var serverMedia = media as IServerMedia;
            var flags = ((serverMedia?.DoNotArchive == true) ? 0x1 : (uint)0x0)
                        | (media.IsProtected ? 0x2 : (uint)0x0)
                        | (media.FieldOrderInverted ? 0x4 : (uint)0x0)
                        | ((uint)media.MediaCategory << 4) // bits 4-7 of 1st byte
                        | ((uint)media.MediaEmphasis << 8) // bits 1-3 of second byte
                        | ((uint)media.Parental << 12) // bits 4-7 of second byte
                        ;
            cmd.Parameters.AddWithValue("@flags", flags);
            if (serverMedia?.Directory is IServerDirectory serverDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typVideo", (byte)serverMedia.VideoFormat);
                if (serverMedia.LastPlayed != default)
#if MYSQL
                    cmd.Parameters.AddWithValue("@LastPlayed", serverMedia.LastPlayed);
#elif SQLITE
                    cmd.Parameters.AddWithValue("@LastPlayed", serverMedia.LastPlayed.Ticks);
#endif
                else
                    cmd.Parameters.AddWithValue("@LastPlayed", DBNull.Value);
            } 
            if (media is IAnimatedMedia && media.Directory is IAnimationDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
                cmd.Parameters.AddWithValue("@LastPlayed", DBNull.Value);
            }
            if (media is IArchiveMedia && media.Directory is IArchiveDirectoryServerSide archiveDirectory)
            {
                cmd.Parameters.AddWithValue("@idArchive", archiveDirectory.IdArchive);
                cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
            }
            cmd.Parameters.AddWithValue("@MediaName", TrimText(tableName, "MediaName", media.MediaName));
#if MYSQL
            cmd.Parameters.AddWithValue("@Duration", media.Duration);
            cmd.Parameters.AddWithValue("@DurationPlay", media.DurationPlay);
            cmd.Parameters.AddWithValue("@TCStart", media.TcStart);
            cmd.Parameters.AddWithValue("@TCPlay", media.TcPlay);
#elif SQLITE
            cmd.Parameters.AddWithValue("@Duration", media.Duration.Ticks);
            cmd.Parameters.AddWithValue("@DurationPlay", media.DurationPlay.Ticks);
            cmd.Parameters.AddWithValue("@TCStart", media.TcStart.Ticks);
            cmd.Parameters.AddWithValue("@TCPlay", media.TcPlay.Ticks);
#endif
            cmd.Parameters.AddWithValue("@Folder", media.Folder);
            cmd.Parameters.AddWithValue("@FileSize", media.FileSize);
            cmd.Parameters.AddWithValue("@FileName", media.FileName);
            if (media.LastUpdated == default)
                cmd.Parameters.AddWithValue("@LastUpdated", DBNull.Value);
            else
#if MYSQL
                cmd.Parameters.AddWithValue("@LastUpdated", media.LastUpdated);
#elif SQLITE
                cmd.Parameters.AddWithValue("@LastUpdated", media.LastUpdated.Ticks);
#endif
            cmd.Parameters.AddWithValue("@statusMedia", (int)media.MediaStatus);
            cmd.Parameters.AddWithValue("@typMedia", (int)media.MediaType);
            cmd.Parameters.AddWithValue("@typAudio", (byte)media.AudioChannelMapping);
            cmd.Parameters.AddWithValue("@AudioVolume", media.AudioVolume);
            cmd.Parameters.AddWithValue("@AudioLevelIntegrated", media.AudioLevelIntegrated);
            cmd.Parameters.AddWithValue("@AudioLevelPeak", media.AudioLevelPeak);
            try
            {
                return cmd.ExecuteNonQuery() == 1;
            }
            catch (Exception e)
            {
                Debug.WriteLine(media, e.Message); 
            }
            return false;
        }

#if MYSQL
        private void MediaReadFields(IPersistentMedia media, DbDataReaderRedundant dataReader)
#elif SQLITE
        private void MediaReadFields(IPersistentMedia media, SQLiteDataReader dataReader)
#endif
        {
            var flags = dataReader.IsDBNull("flags") ? 0 : dataReader.GetUInt32("flags");
            media.DisableIsModified();
            try
            {
                media.MediaName = dataReader.IsDBNull("MediaName") ? string.Empty : dataReader.GetString("MediaName");
                media.LastUpdated = dataReader.GetDateTime("LastUpdated");
                media.MediaGuid = dataReader.GetGuid("MediaGuid");
                media.MediaType = (TMediaType)(dataReader.IsDBNull("typMedia") ? 0 : dataReader.GetInt32("typMedia"));
                media.Duration = dataReader.IsDBNull("Duration") ? default : dataReader.GetTimeSpan("Duration");
                media.DurationPlay = dataReader.IsDBNull("DurationPlay") ? default : dataReader.GetTimeSpan("DurationPlay");
                media.Folder = dataReader.IsDBNull("Folder") ? string.Empty : dataReader.GetString("Folder");
                media.FileName = dataReader.IsDBNull("FileName") ? string.Empty : dataReader.GetString("FileName");
                media.FileSize = dataReader.IsDBNull("FileSize") ? 0 : dataReader.GetUInt64("FileSize");
                media.MediaStatus = (TMediaStatus)(dataReader.IsDBNull("statusMedia") ? 0 : dataReader.GetInt32("statusMedia"));
                media.TcStart = dataReader.IsDBNull("TCStart") ? default : dataReader.GetTimeSpan("TCStart");
                media.TcPlay = dataReader.IsDBNull("TCPlay") ? default : dataReader.GetTimeSpan("TCPlay");
                media.IdProgramme = dataReader.IsDBNull("idProgramme") ? 0 : dataReader.GetUInt64("idProgramme");
                media.AudioVolume = dataReader.IsDBNull("AudioVolume") ? 0 : dataReader.GetDouble("AudioVolume");
                media.AudioLevelIntegrated = dataReader.IsDBNull("AudioLevelIntegrated") ? 0 : dataReader.GetDouble("AudioLevelIntegrated");
                media.AudioLevelPeak = dataReader.IsDBNull("AudioLevelPeak") ? 0 : dataReader.GetDouble("AudioLevelPeak");
                media.AudioChannelMapping = dataReader.IsDBNull("typAudio") ? TAudioChannelMapping.Stereo : (TAudioChannelMapping)dataReader.GetByte("typAudio");
                media.VideoFormat = dataReader.IsDBNull("typVideo") ? TVideoFormat.Other : (TVideoFormat)dataReader.GetByte("typVideo");
                media.IdAux = dataReader.IsDBNull("idAux") ? string.Empty : dataReader.GetString("idAux");
                media.KillDate = dataReader.IsDBNull("KillDate") ? default : dataReader.GetDateTime("KillDate");
                media.MediaEmphasis = (TMediaEmphasis)((flags >> 8) & 0xF);
                media.Parental = (byte)((flags >> 12) & 0xF);
                if (media is IServerMedia serverMedia)
                {
                    serverMedia.DoNotArchive = (flags & 0x1) != 0;
                    serverMedia.LastPlayed = dataReader.IsDBNull("LastPlayed") ? default : dataReader.GetDateTime("LastPlayed");
                }
                media.IsProtected = (flags & 0x2) != 0;
                media.FieldOrderInverted = (flags & 0x4) != 0;
                media.MediaCategory = (TMediaCategory)((flags >> 4) & 0xF); // bits 4-7 of 1st byte
                if (media is ITemplated templated)
                {
                    var templateFields = dataReader.GetString("Fields");
                    if (!string.IsNullOrWhiteSpace(templateFields))
                    {
                        var fieldsDeserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(templateFields);
                        if (fieldsDeserialized != null)
                            templated.Fields = fieldsDeserialized;
                    }
                    templated.Method = (TemplateMethod)dataReader.GetByte("Method");
                    templated.TemplateLayer = dataReader.GetInt32("TemplateLayer");
                    templated.ScheduledDelay = dataReader.GetTimeSpan("ScheduledDelay");
                    templated.StartType = (TStartType)dataReader.GetByte("StartType");
                    if (templated.StartType != TStartType.WithParentFromEnd)
                        templated.StartType = TStartType.WithParent;
                }
            }
            finally
            {
                media.EnableIsModified();
            }
        }

        public void LoadAnimationDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IAnimatedMedia, new()
        {
            Debug.WriteLine(directory, "AnimationDirectory load started");
            lock (Connection)
            {
                using (var cmd = CreateCommand("SELECT servermedia.*, media_templated.Fields, media_templated.Method, media_templated.TemplateLayer, media_templated.ScheduledDelay, media_templated.StartType FROM serverMedia LEFT JOIN media_templated ON servermedia.MediaGuid = media_templated.MediaGuid WHERE idServer=@idServer AND typMedia = @typMedia"))
                {
                    cmd.Parameters.AddWithValue("@idServer", serverId);
                    cmd.Parameters.AddWithValue("@typMedia", TMediaType.Animation);
                    try
                    {
                        using (var dataReader = cmd.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {

                                var media = new T
                                {
                                    IdPersistentMedia = dataReader.GetUInt64("idServerMedia"),
                                };
                                MediaReadFields(media, dataReader);
                                directory.AddMedia(media);
                            }
                        }
                        Debug.WriteLine(directory, "Directory loaded");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(directory, e.Message);
                    }
                }
            }
        }

        public void LoadServerDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IServerMedia, new()
        {
            Debug.WriteLine(directory, "ServerLoadMediaDirectory started");
            lock (Connection)
            {

                using (var cmd = CreateCommand("SELECT * FROM servermedia WHERE idServer=@idServer AND typMedia IN (@typMediaMovie, @typMediaStill)"))
                {
                    cmd.Parameters.AddWithValue("@idServer", serverId);
                    cmd.Parameters.AddWithValue("@typMediaMovie", TMediaType.Movie);
                    cmd.Parameters.AddWithValue("@typMediaStill", TMediaType.Still);
                    try
                    {
                        using (var dataReader = cmd.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                var media = new T
                                {
                                    IdPersistentMedia = dataReader.GetUInt64("idServerMedia")
                                };
                                MediaReadFields(media, dataReader);
                                directory.AddMedia(media);
                            }
                        }
                        Debug.WriteLine(directory, "Directory loaded");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(directory, e.Message);
                    }
                }
            }
        }

        private bool Insert_media_templated(IAnimatedMedia media)
        {
            try
            {
#if MYSQL
                using (var cmd = CreateCommand(@"INSERT IGNORE INTO media_templated (MediaGuid, Fields, TemplateLayer, Method, ScheduledDelay, StartType) VALUES (@MediaGuid, @Fields, @TemplateLayer, @Method, @ScheduledDelay, @StartType);"))
#elif SQLITE
                using (var cmd = CreateCommand(@"INSERT OR IGNORE INTO media_templated (MediaGuid, Fields, TemplateLayer, Method, ScheduledDelay, StartType) VALUES (@MediaGuid, @Fields, @TemplateLayer, @Method, @ScheduledDelay, @StartType);"))
#endif
                {
                    MediaTemplatedFillParametersAndExecute(cmd, media);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Insert_media_templated failed with {e.Message}");
                return false;
            }
        }

        private void Update_media_templated(IAnimatedMedia media)
        {
            try
            {
                using (var cmd = CreateCommand(@"UPDATE media_templated SET Fields=@Fields, TemplateLayer=@TemplateLayer, ScheduledDelay=@ScheduledDelay, StartType=@StartType, Method=@Method WHERE MediaGuid = @MediaGuid;"))
                {
                    MediaTemplatedFillParametersAndExecute(cmd, media);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"_update_media_templated failed with {e.Message}");
            }
        }

#if MYSQL
        private void MediaTemplatedFillParametersAndExecute(DbCommandRedundant cmd, IAnimatedMedia media) 
#elif SQLITE
        private void MediaTemplatedFillParametersAndExecute(SQLiteCommand cmd, IAnimatedMedia media)
#endif
        {
            cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
            cmd.Parameters.AddWithValue("@TemplateLayer", media.TemplateLayer);
            cmd.Parameters.AddWithValue("@Method", (byte)media.Method);
#if MYSQL
            cmd.Parameters.AddWithValue("@ScheduledDelay", media.ScheduledDelay);
#elif SQLITE
            cmd.Parameters.AddWithValue("@ScheduledDelay", media.ScheduledDelay.Ticks);
#endif
            cmd.Parameters.AddWithValue("@StartType", (byte)media.StartType);
            cmd.Parameters.AddWithValue("@Fields", Newtonsoft.Json.JsonConvert.SerializeObject(media.Fields));
            cmd.ExecuteNonQuery();
        }

        private bool Delete_media_templated(IAnimatedMedia media)
        {
            try
            {
                using (var cmd = CreateCommand(@"DELETE FROM media_templated WHERE MediaGuid = @MediaGuid;"))
                {
                    cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"_delete_media_templated failed with {e.Message}");
                return false;
            }
        }

        public void InsertMedia(IAnimatedMedia animatedMedia, ulong serverId )
        {
            var result = false;
            lock (Connection)
            {
                using (var transaction = BeginTransaction())
                {
                    try
                    {
                        result = DbInsertMedia(animatedMedia, serverId)
                            && Insert_media_templated(animatedMedia);
                    }
                    finally
                    {
                        if (result)
                            transaction.Commit();
                        else
                            transaction.Rollback();
                    }
                }
            }
        }

        public void InsertMedia(IServerMedia serverMedia, ulong serverId)
        {
            lock (Connection)
                DbInsertMedia(serverMedia, serverId);
        }

        private bool DbInsertMedia(IPersistentMedia media, ulong serverId)
        {
            using (var cmd = CreateCommand(@"INSERT INTO servermedia 
(idServer, MediaName, Folder, FileName, FileSize, LastUpdated, LastPlayed, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idServer, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @LastPlayed, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);"))
            {
                var result = MediaFillParamsAndExecute(cmd, "servermedia", media, serverId);
                media.IdPersistentMedia = (ulong)cmd.LastInsertedId();
                Debug.WriteLine(media, "ServerMediaInserte-d");
                return result;
            }
        }

        public void InsertMedia(IArchiveMedia archiveMedia, ulong serverid)
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand(@"INSERT INTO archivemedia 
(idArchive, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idArchive, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);"))
                {
                    MediaFillParamsAndExecute(cmd, "archivemedia", archiveMedia, serverid);
                    archiveMedia.IdPersistentMedia = (ulong)cmd.LastInsertedId();
                }
            }
        }

        public bool DeleteMedia(IServerMedia serverMedia)
        {
            lock (Connection)
            {
                return DeleteServerMedia(serverMedia);
            }
        }

        public bool DeleteMedia(IAnimatedMedia animatedMedia)
        {
            lock (Connection)
            {
                var result = false;
                using (var transaction = BeginTransaction())
                {
                    try
                    {
                        result = DeleteServerMedia(animatedMedia);
                        if (result)
                            result = Delete_media_templated(animatedMedia);
                    }
                    finally
                    {
                        if (result)
                            transaction.Commit();
                        else
                            transaction.Rollback();
                    }
                }
                return result;
            }
        }
        
        private bool DeleteServerMedia(IPersistentMedia serverMedia)
        {
            using (var cmd = CreateCommand("DELETE FROM servermedia WHERE idServerMedia=@idServerMedia;"))
            {
                cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.IdPersistentMedia);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public bool DeleteMedia(IArchiveMedia archiveMedia)
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand("DELETE FROM archivemedia WHERE idArchiveMedia=@idArchiveMedia;"))
                {
                    cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.IdPersistentMedia);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        public void UpdateMedia(IAnimatedMedia animatedMedia, ulong serverId)
        {
            lock (Connection)
            {
                using (var transaction = BeginTransaction())
                {
                    try
                    {
                        DbUpdateMedia(animatedMedia, serverId);
                        Update_media_templated(animatedMedia);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
        }


        public void UpdateMedia(IServerMedia serverMedia, ulong serverId)
        {
            lock (Connection)
                DbUpdateMedia(serverMedia, serverId);
        }

        private void DbUpdateMedia(IPersistentMedia serverMedia, ulong serverId)
        {
            using (var cmd = CreateCommand(
@"UPDATE servermedia SET 
idServer=@idServer,
MediaName=@MediaName,
Folder=@Folder,
FileName=@FileName,
FileSize=@FileSize,
LastUpdated=@LastUpdated,
LastPlayed=@LastPlayed,
Duration=@Duration,
DurationPlay=@DurationPlay,
idProgramme=@idProgramme,
statusMedia=@statusMedia,
typMedia=@typMedia,
typAudio=@typAudio,
typVideo=@typVideo,
TCStart=@TCStart,
TCPlay=@TCPlay,
AudioVolume=@AudioVolume,
AudioLevelIntegrated=@AudioLevelIntegrated,
AudioLevelPeak=@AudioLevelPeak,
idAux=@idAux,
KillDate=@KillDate,
MediaGuid=@MediaGuid,
flags=@flags 
WHERE idServerMedia=@idServerMedia;"))
            {
                cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.IdPersistentMedia);
                MediaFillParamsAndExecute(cmd, "servermedia", serverMedia, serverId);
            }
        }

        public void UpdateMedia(IArchiveMedia archiveMedia, ulong serverId)
        {
            lock (Connection)
            {
                using (var cmd = CreateCommand(@"UPDATE archivemedia SET 
idArchive=@idArchive, 
MediaName=@MediaName, 
Folder=@Folder, 
FileName=@FileName, 
FileSize=@FileSize, 
LastUpdated=@LastUpdated, 
Duration=@Duration, 
DurationPlay=@DurationPlay, 
idProgramme=@idProgramme, 
statusMedia=@statusMedia, 
typMedia=@typMedia, 
typAudio=@typAudio, 
typVideo=@typVideo, 
TCStart=@TCStart, 
TCPlay=@TCPlay, 
AudioVolume=@AudioVolume, 
AudioLevelIntegrated=@AudioLevelIntegrated,
AudioLevelPeak=@AudioLevelPeak,
idAux=@idAux, 
KillDate=@KillDate, 
MediaGuid=@MediaGuid, 
flags=@flags 
WHERE idArchiveMedia=@idArchiveMedia;"))
                {
                    cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.IdPersistentMedia);
                    MediaFillParamsAndExecute(cmd, "archivemedia", archiveMedia, serverId);
                    Debug.WriteLine(archiveMedia, "ArchiveMediaUpdate-d");
                }
            }
        }


#endregion // Media

#region MediaSegment
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, WeakReference> _mediaSegments = new System.Collections.Concurrent.ConcurrentDictionary<Guid, WeakReference>();

        private ConstructorInfo _mediaSegmentsConstructorInfo;
        private IMediaSegments FindInDictionary(Guid mediaGuid)
        {
            if (!_mediaSegments.TryGetValue(mediaGuid, out var existingRef))
                return null;
            if (existingRef.IsAlive)
                return (IMediaSegments)existingRef.Target;
            _mediaSegments.TryRemove(mediaGuid, out _);
            return null;
        }

        public T MediaSegmentsRead<T>(IPersistentMedia media) where T : IMediaSegments 
        {
            lock (Connection)
            {
                if (_mediaSegmentsConstructorInfo == null)
                    _mediaSegmentsConstructorInfo = typeof(T).GetConstructor(new[] { typeof(Guid) });

                if (_mediaSegmentsConstructorInfo == null)
                    throw new ApplicationException("No constructor found for IMediaSegments");

                var mediaGuid = media.MediaGuid;
                using (var cmd = CreateCommand("SELECT * FROM mediasegments WHERE MediaGuid = @MediaGuid;"))
                {
                    cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                    var segments = FindInDictionary(mediaGuid);
                    if (segments == null)
                    {
                        segments = (IMediaSegments)_mediaSegmentsConstructorInfo.Invoke(new object[] { mediaGuid });
                        _mediaSegments.TryAdd(mediaGuid, new WeakReference(segments));
                    }
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var newSegment = segments.Add(
                                dataReader.IsDBNull("TCIn") ? default : dataReader.GetTimeSpan("TCIn"),
                                dataReader.IsDBNull("TCOut") ? default : dataReader.GetTimeSpan("TCOut"),
                                dataReader.IsDBNull("SegmentName") ? string.Empty : dataReader.GetString("SegmentName")
                                );
                            newSegment.Id = dataReader.GetUInt64("idMediaSegment");
                        }
                        dataReader.Close();
                    }
                    return (T)segments;
                }
            }
        }

        public void DeleteMediaSegment(IMediaSegment mediaSegment)
        {
            if (!(mediaSegment is IPersistent ps) || ps.Id == 0)
                return;
            lock (Connection)
                using (var cmd = CreateCommand("DELETE FROM mediasegments WHERE idMediaSegment=@idMediaSegment;"))
                {
                    cmd.Parameters.AddWithValue("@idMediaSegment", ps.Id);
                    cmd.ExecuteNonQuery();
                }
        }
        public ulong SaveMediaSegment(IMediaSegment mediaSegment)
        {
            if (!(mediaSegment is IPersistent ps))
                return 0;
            lock (Connection)
                using (var command = ps.Id == 0
                    ? CreateCommand("INSERT INTO mediasegments (MediaGuid, TCIn, TCOut, SegmentName) VALUES (@MediaGuid, @TCIn, @TCOut, @SegmentName);")
                    : CreateCommand("INSERT INTO mediasegments (MediaGuid, TCIn, TCOut, SegmentName) VALUES (@MediaGuid, @TCIn, @TCOut, @SegmentName);"))
                {
                    if (ps.Id != 0)
                        command.Parameters.AddWithValue("@idMediaSegment", ps.Id);
                    command.Parameters.AddWithValue("@MediaGuid", mediaSegment.Owner.MediaGuid);
#if MYSQL
                    command.Parameters.AddWithValue("@TCIn", mediaSegment.TcIn);
                    command.Parameters.AddWithValue("@TCOut", mediaSegment.TcOut);
#elif SQLITE
                    command.Parameters.AddWithValue("@TCIn", mediaSegment.TcIn.Ticks);
                    command.Parameters.AddWithValue("@TCOut", mediaSegment.TcOut.Ticks);
#endif
                    command.Parameters.AddWithValue("@SegmentName", mediaSegment.SegmentName);
                    command.ExecuteNonQuery();
                    return (ulong)command.LastInsertedId();
                }
        }


        #endregion // MediaSegment

        #region Security

        public void InsertSecurityObject(ISecurityObject aco)
        {
            if (!(aco is IPersistent pAco))
                throw new ArgumentNullException(nameof(aco));
            lock (Connection)
                using (var cmd = CreateCommand(@"INSERT INTO aco (typAco, Config) VALUES (@typAco, @Config);"))
                {
                    cmd.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(aco, HibernationSerializerSettings));
                    cmd.Parameters.AddWithValue("@typAco", (int)aco.SecurityObjectTypeType);
                    cmd.ExecuteNonQuery();
                    pAco.Id = (ulong)cmd.LastInsertedId();
                }
        }

        public void DeleteSecurityObject(ISecurityObject aco)
        {
            if (!(aco is IPersistent pAco) || pAco.Id == 0)
                throw new ArgumentNullException(nameof(aco));
            lock (Connection)
                using (var cmd = CreateCommand(@"DELETE FROM aco WHERE idACO=@idACO;"))
                {
                    cmd.Parameters.AddWithValue("@idACO", pAco.Id);
                    cmd.ExecuteNonQuery();
                }
        }

        public void UpdateSecurityObject(ISecurityObject aco)
        {
            if (!(aco is IPersistent pAco) || pAco.Id == 0)
                throw new ArgumentNullException(nameof(aco));
            lock (Connection)
                using (var cmd = CreateCommand(@"UPDATE aco SET Config=@Config WHERE idACO=@idACO;"))
                {
                    cmd.Parameters.AddWithValue("@Config", Newtonsoft.Json.JsonConvert.SerializeObject(aco, HibernationSerializerSettings));
                    cmd.Parameters.AddWithValue("@idACO", pAco.Id);
                    cmd.ExecuteNonQuery();
                }
        }

        public List<T> LoadSecurityObject<T>() where T : ISecurityObject
        {
            var acos = new List<T>();
            lock (Connection)
                using (var cmd = CreateCommand("SELECT * FROM aco WHERE typACO=@typACO;"))
                {
                    if (typeof(IUser).IsAssignableFrom(typeof(T)))
                        cmd.Parameters.AddWithValue("@typACO", (int)SecurityObjectType.User);
                    if (typeof(IGroup).IsAssignableFrom(typeof(T)))
                        cmd.Parameters.AddWithValue("@typACO", (int)SecurityObjectType.Group);
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var aco = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(dataReader.GetString("Config"), HibernationSerializerSettings);
                            if (aco is IPersistent pUser)
                                pUser.Id = dataReader.GetUInt64("idACO");
                            acos.Add(aco);
                        }
                        dataReader.Close();
                        return acos;
                    }
                }
        }

#endregion

        protected DbTransaction BeginTransaction() => Connection.BeginTransaction();



    }
}
