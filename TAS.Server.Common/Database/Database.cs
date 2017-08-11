//#undef DEBUG
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Common.Database
{
    public static class Database
    {
        static DbConnectionRedundant _connection;
        private static string _connectionStringSecondary;
        private static string _connectionStringPrimary;

        public static void Open(string connectionStringPrimary = null, string connectionStringSecondary = null)
        {
            if (connectionStringPrimary != null)
            {
                _connectionStringPrimary = connectionStringPrimary;
                _connectionStringSecondary = connectionStringSecondary;
            }
            _connection = new DbConnectionRedundant(_connectionStringPrimary, _connectionStringSecondary);
            _connection.StateRedundantChange += _connection_StateRedundantChange;
            _connection.Open();
            //_connection.Update();
        }

        private static void _connection_StateRedundantChange(object sender, RedundantConnectionStateEventArgs e)
        {
            ConnectionStateChanged?.Invoke(sender, e);
        }

        public static event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;

        public static void Close()
        {
            _connection.Close();
        }

        public static string ConnectionStringPrimary => _connectionStringPrimary;
        public static string ConnectionStringSecondary => _connectionStringSecondary;

        public static ConnectionStateRedundant ConnectionState => _connection.StateRedundant;

        #region Configuration Functions
        public static bool TestConnect(string connectionString)
        {
            return DbConnectionRedundant.TestConnect(connectionString);
        }
        
        public static bool CreateEmptyDatabase(string connectionString, string collate)
        {
            return DbConnectionRedundant.CreateEmptyDatabase(connectionString, collate);
        }

        public static bool DropDatabase(string connectionString)
        {
            return DbConnectionRedundant.DropDatabase(connectionString);
        }

        public static bool CloneDatabase(string connectionStringSource, string connectionStringDestination)
        {
            DbConnectionRedundant.CloneDatabase(connectionStringSource, connectionStringDestination);
            return DbConnectionRedundant.TestConnect(connectionStringDestination);
        }
                
        public static bool UpdateRequired()
        {
            var command = new DbCommandRedundant("select `value` from `params` where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", _connection);
            string dbVersionStr;
            int dbVersionNr = 0; int resVersionNr;
            try
            {
                lock (_connection)
                    dbVersionStr = (string)command.ExecuteScalar();
                var regexMatchDb = System.Text.RegularExpressions.Regex.Match(dbVersionStr, @"\d+");
                if (regexMatchDb.Success)
                    int.TryParse(regexMatchDb.Value, out dbVersionNr);
            }
            catch
            {
                // ignored
            }
            var schemaUpdates = new System.Resources.ResourceManager("TAS.Server.Common.Database.SchemaUpdates", Assembly.GetExecutingAssembly());
            var resourceEnumerator = schemaUpdates.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true).GetEnumerator();
            while (resourceEnumerator.MoveNext())
            {
                if (resourceEnumerator.Key is string && resourceEnumerator.Value is string)
                {
                    var regexMatchRes = System.Text.RegularExpressions.Regex.Match((string)resourceEnumerator.Key, @"\d+");
                    if (regexMatchRes.Success
                        && int.TryParse(regexMatchRes.Value, out resVersionNr)
                        && resVersionNr > dbVersionNr)
                        return true;
                }
            }
            return false;
        }

        public static bool UpdateDb()
        {
            var command = new DbCommandRedundant("select `value` from `params` where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", _connection);
            int dbVersionNr = 0;
            try
            {
                string dbVersionStr;
                lock (_connection)
                    dbVersionStr = (string)command.ExecuteScalar();
                var regexMatchDb = System.Text.RegularExpressions.Regex.Match(dbVersionStr, @"\d+");
                if (regexMatchDb.Success)
                    int.TryParse(regexMatchDb.Value, out dbVersionNr);
            }
            catch
            {
                // ignored
            }
            var schemaUpdates = new System.Resources.ResourceManager("TAS.Server.Common.Database.SchemaUpdates", Assembly.GetExecutingAssembly());
            var resourceEnumerator = schemaUpdates.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true).GetEnumerator();
            var updatesPending = new SortedList<int, string>();
            while (resourceEnumerator.MoveNext())
            {
                if (resourceEnumerator.Key is string && resourceEnumerator.Value is string)
                {
                    var regexMatchRes = System.Text.RegularExpressions.Regex.Match((string)resourceEnumerator.Key, @"\d+");
                    int resVersionNr;
                    if (regexMatchRes.Success
                        && int.TryParse(regexMatchRes.Value, out resVersionNr)
                        && resVersionNr > dbVersionNr)
                        updatesPending.Add(resVersionNr, (string)resourceEnumerator.Value);
                }
            }
            if (updatesPending.Count > 0)
            {
                foreach (var kvp in updatesPending)
                {
                    using (var tran = _connection.BeginTransaction())
                    {
                        if (_connection.ExecuteScript(kvp.Value))
                        {
                            var cmdUpdateVersion = new DbCommandRedundant(string.Format("update `params` set `value` = \"{0}\" where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", kvp.Key), _connection);
                            cmdUpdateVersion.ExecuteNonQuery();
                            tran.Commit();
                        }
                        else
                        {
                            tran.Rollback();
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        #endregion //Configuration functions

        #region IPlayoutServer

        public static List<T> DbLoadServers<T>() where T : IPlayoutServerProperties
        {
            List<T> servers = new List<T>();
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM server;", _connection);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        StringReader reader = new StringReader(dataReader.GetString("Config"));
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        T server = (T)serializer.Deserialize(reader);
                        server.Id = dataReader.GetUInt64("idServer");
                        servers.Add(server);
                    }
                    dataReader.Close();
                }
            }
            return servers;
        }

        public static void DbInsertServer(this IPlayoutServerProperties server) 
        {
            lock (_connection)
            {
                {
                    DbCommandRedundant cmd = new DbCommandRedundant(@"INSERT INTO server set typServer=0, Config=@Config", _connection);
                    XmlSerializer serializer = new XmlSerializer(server.GetType());
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, server);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.ExecuteNonQuery();
                    server.Id = (ulong)cmd.LastInsertedId;
                }
            }
        }

        public static void DbUpdateServer(this IPlayoutServerProperties server) 
        {
            lock (_connection)
            {

                DbCommandRedundant cmd = new DbCommandRedundant("UPDATE server SET Config=@Config WHERE idServer=@idServer;", _connection);
                cmd.Parameters.AddWithValue("@idServer", server.Id);
                XmlSerializer serializer = new XmlSerializer(server.GetType());
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, server);
                    cmd.Parameters.AddWithValue("@Config", writer.ToString());
                }
                cmd.ExecuteNonQuery();
            }
        }

        public static void DbDeleteServer(this IPlayoutServerProperties server) 
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("DELETE FROM server WHERE idServer=@idServer;", _connection);
                cmd.Parameters.AddWithValue("@idServer", server.Id);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion //IPlayoutServer

        #region IEngine

        public static List<T> DbLoadEngines<T>(ulong? instance = null) where T : IEnginePersistent
        {
            List<T> engines = new List<T>();
            lock (_connection)
            {
                DbCommandRedundant cmd;
                if (instance == null)
                    cmd = new DbCommandRedundant("SELECT * FROM engine;", _connection);
                else
                {
                    cmd = new DbCommandRedundant("SELECT * FROM engine where Instance=@Instance;", _connection);
                    cmd.Parameters.AddWithValue("@Instance", instance);
                }
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        StringReader reader = new StringReader(dataReader.GetString("Config"));
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        T engine = (T)serializer.Deserialize(reader);
                        engine.Id = dataReader.GetUInt64("idEngine");
                        engine.IdServerPRI = dataReader.GetUInt64("idServerPRI");
                        engine.ServerChannelPRI = dataReader.GetInt32("ServerChannelPRI");
                        engine.IdServerSEC = dataReader.GetUInt64("idServerSEC");
                        engine.ServerChannelSEC = dataReader.GetInt32("ServerChannelSEC");
                        engine.IdServerPRV = dataReader.GetUInt64("idServerPRV");
                        engine.ServerChannelPRV = dataReader.GetInt32("ServerChannelPRV");
                        engine.IdArchive = dataReader.GetUInt64("IdArchive");
                        engine.Instance = dataReader.GetUInt64("Instance");
                        engines.Add(engine);
                    }
                    dataReader.Close();
                    return engines;
                }
            }
        }

        public static void DbInsertEngine(this IEnginePersistent engine) 
        {
            lock (_connection)
            {
                {
                    DbCommandRedundant cmd = new DbCommandRedundant(@"INSERT INTO engine set Instance=@Instance, idServerPRI=@idServerPRI, ServerChannelPRI=@ServerChannelPRI, idServerSEC=@idServerSEC, ServerChannelSEC=@ServerChannelSEC,  idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config;", _connection);
                    cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                    cmd.Parameters.AddWithValue("@idServerPRI", engine.IdServerPRI);
                    cmd.Parameters.AddWithValue("@ServerChannelPRI", engine.ServerChannelPRI);
                    cmd.Parameters.AddWithValue("@idServerSEC", engine.IdServerSEC);
                    cmd.Parameters.AddWithValue("@ServerChannelSEC", engine.ServerChannelSEC);
                    cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                    cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                    cmd.Parameters.AddWithValue("@IdArchive", engine.IdArchive);
                    XmlSerializer serializer = new XmlSerializer(engine.GetType());
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, engine);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.ExecuteNonQuery();
                    engine.Id = (ulong)cmd.LastInsertedId;
                }
            }
        }

        public static void DbUpdateEngine(this IEnginePersistent engine)
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant(@"UPDATE engine set Instance=@Instance, idServerPRI=@idServerPRI, ServerChannelPRI=@ServerChannelPRI, idServerSEC=@idServerSEC, ServerChannelSEC=@ServerChannelSEC, idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config where idEngine=@idEngine", _connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                cmd.Parameters.AddWithValue("@idServerPRI", engine.IdServerPRI);
                cmd.Parameters.AddWithValue("@ServerChannelPRI", engine.ServerChannelPRI);
                cmd.Parameters.AddWithValue("@idServerSEC", engine.IdServerSEC);
                cmd.Parameters.AddWithValue("@ServerChannelSEC", engine.ServerChannelSEC);
                cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                cmd.Parameters.AddWithValue("@IdArchive", engine.IdArchive);
                XmlSerializer serializer = new XmlSerializer(engine.GetType());
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, engine);
                    cmd.Parameters.AddWithValue("@Config", writer.ToString());
                }
                cmd.ExecuteNonQuery();
            }
        }

        public static void DbDeleteEngine(this IEnginePersistent engine) 
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("DELETE FROM engine WHERE idEngine=@idEngine;", _connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DbReadRootEvents(this IEngine engine)
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where typStart in (@StartTypeManual, @StartTypeOnFixedTime, @StartTypeNone) and idEventBinding=0 and idEngine=@idEngine order by ScheduledTime, EventName", _connection);
                cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                cmd.Parameters.AddWithValue("@StartTypeManual", (byte)TStartType.Manual);
                cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", (byte)TStartType.OnFixedTime);
                cmd.Parameters.AddWithValue("@StartTypeNone", (byte)TStartType.None);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var newEvent = _eventRead(engine, dataReader);
                        engine.AddRootEvent(newEvent);
                    }
                    dataReader.Close();
                }
                Debug.WriteLine(engine, "EventReadRootEvents read");
            }
        }

        public static void DbSearchMissing(this IEngine engine) 
        {
            {
                lock (_connection)
                {
                    DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM rundownevent m WHERE m.idEngine=@idEngine and (SELECT s.idRundownEvent FROM rundownevent s WHERE m.idEventBinding = s.idRundownEvent) IS NULL", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                    IEvent newEvent;
                    List<IEvent> foundEvents = new List<IEvent>();
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            if (!engine.GetRootEvents().Any(e => (e as IEventPesistent)?.Id == dataReader.GetUInt64("idRundownEvent")))
                            {
                                newEvent = _eventRead(engine, dataReader);
                                foundEvents.Add(newEvent);
                            }
                        }
                        dataReader.Close();
                    }
                    foreach (IEvent e in foundEvents)
                    {
                        if (e is ITemplated && e is IEventPesistent)
                            _readAnimatedEvent(((IEventPesistent)e).Id, e as ITemplated);
                        e.StartType = TStartType.Manual;
                        e.IsModified = false;
                        engine.AddRootEvent(e);
                        e.Save();
                    }
                }
            }
        }

        public static List<IEvent> DbSearchPlaying(this IEngine engine)
        {
            {
                lock (_connection)
                {
                    DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM rundownevent WHERE idEngine=@idEngine and PlayState=@PlayState", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                    cmd.Parameters.AddWithValue("@PlayState", TPlayState.Playing);
                    List<IEvent> foundEvents = new List<IEvent>();
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var newEvent = _eventRead(engine, dataReader);
                            foundEvents.Add(newEvent);
                        }
                        dataReader.Close();
                    }
                    foreach (var ev in foundEvents)
                        if (ev is ITemplated && ev is IEventPesistent)
                        {
                            _readAnimatedEvent(((IEventPesistent)ev).Id, ev as ITemplated);
                            ev.IsModified = false;
                        }
                    return foundEvents;
                }
            }
        }

        public static MediaDeleteDenyReason DbMediaInUse(this IEngine engine, IServerMedia serverMedia)
        {
            MediaDeleteDenyReason reason = MediaDeleteDenyReason.NoDeny;
            lock (_connection)
            {
                string query = "select * from rundownevent where MediaGuid=@MediaGuid and ADDTIME(ScheduledTime, Duration) > UTC_TIMESTAMP();";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", serverMedia.MediaGuid);
                IEvent futureScheduled = null;
                using (DbDataReaderRedundant reader = cmd.ExecuteReader())
                    if (reader.Read())
                        futureScheduled = _eventRead(engine, reader);
                if (futureScheduled is ITemplated && futureScheduled is IEventPesistent)
                {
                    _readAnimatedEvent(((IEventPesistent)futureScheduled).Id, futureScheduled as ITemplated);
                    futureScheduled.IsModified = false;
                }
                if (futureScheduled != null)
                    return new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.MediaInFutureSchedule, Media = serverMedia, Event = futureScheduled };
            }
            return reason;
        }

        #endregion //IEngine

        #region ArchiveDirectory
        public static List<T> DbLoadArchiveDirectories<T>() where T : IArchiveDirectoryProperties, new()
        {
            List<T> directories = new List<T>();
            lock (_connection)
            {
                DbCommandRedundant cmd;
                cmd = new DbCommandRedundant("SELECT * FROM archive;", _connection);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dir = new T();
                        dir.idArchive = dataReader.GetUInt64("idArchive");
                        dir.Folder = dataReader.GetString("Folder");
                        directories.Add(dir);
                    }
                    dataReader.Close();
                }
            }
            return directories;
        }

        public static void DbInsertArchiveDirectory(this IArchiveDirectoryProperties dir) 
        {
            lock (_connection)
            {
                {
                    DbCommandRedundant cmd = new DbCommandRedundant(@"INSERT INTO archive set Folder=@Folder", _connection);
                    cmd.Parameters.AddWithValue("@Folder", dir.Folder);
                    cmd.ExecuteNonQuery();
                    dir.idArchive = (ulong)cmd.LastInsertedId;
                }
            }
        }

        public static void DbUpdateArchiveDirectory(this IArchiveDirectoryProperties dir)
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant(@"UPDATE archive set Folder=@Folder where idArchive=@idArchive", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                cmd.Parameters.AddWithValue("@Folder", dir.Folder);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DbDeleteArchiveDirectory(this IArchiveDirectoryProperties dir) 
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("DELETE FROM archive WHERE idArchive=@idArchive;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                cmd.ExecuteNonQuery();
            }
        }

        private static ConstructorInfo _archiveMediaConstructorInfo;

        private static T _readArchiveMedia<T>(DbDataReaderRedundant dataReader, IArchiveDirectory dir) where T: IArchiveMedia
        {
            if (_archiveMediaConstructorInfo == null)
                _archiveMediaConstructorInfo = typeof(T).GetConstructor(new[] { typeof(IArchiveDirectory), typeof(Guid), typeof(UInt64) });
            if (_archiveMediaConstructorInfo != null)
            {
                T media = (T)_archiveMediaConstructorInfo.Invoke(new object[] { dir, dataReader.GetGuid("MediaGuid"), dataReader.GetUInt64("idArchiveMedia") });
                media._mediaReadFields(dataReader);
                media.IsModified = false;
                return media;
            }
            throw new ApplicationException("No IArchiveMedia constructor found");
        }

        public static void DbSearch<T>(this IArchiveDirectory dir) where T: IArchiveMedia
        {
            string search = dir.SearchString;
            lock (_connection)
            {
                var textSearches = (from text in search.ToLower().Split(' ').Where(s => !string.IsNullOrEmpty(s)) select "(LOWER(MediaName) LIKE \"%" + text + "%\" or LOWER(FileName) LIKE \"%" + text + "%\")").ToArray();
                DbCommandRedundant cmd;
                if (dir.SearchMediaCategory == null)
                    cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive" 
                                                + (textSearches.Length > 0 ? " and" + string.Join(" and", textSearches) : string.Empty)
                                                + " order by idArchiveMedia DESC LIMIT 0, 1000;", _connection);
                else
                {
                    cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and ((flags >> 4) & 3)=@Category"
                                                + (textSearches.Length > 0 ? " and" + string.Join(" and", textSearches) : string.Empty)
                                                + " order by idArchiveMedia DESC LIMIT 0, 1000;", _connection);
                    cmd.Parameters.AddWithValue("@Category", (uint)dir.SearchMediaCategory);
                }
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                        _readArchiveMedia<T>(dataReader, dir);
                    dataReader.Close();
                }
            }
        }

        private static ConstructorInfo _archiveDirectoryConstructorInfo;
        public static IArchiveDirectory LoadArchiveDirectory<T>(this IMediaManager manager, UInt64 idArchive) where T: IArchiveDirectory
        {
            lock (_connection)
            {
                if (_archiveDirectoryConstructorInfo == null)
                    _archiveDirectoryConstructorInfo = typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any,  new[] { typeof(IMediaManager), typeof(ulong), typeof(string) }, null);
                string query = "SELECT Folder FROM archive WHERE idArchive=@idArchive;";
                string folder;
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idArchive", idArchive);
                folder = (string)cmd.ExecuteScalar();
                if (!string.IsNullOrEmpty(folder))
                {
                    T directory = (T)_archiveDirectoryConstructorInfo.Invoke(new object[] { manager, idArchive, folder });
                    return directory;
                }
                return null;
            }
        }

        public static IEnumerable<IArchiveMedia> DbFindStaleMedia<T>(this IArchiveDirectory dir) where T: IArchiveMedia
        {
            List<IArchiveMedia> returnList = new List<IArchiveMedia>();
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and KillDate<CURRENT_DATE and KillDate>'2000-01-01' LIMIT 0, 1000;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                        returnList.Add(_readArchiveMedia<T>(dataReader, dir));
                    dataReader.Close();
                }
            }
            return returnList;
        }

        public static T DbMediaFind<T>(this IArchiveDirectory dir, IMediaProperties media) where T: IArchiveMedia
        {
            T result = default(T);
            lock (_connection)
            {
                DbCommandRedundant cmd;
                if (media.MediaGuid != Guid.Empty)
                {
                    cmd = new DbCommandRedundant("SELECT * FROM archivemedia WHERE idArchive=@idArchive && MediaGuid=@MediaGuid;", _connection);
                    cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                    cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (dataReader.Read())
                            result = _readArchiveMedia<T>(dataReader, dir);
                        dataReader.Close();
                    }
                }
            }
            return result;
        }

        public static bool DbArchiveContainsMedia(this IArchiveDirectory dir, IMediaProperties media)
        {
            lock (_connection)
            {
                DbCommandRedundant cmd;
                if (media.MediaGuid != Guid.Empty)
                {
                    cmd = new DbCommandRedundant("SELECT count(*) FROM archivemedia WHERE idArchive=@idArchive && MediaGuid=@MediaGuid;", _connection);
                    cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                    cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                    object result = cmd.ExecuteScalar();
                    return result != null && (long)result > 0;
                }
                return false;
            }
        }

        #endregion // ArchiveDirectory

        #region IEvent
        public static List<IEvent> DbReadSubEvents(this IEngine engine, IEventPesistent eventOwner)
        {
            lock (_connection)
            {
                DbCommandRedundant cmd;
                if (eventOwner != null)
                {
                    cmd = new DbCommandRedundant("SELECT * FROM RundownEvent WHERE idEventBinding = @idEventBinding AND (typStart=@StartTypeManual OR typStart=@StartTypeOnFixedTime);", _connection);
                    if (eventOwner.EventType == TEventType.Container)
                    {
                        cmd.Parameters.AddWithValue("@StartTypeManual", TStartType.Manual);
                        cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", TStartType.OnFixedTime);
                    }
                    else
                    {
                        cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idEventBinding = @idEventBinding and typStart in (@StartTypeWithParent, @StartTypeWithParentFromEnd);", _connection);
                        cmd.Parameters.AddWithValue("@StartTypeWithParent", TStartType.WithParent);
                        cmd.Parameters.AddWithValue("@StartTypeWithParentFromEnd", TStartType.WithParentFromEnd);
                    }
                    cmd.Parameters.AddWithValue("@idEventBinding", eventOwner.Id);
                    List<IEvent> subevents = new List<IEvent>();
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                            subevents.Add(_eventRead(engine, dataReader));
                    }
                    foreach (var e in subevents)
                        if (e is ITemplated)
                        {
                            _readAnimatedEvent(e.Id, e as ITemplated);
                            e.IsModified = false;
                        }
                    return subevents;
                }
                return null;
            }
        }

        public static IEvent DbReadNext(this IEngine engine, IEventPesistent aEvent) 
        {
            lock (_connection)
            {
                if (aEvent != null)
                {
                    IEvent next = null;
                    DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idEventBinding = @idEventBinding and typStart=@StartType;", _connection);
                    cmd.Parameters.AddWithValue("@idEventBinding", aEvent.Id);
                    cmd.Parameters.AddWithValue("@StartType", TStartType.After);
                    using (DbDataReaderRedundant reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            next = _eventRead(engine, reader);
                    }
                    if (next is ITemplated && next is IEventPesistent)
                    {
                        _readAnimatedEvent(((IEventPesistent)next).Id, next as ITemplated);
                        next.IsModified = false;
                    }
                    return next;
                }
                return null;
            }
        }

        private static void _readAnimatedEvent(ulong id, ITemplated animatedEvent)
        {
            DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM `rundownevent_templated` where `idrundownevent_templated` = @id;", _connection);
            cmd.Parameters.AddWithValue("@id", id);
            using (DbDataReaderRedundant reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    animatedEvent.Method = (TemplateMethod)reader.GetByte("Method");
                    animatedEvent.TemplateLayer = reader.GetInt16("TemplateLayer");
                    string templateFields = reader.GetString("Fields");
                    if (!string.IsNullOrWhiteSpace(templateFields))
                    {
                        var fieldsDeserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(templateFields);
                        if (fieldsDeserialized != null)
                            animatedEvent.Fields = fieldsDeserialized;
                    }
                }
            }
        }

        public static IEvent DbReadEvent(this IEngine engine, UInt64 idRundownEvent)
        {
            lock (_connection)
            {
                if (idRundownEvent > 0)
                {
                    IEvent result = null;
                    DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idRundownEvent = @idRundownEvent", _connection);
                    cmd.Parameters.AddWithValue("@idRundownEvent", idRundownEvent);
                    using (DbDataReaderRedundant reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            result = _eventRead(engine, reader);
                    }
                    if (result is ITemplated && result is IEventPesistent)
                    {
                        _readAnimatedEvent(((IEventPesistent)result).Id, result as ITemplated);
                        result.IsModified = false;
                    }
                }
                return null;
            }
        }

        private static IEvent _eventRead(IEngine engine, DbDataReaderRedundant dataReader)
        {
            uint flags = dataReader.IsDBNull(dataReader.GetOrdinal("flagsEvent")) ? 0 : dataReader.GetUInt32("flagsEvent");
            ushort transitionType = dataReader.GetUInt16("typTransition");
            TEventType eventType = (TEventType)dataReader.GetByte("typEvent");
            IEvent newEvent = engine.CreateNewEvent(
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
                dataReader.IsDBNull(dataReader.GetOrdinal("RequestedStartTime")) ? null : (TimeSpan?)dataReader.GetTimeSpan("RequestedStartTime"),
                dataReader.GetTimeSpan("TransitionTime"),
                dataReader.GetTimeSpan("TransitionPauseTime"),
                (TTransitionType)(transitionType & 0xFF),
                (TEasing)(transitionType >> 8),
                dataReader.IsDBNull(dataReader.GetOrdinal("AudioVolume")) ? null : (decimal?)dataReader.GetDecimal("AudioVolume"),
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
                dataReader.GetString("Commands")
                );
            return newEvent;
        }

        private static DateTime _minMySqlDate = new DateTime(1000, 01, 01);
        private static DateTime _maxMySQLDate = new DateTime(9999, 12, 31, 23, 59, 59);

        private static bool _eventFillParamsAndExecute(DbCommandRedundant cmd, IEventPesistent aEvent)
        {

            Debug.WriteLineIf(aEvent.Duration.Days > 1, aEvent, "Duration extremely long");
            cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)aEvent.Engine).Id);
            cmd.Parameters.AddWithValue("@idEventBinding", aEvent.IdEventBinding);
            cmd.Parameters.AddWithValue("@Layer", (sbyte)aEvent.Layer);
            cmd.Parameters.AddWithValue("@typEvent", aEvent.EventType);
            cmd.Parameters.AddWithValue("@typStart", aEvent.StartType);
            if (aEvent.ScheduledTime < _minMySqlDate || aEvent.ScheduledTime > _maxMySQLDate)
            {
                cmd.Parameters.AddWithValue("@ScheduledTime", DBNull.Value);
            }
            else
                cmd.Parameters.AddWithValue("@ScheduledTime", aEvent.ScheduledTime);
            cmd.Parameters.AddWithValue("@Duration", aEvent.Duration);
            if (aEvent.ScheduledTc.Equals(TimeSpan.Zero))
                cmd.Parameters.AddWithValue("@ScheduledTC", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@ScheduledTC", aEvent.ScheduledTc);
            cmd.Parameters.AddWithValue("@ScheduledDelay", aEvent.ScheduledDelay);
            if (aEvent.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", aEvent.MediaGuid);
            cmd.Parameters.AddWithValue("@EventName", aEvent.EventName);
            cmd.Parameters.AddWithValue("@PlayState", aEvent.PlayState);
            if (aEvent.StartTime < _minMySqlDate || aEvent.StartTime > _maxMySQLDate)
                cmd.Parameters.AddWithValue("@StartTime", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@StartTime", aEvent.StartTime);
            if (aEvent.StartTc.Equals(TimeSpan.Zero))
                cmd.Parameters.AddWithValue("@StartTC", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@StartTC", aEvent.StartTc);
            if (aEvent.RequestedStartTime == null)
                cmd.Parameters.AddWithValue("@RequestedStartTime", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@RequestedStartTime", aEvent.RequestedStartTime);
            cmd.Parameters.AddWithValue("@TransitionTime", aEvent.TransitionTime);
            cmd.Parameters.AddWithValue("@TransitionPauseTime", aEvent.TransitionPauseTime);
            cmd.Parameters.AddWithValue("@typTransition", (ushort)aEvent.TransitionType | ((ushort)aEvent.TransitionEasing)<<8);
            cmd.Parameters.AddWithValue("@idProgramme", aEvent.IdProgramme);
            if (aEvent.AudioVolume == null)
                cmd.Parameters.AddWithValue("@AudioVolume", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@AudioVolume", aEvent.AudioVolume);
            cmd.Parameters.AddWithValue("@flagsEvent", aEvent.ToFlags());
            object command = aEvent.EventType == TEventType.CommandScript && aEvent is ICommandScript
                ? (object)(aEvent as ICommandScript).Command
                : DBNull.Value;
            cmd.Parameters.AddWithValue("@Commands", command);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static void _eventAnimatedSave(ulong id,  ITemplated e, bool inserting)
        {
            string query = inserting ?
                @"INSERT INTO `rundownevent_templated` (`idrundownevent_templated`, `Method`, `TemplateLayer`, `Fields`) VALUES (@idrundownevent_templated, @Method, @TemplateLayer, @Fields);" :
                @"UPDATE `rundownevent_templated` SET  `Method`=@Method, `TemplateLayer`=@TemplateLayer, `Fields`=@Fields WHERE `idrundownevent_templated`=@idrundownevent_templated;";
            using (DbCommandRedundant cmd = new DbCommandRedundant(query, _connection))
            {
                cmd.Parameters.AddWithValue("@idrundownevent_templated", id);
                cmd.Parameters.AddWithValue("@Method", (byte)e.Method);
                cmd.Parameters.AddWithValue("@TemplateLayer", e.TemplateLayer);
                cmd.Parameters.AddWithValue("@Fields", Newtonsoft.Json.JsonConvert.SerializeObject(e.Fields));
                cmd.ExecuteNonQuery();
            }
        }


        public static bool DbInsertEvent(this IEventPesistent aEvent)
        {
            lock (_connection)
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    string query =
@"INSERT INTO RundownEvent 
(idEngine, idEventBinding, Layer, typEvent, typStart, ScheduledTime, ScheduledDelay, Duration, ScheduledTC, MediaGuid, EventName, PlayState, StartTime, StartTC, RequestedStartTime, TransitionTime, TransitionPauseTime, typTransition, AudioVolume, idProgramme, flagsEvent, Commands) 
VALUES 
(@idEngine, @idEventBinding, @Layer, @typEvent, @typStart, @ScheduledTime, @ScheduledDelay, @Duration, @ScheduledTC, @MediaGuid, @EventName, @PlayState, @StartTime, @StartTC, @RequestedStartTime, @TransitionTime, @TransitionPauseTime, @typTransition, @AudioVolume, @idProgramme, @flagsEvent, @Commands);";
                    using (DbCommandRedundant cmd = new DbCommandRedundant(query, _connection))
                        if (_eventFillParamsAndExecute(cmd, aEvent))
                        {
                            aEvent.Id = (ulong)cmd.LastInsertedId;
                            Debug.WriteLine("Event DbInsertSecurityObject Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                            if (aEvent is ITemplated)
                                _eventAnimatedSave(aEvent.Id, aEvent as ITemplated, true);
                            transaction.Commit();
                            return true;
                        }
                }
            }
            return false;
        }

        public static bool DbUpdateEvent<TEvent>(this TEvent aEvent) where  TEvent: IEventPesistent
        {
            lock (_connection)
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    string query =
@"UPDATE RundownEvent 
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
Commands=@Commands
WHERE idRundownEvent=@idRundownEvent;";
                    using (DbCommandRedundant cmd = new DbCommandRedundant(query, _connection))
                    {
                        cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                        if (_eventFillParamsAndExecute(cmd, aEvent))
                        {
                            Debug.WriteLine("Event DbUpdateSecurityObject Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                            if (aEvent is ITemplated)
                                _eventAnimatedSave(aEvent.Id, aEvent as ITemplated, false);
                            transaction.Commit();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool DbDeleteEvent(this IEventPesistent aEvent)
        {
            lock (_connection)
            {
                string query = "DELETE FROM RundownEvent WHERE idRundownEvent=@idRundownEvent;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                cmd.ExecuteNonQuery();
                Debug.WriteLine("Event DbDeleteMediaSegment Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                return true;
            }
        }

        public static void AsRunLogWrite(this IEventPesistent e)
        {
            try
            {
                lock (_connection)
                {
                    DbCommandRedundant cmd = new DbCommandRedundant(
@"INSERT INTO asrunlog (
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
);", _connection);

                    cmd.Parameters.AddWithValue("@ExecuteTime", e.StartTime);
                    IMedia media = e.Media;
                    if (media != null)
                    {
                        cmd.Parameters.AddWithValue("@MediaName", media.MediaName);
                        if (media is IPersistentMedia)
                            cmd.Parameters.AddWithValue("@idAuxMedia", (media as IPersistentMedia).IdAux);
                        else
                            cmd.Parameters.AddWithValue("@idAuxMedia", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
                        cmd.Parameters.AddWithValue("@typAudio", (byte)media.AudioChannelMapping);
                    }
                    else
                    {
                        if (e.EventType == TEventType.Live)
                            cmd.Parameters.AddWithValue("@MediaName", "LIVE");
                        else
                            cmd.Parameters.AddWithValue("@MediaName", DBNull.Value);
                        cmd.Parameters.AddWithValue("@idAuxMedia", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typAudio", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@StartTC", e.StartTc);
                    cmd.Parameters.AddWithValue("@Duration", e.Duration);
                    cmd.Parameters.AddWithValue("@idProgramme", e.IdProgramme);
                    cmd.Parameters.AddWithValue("@idAuxRundown", e.IdAux);
                    cmd.Parameters.AddWithValue("@SecEvents", string.Join(";", e.SubEvents.Select(se => se.EventName)));
                    cmd.Parameters.AddWithValue("@Flags", e.ToFlags());
                    cmd.ExecuteNonQuery();
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

        public static List<IAclRight> DbReadEventAclList<TEventAcl>(IEventPesistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl: IAclRight, IPersistent, new()
        {
            if (aEvent == null)
                return null;
            lock (_connection)
            {
                DbCommandRedundant cmd =
                    new DbCommandRedundant("SELECT * FROM rundownevent_acl WHERE idRundownEvent = @idRundownEvent;",
                        _connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                List<IAclRight> acl = new List<IAclRight>();
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
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

        public static bool DbInsertEventAcl<TEventAcl>(this TEventAcl acl) where TEventAcl: IAclRight, IPersistent
        {
            if (acl?.Owner == null)
                return false;
            lock (_connection)
            {
                using (DbCommandRedundant cmd =
                    new DbCommandRedundant(
                        "INSERT INTO rundownevent_acl (idRundownEvent, idACO, ACL) VALUES (@idRundownEvent, @idACO, @ACL);",
                        _connection))
                {
                    cmd.Parameters.AddWithValue("@idRundownEvent", acl.Owner.Id);
                    cmd.Parameters.AddWithValue("@idACO", ((IPersistent)acl.SecurityObject).Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    if (cmd.ExecuteNonQuery() == 1)
                        acl.Id = (ulong) cmd.LastInsertedId;
                    return true;
                }
            }
        }

        public static bool DbUpdateEventAcl<TEventAcl>(this TEventAcl acl) where TEventAcl: IAclRight, IPersistent
        {
            lock (_connection)
            {
                using (DbCommandRedundant cmd =
                    new DbCommandRedundant(
                        "UPDATE rundownevent_acl SET ACL=@ACL WHERE idRundownevent_ACL=@idRundownevent_ACL;",
                        _connection))
                {
                    cmd.Parameters.AddWithValue("@idRundownevent_ACL", acl.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        public static bool DbDeleteEventAcl<TEventAcl>(this TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (_connection)
            {
                string query = "DELETE FROM rundownevent_acl WHERE idRundownevent_ACL=@idRundownevent_ACL;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idRundownevent_ACL", acl.Id);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        #endregion //ACL

        #region Media
        private static void _mediaFillParamsAndExecute(DbCommandRedundant cmd, IPersistentMedia media, ulong serverId)
        {
            cmd.Parameters.AddWithValue("@idProgramme", media.IdProgramme);
            cmd.Parameters.AddWithValue("@idAux", media.IdAux);
            if (media.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
            if (media.KillDate == default(DateTime))
                cmd.Parameters.AddWithValue("@KillDate", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@KillDate", media.KillDate);
            uint flags = ((media is IServerMedia && (media as IServerMedia).DoNotArchive) ? 0x1 : (uint)0x0)
                        | (media.Protected ? 0x2 : (uint)0x0)
                        | (media.FieldOrderInverted ? 0x4 : (uint)0x0)
                        | ((uint)media.MediaCategory << 4) // bits 4-7 of 1st byte
                        | ((uint)media.MediaEmphasis << 8) // bits 1-3 of second byte
                        | ((uint)media.Parental << 12) // bits 4-7 of second byte
                        ;
            cmd.Parameters.AddWithValue("@flags", flags);
            if (media is IServerMedia && media.Directory is IServerDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
            }
            if (media is IAnimatedMedia && media.Directory is IAnimationDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
            }
            if (media is IArchiveMedia && media.Directory is IArchiveDirectory)
            {
                cmd.Parameters.AddWithValue("@idArchive", ((IArchiveDirectory)((IArchiveMedia)media).Directory).idArchive);
                cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
            }
            cmd.Parameters.AddWithValue("@MediaName", media.MediaName);
            cmd.Parameters.AddWithValue("@Duration", media.Duration);
            cmd.Parameters.AddWithValue("@DurationPlay", media.DurationPlay);
            cmd.Parameters.AddWithValue("@Folder", media.Folder);
            cmd.Parameters.AddWithValue("@FileSize", media.FileSize);
            cmd.Parameters.AddWithValue("@FileName", media.FileName);
            if (media.LastUpdated == default(DateTime))
                cmd.Parameters.AddWithValue("@LastUpdated", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@LastUpdated", media.LastUpdated);
            cmd.Parameters.AddWithValue("@statusMedia", (int)media.MediaStatus);
            cmd.Parameters.AddWithValue("@typMedia", (int)media.MediaType);
            cmd.Parameters.AddWithValue("@typAudio", (byte)media.AudioChannelMapping);
            cmd.Parameters.AddWithValue("@AudioVolume", media.AudioVolume);
            cmd.Parameters.AddWithValue("@AudioLevelIntegrated", media.AudioLevelIntegrated);
            cmd.Parameters.AddWithValue("@AudioLevelPeak", media.AudioLevelPeak);
            cmd.Parameters.AddWithValue("@TCStart", media.TcStart);
            cmd.Parameters.AddWithValue("@TCPlay", media.TcPlay);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            { Debug.WriteLine(media, e.Message); }
        }

        private static void _mediaReadFields(this IPersistentMedia media, DbDataReaderRedundant dataReader)
        {
            uint flags = dataReader.IsDBNull(dataReader.GetOrdinal("flags")) ? 0 : dataReader.GetUInt32("flags");
            media.MediaName = dataReader.IsDBNull(dataReader.GetOrdinal("MediaName")) ? string.Empty : dataReader.GetString("MediaName");
            media.Duration = dataReader.IsDBNull(dataReader.GetOrdinal("Duration")) ? default(TimeSpan) : dataReader.GetTimeSpan("Duration");
            media.DurationPlay = dataReader.IsDBNull(dataReader.GetOrdinal("DurationPlay")) ? default(TimeSpan) : dataReader.GetTimeSpan("DurationPlay");
            media.Folder = dataReader.IsDBNull(dataReader.GetOrdinal("Folder")) ? string.Empty : dataReader.GetString("Folder");
            media.FileName = dataReader.IsDBNull(dataReader.GetOrdinal("FileName")) ? string.Empty : dataReader.GetString("FileName");
            media.FileSize = dataReader.IsDBNull(dataReader.GetOrdinal("FileSize")) ? 0 : dataReader.GetUInt64("FileSize");
            media.LastUpdated = dataReader.GetDateTime("LastUpdated");
            media.MediaStatus = (TMediaStatus)(dataReader.IsDBNull(dataReader.GetOrdinal("statusMedia")) ? 0 : dataReader.GetInt32("statusMedia"));
            media.MediaType = (TMediaType)(dataReader.IsDBNull(dataReader.GetOrdinal("typMedia")) ? 0 : dataReader.GetInt32("typMedia"));
            media.TcStart = dataReader.IsDBNull(dataReader.GetOrdinal("TCStart")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCStart");
            media.TcPlay = dataReader.IsDBNull(dataReader.GetOrdinal("TCPlay")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCPlay");
            media.IdProgramme = dataReader.IsDBNull(dataReader.GetOrdinal("idProgramme")) ? 0 : dataReader.GetUInt64("idProgramme");
            media.AudioVolume = dataReader.IsDBNull(dataReader.GetOrdinal("AudioVolume")) ? 0 : dataReader.GetDecimal("AudioVolume");
            media.AudioLevelIntegrated = dataReader.IsDBNull(dataReader.GetOrdinal("AudioLevelIntegrated")) ? 0 : dataReader.GetDecimal("AudioLevelIntegrated");
            media.AudioLevelPeak = dataReader.IsDBNull(dataReader.GetOrdinal("AudioLevelPeak")) ? 0 : dataReader.GetDecimal("AudioLevelPeak");
            media.AudioChannelMapping = dataReader.IsDBNull(dataReader.GetOrdinal("typAudio")) ? TAudioChannelMapping.Stereo : (TAudioChannelMapping)dataReader.GetByte("typAudio");
            media.VideoFormat = (TVideoFormat)(dataReader.IsDBNull(dataReader.GetOrdinal("typVideo")) ? (byte)0 : (byte)(dataReader.GetByte("typVideo") & 0x7F));
            media.IdAux = dataReader.IsDBNull(dataReader.GetOrdinal("idAux")) ? string.Empty : dataReader.GetString("idAux");
            media.KillDate = dataReader.GetDateTime("KillDate");
            media.MediaEmphasis = (TMediaEmphasis)((flags >> 8) & 0xF);
            media.Parental = (byte)((flags >> 12) & 0xF);
            if (media is IServerMedia)
                ((IServerMedia)media).DoNotArchive = (flags & 0x1) != 0;
            media.Protected = (flags & 0x2) != 0;
            media.FieldOrderInverted = (flags & 0x4) != 0;
            media.MediaCategory = (TMediaCategory)((flags >> 4) & 0xF); // bits 4-7 of 1st byte
        }

        static ConstructorInfo _serverMediaConstructorInfo;
        static ConstructorInfo _animatedMediaConstructorInfo;
        public static void Load<T>(this IAnimationDirectory directory, ulong serverId) where T: IAnimatedMedia
        {
            Debug.WriteLine(directory, "AnimationDirectory load started");
            lock (_connection)
            {
                if (_animatedMediaConstructorInfo == null)
                    _animatedMediaConstructorInfo = typeof(T).GetConstructor(new[] { typeof(IMediaDirectory), typeof(Guid), typeof(UInt64) });
                if (_animatedMediaConstructorInfo == null)
                    throw new ApplicationException("No constructor found for IAnimatedMedia");

                DbCommandRedundant cmd = new DbCommandRedundant("SELECT servermedia.*, media_templated.`Fields`, media_templated.`Method`, media_templated.`TemplateLayer` FROM serverMedia LEFT JOIN media_templated ON servermedia.MediaGuid = media_templated.MediaGuid WHERE idServer=@idServer and typMedia = @typMedia", _connection);
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typMedia", TMediaType.Animation);
                try
                {
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {

                            T nm = (T)_animatedMediaConstructorInfo.Invoke(new object[] { directory, dataReader.GetGuid("MediaGuid"), dataReader.GetUInt64("idServerMedia")});
                            nm._mediaReadFields(dataReader);
                            string templateFields = dataReader.GetString("Fields");
                            if (!string.IsNullOrWhiteSpace(templateFields))
                            {
                                var fieldsDeserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(templateFields);
                                if (fieldsDeserialized != null)
                                    nm.Fields = fieldsDeserialized;
                            }
                            nm.Method = (TemplateMethod)dataReader.GetByte("Method");
                            nm.TemplateLayer = dataReader.GetInt32("TemplateLayer");
                            nm.IsModified = false;
                            if (nm.MediaStatus != TMediaStatus.Available)
                            {
                                nm.MediaStatus = TMediaStatus.Unknown;
                                nm.ReVerify();
                            }
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

        public static void Load<T>(this IServerDirectory directory, IArchiveDirectory archiveDirectory, ulong serverId) where T : IServerMedia
        {
            Debug.WriteLine(directory, "ServerLoadMediaDirectory started");
            lock (_connection)
            {
                if (_serverMediaConstructorInfo == null)
                    _serverMediaConstructorInfo = typeof(T).GetConstructor(new[] { typeof(IMediaDirectory), typeof(Guid), typeof(UInt64), typeof(IArchiveDirectory) });
                if (_serverMediaConstructorInfo == null)
                    throw new ApplicationException("No constructor found for IServerMedia");

                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM serverMedia WHERE idServer=@idServer and typMedia in (@typMediaMovie, @typMediaStill)", _connection);
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typMediaMovie", TMediaType.Movie);
                cmd.Parameters.AddWithValue("@typMediaStill", TMediaType.Still);
                try
                {
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            T nm = (T)_serverMediaConstructorInfo.Invoke(new object[] { directory, dataReader.GetGuid("MediaGuid"), dataReader.GetUInt64("idServerMedia"), archiveDirectory});
                            nm._mediaReadFields(dataReader);
                            nm.IsModified = false;
                            if (nm.MediaStatus != TMediaStatus.Available)
                            {
                                nm.MediaStatus = TMediaStatus.Unknown;
                                nm.ReVerify();
                            }
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

        private static bool _insert_media_templated(IAnimatedMedia media)
        {
            try
            {
                string query = @"INSERT IGNORE INTO media_templated (MediaGuid, Fields, TemplateLayer, Method) VALUES (@MediaGuid, @Fields, @TemplateLayer, @Method);";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                cmd.Parameters.AddWithValue("@TemplateLayer", media.TemplateLayer);
                cmd.Parameters.AddWithValue("@Method", (byte)media.Method);
                cmd.Parameters.AddWithValue("@Fields", Newtonsoft.Json.JsonConvert.SerializeObject(media.Fields));

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("_insert_media_templated failed with {0}", e.Message, null);
                return false;
            }
        }

        private static void _update_media_templated(IAnimatedMedia media)
        {
            try
            {
                string query = @"UPDATE media_templated SET Fields = @Fields, TemplateLayer=@TemplateLayer, Method=@Method WHERE MediaGuid = @MediaGuid;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                cmd.Parameters.AddWithValue("@TemplateLayer", media.TemplateLayer);
                cmd.Parameters.AddWithValue("@Method", (byte)media.Method);
                cmd.Parameters.AddWithValue("@Fields", Newtonsoft.Json.JsonConvert.SerializeObject(media.Fields));
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Debug.WriteLine("_update_media_templated failed with {0}", e.Message, null);
            }
        }

        private static bool _delete_media_templated(IAnimatedMedia media)
        {
            try
            {
                string query = @"DELETE FROM media_templated WHERE MediaGuid = @MediaGuid;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("_delete_media_templated failed with {0}", e.Message, null);
                return false;
            }
        }

        public static bool DbInsertMedia(this IAnimatedMedia animatedMedia, ulong serverId )
        {
            bool result = false;
            lock (_connection)
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        result = _dbInsertMedia(animatedMedia, serverId);
                        if (result)
                            result = _insert_media_templated(animatedMedia);
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
            return result;
        }

        public static bool DbInsertMedia(this IServerMedia serverMedia, ulong serverId)
        {
            lock (_connection)
                return _dbInsertMedia(serverMedia, serverId);
        }

        private static bool _dbInsertMedia(IPersistentMedia media, ulong serverId)
        {
            string query =
@"INSERT INTO servermedia 
(idServer, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idServer, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);";
            DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
            _mediaFillParamsAndExecute(cmd, media, serverId);
            media.IdPersistentMedia = (UInt64)cmd.LastInsertedId;
            Debug.WriteLine(media, "ServerMediaInserte-d");
            return true;
        }

        public static bool DbInsertMedia(this IArchiveMedia archiveMedia, ulong serverid)
        {
            lock (_connection)
            {
                string query =
@"INSERT INTO archivemedia 
(idArchive, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idArchive, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                _mediaFillParamsAndExecute(cmd, archiveMedia, serverid);
                archiveMedia.IdPersistentMedia = (UInt64)cmd.LastInsertedId;
            }
            return true;
        }

        public static bool DbDeleteMedia(this IServerMedia serverMedia)
        {
            lock (_connection)
            {
                return _dbDeleteMedia(serverMedia);
            }
        }

        public static bool DbDeleteMedia(this IAnimatedMedia animatedMedia)
        {
            lock (_connection)
            {
                bool result = false;
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        result = _dbDeleteMedia(animatedMedia);
                        if (result)
                            result = _delete_media_templated(animatedMedia);
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


        private static bool _dbDeleteMedia(IPersistentMedia serverMedia)
        {
            string query = "DELETE FROM ServerMedia WHERE idServerMedia=@idServerMedia;";
            DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
            cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.IdPersistentMedia);
            return cmd.ExecuteNonQuery() == 1;
        }

        public static bool DbDeleteMedia(this IArchiveMedia archiveMedia)
        {
            lock (_connection)
            {
                string query = "DELETE FROM archivemedia WHERE idArchiveMedia=@idArchiveMedia;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.IdPersistentMedia);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public static void DbUpdateMedia(this IAnimatedMedia animatedMedia, ulong serverId)
        {
            lock (_connection)
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        _dbUpdateMedia(animatedMedia, serverId);
                        _update_media_templated(animatedMedia);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
        }


        public static void DbUpdateMedia(this IServerMedia serverMedia, ulong serverId)
        {
            lock (_connection)
                _dbUpdateMedia(serverMedia, serverId);
        }

        private static void _dbUpdateMedia(IPersistentMedia serverMedia, ulong serverId)
        {
            string query =
                @"UPDATE ServerMedia SET 
idServer=@idServer, 
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
WHERE idServerMedia=@idServerMedia;";
            DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
            cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.IdPersistentMedia);
            _mediaFillParamsAndExecute(cmd, serverMedia, serverId);
            Debug.WriteLine(serverMedia, "ServerMediaUpdate-d");
        }

        public static void DbUpdateMedia(this IArchiveMedia archiveMedia, ulong serverId)
        {
            lock (_connection)
            {
                string query =
@"UPDATE archivemedia SET 
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
WHERE idArchiveMedia=@idArchiveMedia;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.IdPersistentMedia);
                _mediaFillParamsAndExecute(cmd, archiveMedia, serverId);
                Debug.WriteLine(archiveMedia, "ArchiveMediaUpdate-d");
            }
        }


        #endregion // Media

        #region MediaSegment
        private static System.Collections.Concurrent.ConcurrentDictionary<Guid, WeakReference> _mediaSegments = new System.Collections.Concurrent.ConcurrentDictionary<Guid, WeakReference>();
        private static ConstructorInfo _mediaSegmentsConstructorInfo;
        private static IMediaSegments _findInDictionary(Guid mediaGuid)
        {
            WeakReference existingRef;
            if (_mediaSegments.TryGetValue(mediaGuid, out existingRef))
            {
                if (existingRef.IsAlive)
                    return (IMediaSegments)existingRef.Target;
                else
                    _mediaSegments.TryRemove(mediaGuid, out existingRef);
            }
            return null;
        }

        public static T DbMediaSegmentsRead<T>(this IPersistentMedia media) where T : IMediaSegments 
        {
            lock (_connection)
            {
                if (_mediaSegmentsConstructorInfo == null)
                    _mediaSegmentsConstructorInfo = typeof(T).GetConstructor(new[] { typeof(Guid) });

                if (_mediaSegmentsConstructorInfo == null)
                    throw new ApplicationException("No constructor found for IMediaSegments");

                Guid mediaGuid = media.MediaGuid;
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM MediaSegments where MediaGuid = @MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                IMediaSegments segments = _findInDictionary(mediaGuid);
                if (segments == null)
                {
                    segments = (IMediaSegments)_mediaSegmentsConstructorInfo.Invoke(new object[] { mediaGuid });
                    _mediaSegments.TryAdd(mediaGuid, new WeakReference(segments));
                }
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var newSegment = segments.Add(
                            dataReader.IsDBNull(dataReader.GetOrdinal("TCIn")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCIn"),
                            dataReader.IsDBNull(dataReader.GetOrdinal("TCOut")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCOut"),
                            dataReader.IsDBNull(dataReader.GetOrdinal("SegmentName")) ? string.Empty : dataReader.GetString("SegmentName")
                            );
                        ((IPersistent)newSegment).Id = dataReader.GetUInt64("idMediaSegment");
                    }
                    dataReader.Close();
                }
                return (T)segments;
            }
        }

        public static void DbDeleteMediaSegment(this IMediaSegment mediaSegment)
        {
            var ps = mediaSegment as IPersistent;
            if (ps != null && ps.Id!= 0)
            {
                lock (_connection)
                {
                    string query = "DELETE FROM mediasegments WHERE idMediaSegment=@idMediaSegment;";
                    DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                    cmd.Parameters.AddWithValue("@idMediaSegment", ps.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static ulong DbSaveMediaSegment(this IMediaSegment mediaSegment)
        {
            var ps = mediaSegment as IPersistent;
            if (ps == null)
                return 0;
            lock (_connection)
            {
                DbCommandRedundant command;
                if (ps.Id == 0)
                    command = new DbCommandRedundant("INSERT INTO mediasegments (MediaGuid, TCIn, TCOut, SegmentName) VALUES (@MediaGuid, @TCIn, @TCOut, @SegmentName);", _connection);
                else
                {
                    command = new DbCommandRedundant("UPDATE mediasegments SET TCIn = @TCIn, TCOut = @TCOut, SegmentName = @SegmentName WHERE idMediaSegment=@idMediaSegment AND MediaGuid = @MediaGuid;", _connection);
                    command.Parameters.AddWithValue("@idMediaSegment", ps.Id);
                }
                command.Parameters.AddWithValue("@MediaGuid", mediaSegment.Owner.MediaGuid);
                command.Parameters.AddWithValue("@TCIn", mediaSegment.TcIn);
                command.Parameters.AddWithValue("@TCOut", mediaSegment.TcOut);
                command.Parameters.AddWithValue("@SegmentName", mediaSegment.SegmentName);
                command.ExecuteNonQuery();
                return (ulong)command.LastInsertedId;
            }
        }


        #endregion // MediaSegment

        #region Security

        public static void DbInsertSecurityObject(this ISecurityObject aco)
        {
            var pAco = aco as IPersistent;
            if (pAco == null)
            {
#if  DEBUG
                throw new NoNullAllowedException("DbInsertSecurityObject: operation on null");
#endif
                return;
            }
            lock (_connection)
            {
                {
                    var cmd = new DbCommandRedundant(@"insert into aco set typAco=@typAco, Config=@Config;", _connection);
                    var serializer = new XmlSerializer(pAco.GetType());
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, pAco);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.Parameters.AddWithValue("@typAco", (int) aco.SceurityObjectTypeType);
                    cmd.ExecuteNonQuery();
                    pAco.Id = (ulong)cmd.LastInsertedId;
                }
            }
        }

        public static void DbDeleteSecurityObject(this ISecurityObject aco)
        {
            var pAco = aco as IPersistent;
            if (pAco == null || pAco.Id == 0)
            {
#if  DEBUG
                throw new ApplicationException("DbDeleteMediaSegment: operation on null or not saved object");
#endif
                return;
            }
            lock (_connection)
            {
                {
                    var cmd = new DbCommandRedundant(@"delete from aco where idACO=@idACO;", _connection);
                    cmd.Parameters.AddWithValue("@idACO", pAco.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DbUpdateSecurityObject(this ISecurityObject aco)
        {
            var pAco = aco as IPersistent;
            if (pAco == null || pAco.Id == 0)
            {
#if  DEBUG
                throw new ApplicationException("DbUpdateSecurityObject: operation on null or not saved object");
#endif
                return;
            }
            lock (_connection)
            {
                {
                    var cmd = new DbCommandRedundant(@"update aco set Config=@Config where idACO=@idACO;", _connection);
                    var serializer = new XmlSerializer(pAco.GetType());
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, pAco);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.Parameters.AddWithValue("@idACO", pAco.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<T> DbLoad<T>() where T : ISecurityObject
        {
            var users = new List<T>();
            lock (_connection)
            {
                var cmd = new DbCommandRedundant("select * from aco where typACO=@typACO;", _connection);
                if (typeof(IUser).IsAssignableFrom(typeof(T)))
                    cmd.Parameters.AddWithValue("@typACO", (int) SceurityObjectType.User);
                if (typeof(IGroup).IsAssignableFrom(typeof(T)))
                    cmd.Parameters.AddWithValue("@typACO", (int)SceurityObjectType.Group);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var reader = new StringReader(dataReader.GetString("Config"));
                        var serializer = new XmlSerializer(typeof(T));
                        var user = (T)serializer.Deserialize(reader);
                        var pUser = user as IPersistent;
                        if (pUser != null)
                            pUser.Id = dataReader.GetUInt64("idACO");
                        users.Add(user);
                    }
                    dataReader.Close();
                    return users;
                }
            }
        }

        #endregion

    }
}
