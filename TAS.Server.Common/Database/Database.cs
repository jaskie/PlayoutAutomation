using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Data;
using System.Diagnostics;
using TAS.Server.Interfaces;
using TAS.Common;
using System.Xml;
using System.Xml.Serialization;
using TAS.Server.Common;

namespace TAS.Server.Database
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
            var h = ConnectionStateChanged;
            if (h != null)
                h(sender, e);
        }

        public static event StateRedundantChangeEventHandler ConnectionStateChanged;

        public static void Close()
        {
            _connection.Close();
        }

        public static string ConnectionStringPrimary { get { return _connectionStringPrimary; } }
        public static string ConnectionStringSecondary { get { return _connectionStringSecondary; } }

        public static ConnectionStateRedundant ConnectionState { get { return _connection.StateRedundant; } }

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
                var regexMatchDB = System.Text.RegularExpressions.Regex.Match(dbVersionStr, @"\d+");
                if (regexMatchDB.Success)
                    int.TryParse(regexMatchDB.Value, out dbVersionNr);
            }
            catch { }
            var schemaUpdates = new System.Resources.ResourceManager("TAS.Server.Common.Database.SchemaUpdates", System.Reflection.Assembly.GetExecutingAssembly());
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

        public static bool UpdateDB()
        {
            var command = new DbCommandRedundant("select `value` from `params` where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", _connection);
            string dbVersionStr;
            int dbVersionNr = 0; int resVersionNr;
            try
            {
                lock (_connection)
                    dbVersionStr = (string)command.ExecuteScalar();
                var regexMatchDB = System.Text.RegularExpressions.Regex.Match(dbVersionStr, @"\d+");
                if (regexMatchDB.Success)
                    int.TryParse(regexMatchDB.Value, out dbVersionNr);
            }
            catch { }
            var schemaUpdates = new System.Resources.ResourceManager("TAS.Server.Common.Database.SchemaUpdates", System.Reflection.Assembly.GetExecutingAssembly());
            var resourceEnumerator = schemaUpdates.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true).GetEnumerator();
            var updatesPending = new SortedList<int, string>();
            while (resourceEnumerator.MoveNext())
            {
                if (resourceEnumerator.Key is string && resourceEnumerator.Value is string)
                {
                    var regexMatchRes = System.Text.RegularExpressions.Regex.Match((string)resourceEnumerator.Key, @"\d+");
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
                    var tran = _connection.BeginTransaction();
                    if (_connection.ExecuteScript(kvp.Value))
                    {
                        var cmdUpdateVersion = new DbCommandRedundant(string.Format("update `params` set `value` = \"{0}\" where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", kvp.Key), _connection);
                        if (cmdUpdateVersion.ExecuteNonQuery() > 0)
                        {
                            tran.Commit();
                            continue;
                        }
                    }
                    tran.Rollback();
                    return false;
                }
            }
            return true;
        }

        #endregion //Configuration functions

        #region IPlayoutServer

        public static List<T> DbLoadServers<T>() where T : IPlayoutServerConfig
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

        public static void DbInsertServer(this IPlayoutServerConfig server) 
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

        public static void DbUpdateServer(this IPlayoutServerConfig server) 
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

        public static void DbDeleteServer(this IPlayoutServerConfig server) 
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

        public static List<T> DbLoadEngines<T>(ulong? instance = null) where T : IEngineConfig
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

        public static void DbInsertEngine(this IEngineConfig engine) 
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

        public static void DbUpdateEngine(this IEngineConfig engine)
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

        public static void DbDeleteEngine(this IEngineConfig engine) 
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
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.Parameters.AddWithValue("@StartTypeManual", (byte)TStartType.Manual);
                cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", (byte)TStartType.OnFixedTime);
                cmd.Parameters.AddWithValue("@StartTypeNone", (byte)TStartType.None);
                IEvent NewEvent;
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        NewEvent = _eventRead(engine, dataReader);
                        engine.RootEvents.Add(NewEvent);
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
                    cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                    IEvent newEvent;
                    List<IEvent> foundEvents = new List<IEvent>();
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            if (!engine.RootEvents.Any(e => e.IdRundownEvent == dataReader.GetUInt64("idRundownEvent")))
                            {
                                newEvent = _eventRead(engine, dataReader);
                                foundEvents.Add(newEvent);
                            }
                        }
                        dataReader.Close();
                    }
                    foreach (IEvent e in foundEvents)
                    {
                        e.StartType = TStartType.Manual;
                        e.Save();
                        engine.RootEvents.Add(e);
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
                    cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                    cmd.Parameters.AddWithValue("@PlayState", TPlayState.Playing);
                    IEvent newEvent;
                    List<IEvent> foundEvents = new List<IEvent>();
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            newEvent = _eventRead(engine, dataReader);
                            foundEvents.Add(newEvent);
                        }
                        dataReader.Close();
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
                using (DbDataReaderRedundant reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.MediaInFutureSchedule, Media = serverMedia, Event = _eventRead(engine, reader) };
                }
            }
            return reason;
        }


        #endregion //IEngine

        #region ArchiveDirectory
        public static List<T> DbLoadArchiveDirectories<T>() where T : IArchiveDirectoryConfig, new()
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

        public static void DbInsertArchiveDirectory(this IArchiveDirectoryConfig dir) 
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

        public static void DbUpdateArchiveDirectory(this IArchiveDirectoryConfig dir)
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant(@"UPDATE archive set Folder=@Folder where idArchive=@idArchive", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                cmd.Parameters.AddWithValue("@Folder", dir.Folder);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DbDeleteArchiveDirectory(this IArchiveDirectoryConfig dir) 
        {
            lock (_connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("DELETE FROM archive WHERE idArchive=@idArchive;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                cmd.ExecuteNonQuery();
            }
        }

        private static System.Reflection.ConstructorInfo _archiveMediaConstructorInfo;

        private static T _readArchiveMedia<T>(DbDataReaderRedundant dataReader, IArchiveDirectory dir) where T: IArchiveMedia
        {
            if (_archiveMediaConstructorInfo == null)
                _archiveMediaConstructorInfo = typeof(T).GetConstructor(new[] { typeof(IArchiveDirectory), typeof(Guid), typeof(UInt64) });
            byte typVideo = dataReader.IsDBNull(dataReader.GetOrdinal("typVideo")) ? (byte)0 : dataReader.GetByte("typVideo");
            T media = (T)_archiveMediaConstructorInfo.Invoke(new object[] { dir, dataReader.GetGuid("MediaGuid"), dataReader.GetUInt64("idArchiveMedia") });
            media._mediaReadFields(dataReader);
            return media;
        }

        public static void DbSearch<T>(this IArchiveDirectory dir) where T: IArchiveMedia
        {
            string search = dir.SearchString;
            if (string.IsNullOrWhiteSpace(search))
                return;
            lock (_connection)
            {
                var textSearches = from text in search.ToLower().Split(' ').Where(s => !string.IsNullOrEmpty(s)) select "(LOWER(MediaName) LIKE \"%" + text + "%\" or LOWER(FileName) LIKE \"%" + text + "%\")";
                DbCommandRedundant cmd;
                if (dir.SearchMediaCategory == null)
                    cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and " + string.Join(" and ", textSearches) + " LIMIT 0, 1000;", _connection);
                else
                {
                    cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and ((flags >> 4) & 3)=@Category and  " + string.Join(" and ", textSearches) + " LIMIT 0, 1000;", _connection);
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

        private static System.Reflection.ConstructorInfo _archiveDirectoryConstructorInfo;
        public static IArchiveDirectory LoadArchiveDirectory<T>(this IMediaManager manager, UInt64 idArchive) where T: IArchiveDirectory
        {
            lock (_connection)
            {
                if (_archiveDirectoryConstructorInfo == null)
                    _archiveDirectoryConstructorInfo = typeof(T).GetConstructor(new[] { typeof(IMediaManager), typeof(UInt64), typeof(string) });
                string query = "SELECT Folder FROM archive WHERE idArchive=@idArchive;";
                string folder = null;
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

        public static T DbMediaFind<T>(this IArchiveDirectory dir, IMedia media) where T: IArchiveMedia
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

        public static bool DbArchiveContainsMedia(this IArchiveDirectory dir, IMedia media)
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
        public static void DbReadSubEvents(this IEngine engine, IEvent eventOwner, IList<IEvent> subevents)
        {
            lock (_connection)
            {
                DbCommandRedundant cmd;
                if (eventOwner != null)
                {
                    cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idEventBinding = @idEventBinding and typStart=@StartType;", _connection);
                    cmd.Parameters.AddWithValue("@idEventBinding", eventOwner.IdRundownEvent);
                    if (eventOwner.EventType == TEventType.Container)
                        cmd.Parameters.AddWithValue("@StartType", TStartType.Manual);
                    else
                        cmd.Parameters.AddWithValue("@StartType", TStartType.With);
                    IEvent newEvent;
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        try
                        {
                            while (dataReader.Read())
                            { 
                                newEvent = _eventRead(engine, dataReader);
                                subevents.Add(newEvent);
                            }
                        }
                        finally
                        {
                            dataReader.Close();
                        }
                    }
                }
            }
        }

        public static IEvent DbReadNext(this IEngine engine, IEvent aEvent) 
        {
            lock (_connection)
            {
                if (aEvent != null)
                {
                    DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idEventBinding = @idEventBinding and typStart=@StartType;", _connection);
                    cmd.Parameters.AddWithValue("@idEventBinding", aEvent.IdRundownEvent);
                    cmd.Parameters.AddWithValue("@StartType", TStartType.After);
                    DbDataReaderRedundant reader = cmd.ExecuteReader();
                    try
                    {
                        if (reader.Read())
                            return _eventRead(engine, reader);
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                return null;
            }
        }

        public static IEvent DbReadEvent(this IEngine engine, UInt64 idRundownEvent)
        {
            lock (_connection)
            {
                if (idRundownEvent > 0)
                {
                    DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idRundownEvent = @idRundownEvent", _connection);
                    cmd.Parameters.AddWithValue("@idRundownEvent", idRundownEvent);
                    DbDataReaderRedundant reader = cmd.ExecuteReader();
                    try
                    {
                        if (reader.Read())
                            return _eventRead(engine, reader);
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                return null;
            }
        }

        private static IEvent _eventRead(IEngine engine, DbDataReaderRedundant dataReader)
        {
            uint flags = dataReader.IsDBNull(dataReader.GetOrdinal("flagsEvent")) ? 0 : dataReader.GetUInt32("flagsEvent");
            IEvent newEvent = engine.AddNewEvent(
                dataReader.GetUInt64("idRundownEvent"),
                dataReader.GetUInt64("idEventBinding"),
                (VideoLayer)dataReader.GetSByte("Layer"),
                (TEventType)dataReader.GetByte("typEvent"),
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
                (TTransitionType)dataReader.GetByte("typTransition"),
                dataReader.IsDBNull(dataReader.GetOrdinal("AudioVolume")) ? null : (decimal?)dataReader.GetDecimal("AudioVolume"),
                dataReader.GetUInt64("idProgramme"),
                dataReader.GetString("IdAux"),
                (flags & (1 << 0)) != 0, // IsEnabled
                (flags & (1 << 1)) != 0, // IsHold
                (flags & (1 << 2)) != 0, // IsLoop
                EventGPI.FromUInt64((flags >> 4) & EventGPI.Mask)
                );  
            return newEvent;
        }

        private static DateTime _minMySqlDate = new DateTime(1000, 01, 01);
        private static DateTime _maxMySQLDate = new DateTime(9999, 12, 31, 23, 59, 59);

        private static Boolean _EventFillParamsAndExecute(DbCommandRedundant cmd, IEvent aEvent)
        {

            Debug.WriteLineIf(aEvent.Duration.Days > 1, aEvent, "Duration extremely long");
            cmd.Parameters.AddWithValue("@idEngine", aEvent.Engine.Id);
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
            cmd.Parameters.AddWithValue("@typTransition", aEvent.TransitionType);
            cmd.Parameters.AddWithValue("@idProgramme", aEvent.IdProgramme);
            if (aEvent.AudioVolume == null)
                cmd.Parameters.AddWithValue("@AudioVolume", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@AudioVolume", aEvent.AudioVolume);
            UInt64 flags = Convert.ToUInt64(aEvent.IsEnabled) << 0
                         | Convert.ToUInt64(aEvent.IsHold) << 1
                         | Convert.ToUInt64(aEvent.IsLoop) << 2
                         | aEvent.GPI.ToUInt64() << 4 // of size EventGPI.Size
                         ;
            cmd.Parameters.AddWithValue("@flagsEvent", flags);
            return cmd.ExecuteNonQuery() == 1;
        }

        public static bool DbInsert(this IEvent aEvent)
        {
            lock (_connection)
            {
                string query =
@"INSERT INTO RundownEvent 
(idEngine, idEventBinding, Layer, typEvent, typStart, ScheduledTime, ScheduledDelay, Duration, ScheduledTC, MediaGuid, EventName, PlayState, StartTime, StartTC, RequestedStartTime, TransitionTime, typTransition, AudioVolume, idProgramme, flagsEvent) 
VALUES 
(@idEngine, @idEventBinding, @Layer, @typEvent, @typStart, @ScheduledTime, @ScheduledDelay, @Duration, @ScheduledTC, @MediaGuid, @EventName, @PlayState, @StartTime, @StartTC, @RequestedStartTime, @TransitionTime, @typTransition, @AudioVolume, @idProgramme, @flagsEvent);";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                if (_EventFillParamsAndExecute(cmd, aEvent))
                {
                    aEvent.IdRundownEvent = (ulong)cmd.LastInsertedId;
                    Debug.WriteLine("Event DbInsert Id={0}, EventName={1}", aEvent.IdRundownEvent, aEvent.EventName);
                    return true;
                }
            }
            return false;
        }

        public static Boolean DbUpdate(this IEvent aEvent)
        {
            lock (_connection)
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
typTransition=@typTransition, 
AudioVolume=@AudioVolume, 
idProgramme=@idProgramme, 
flagsEvent=@flagsEvent 
WHERE idRundownEvent=@idRundownEvent;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.IdRundownEvent);
                if (_EventFillParamsAndExecute(cmd, aEvent))
                {
                    Debug.WriteLine("Event DbUpdate Id={0}, EventName={1}", aEvent.IdRundownEvent, aEvent.EventName);
                    return true;
                }
            }
            return false;
        }

        public static Boolean DbDelete(this IEvent aEvent)
        {
            Boolean success = false;
            lock (_connection)
            {
                string query = "DELETE FROM RundownEvent WHERE idRundownEvent=@idRundownEvent;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.IdRundownEvent);
                cmd.ExecuteNonQuery();
                success = true;
                Debug.WriteLine("Event DbDelete Id={0}, EventName={1}", aEvent.IdRundownEvent, aEvent.EventName);
            }
            return success;
        }

        public static void AsRunLogWrite(this IEvent e)
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
typAudio
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
@typAudio
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
                    cmd.Parameters.AddWithValue("@SecEvents", string.Join(";", e.SubEvents.ToList().Select(se => se.EventName)));
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

        #region Media
        private static Boolean _mediaFillParamsAndExecute(DbCommandRedundant cmd, IPersistentMedia media, ulong serverId)
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
            uint flags = ((media is IServerMedia && (media as IServerMedia).DoNotArchive) ? (uint)0x1 : (uint)0x0)
                        | (media.Protected ? (uint)0x2 : (uint)0x0)
                        | ((uint)(media.MediaCategory) << 4) // bits 4-7 of 1st byte
                        | ((uint)media.MediaEmphasis << 8) // bits 1-3 of second byte
                        | ((uint)media.Parental << 12) // bits 4-7 of second byte
                        ;
            cmd.Parameters.AddWithValue("@flags", flags);
            if (media is IServerMedia && media.Directory is IServerDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
            }
            if (media is IServerMedia && media.Directory is IAnimationDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
            }
            if (media is IArchiveMedia && media.Directory is IArchiveDirectory)
            {
                cmd.Parameters.AddWithValue("@idArchive", (((media as IArchiveMedia).Directory) as IArchiveDirectory).idArchive);
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
            return true;
        }

        private static void _mediaReadFields(this IPersistentMedia media, DbDataReaderRedundant dataReader)
        {
            uint flags = dataReader.IsDBNull(dataReader.GetOrdinal("flags")) ? (uint)0 : dataReader.GetUInt32("flags");
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
            media.Parental = (TParental)((flags >> 12) & 0xF);
            if (media is IServerMedia)
                ((IServerMedia)media).DoNotArchive = (flags & 0x1) != 0;
            media.Protected = (flags & 0x2) != 0;
            media.MediaCategory = (TMediaCategory)((flags >> 4) & 0xF); // bits 4-7 of 1st byte
            media.Modified = false;
        }

        static System.Reflection.ConstructorInfo _serverMediaConstructorInfo;
        public static void Load<T>(this IAnimationDirectory directory, ulong serverId) where T: IServerMedia, ITemplated
        {
            Debug.WriteLine(directory, "ServerLoadMediaDirectory animation started");
            lock (_connection)
            {
                if (_serverMediaConstructorInfo == null)
                    _serverMediaConstructorInfo = typeof(T).GetConstructor(new[] { typeof(IMediaDirectory), typeof(Guid), typeof(UInt64) });

                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM serverMedia WHERE idServer=@idServer and typMedia = @typMedia", _connection);
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typMedia", TMediaType.Animation);
                try
                {
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {

                            T nm = (T)_serverMediaConstructorInfo.Invoke(new object[] { directory, dataReader.GetGuid("MediaGuid"), dataReader.GetUInt64("idServerMedia")});
                            nm._mediaReadFields(dataReader);
                            if (nm.MediaStatus != TMediaStatus.Available)
                            {
                                nm.MediaStatus = TMediaStatus.Unknown;
                                nm.ReVerify();
                            }
                        }
                        dataReader.Close();
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
                            if (nm.MediaStatus != TMediaStatus.Available)
                            {
                                nm.MediaStatus = TMediaStatus.Unknown;
                                nm.ReVerify();
                            }
                        }
                        dataReader.Close();
                    }
                    Debug.WriteLine(directory, "Directory loaded");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(directory, e.Message);
                }
            }
        }

        public static Boolean DbInsert(this IServerMedia serverMedia, ulong serverId)
        {
            Boolean success = false;
            lock (_connection)
            {
                string query =
@"INSERT INTO servermedia 
(idServer, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idServer, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                _mediaFillParamsAndExecute(cmd, serverMedia, serverId);
                serverMedia.IdPersistentMedia = (UInt64)cmd.LastInsertedId;
                success = true;
                Debug.WriteLineIf(success, serverMedia, "ServerMediaInserte-d");
            }
            return success;
        }

        public static Boolean DbInsert(this IArchiveMedia archiveMedia, ulong serverid)
        {
            Boolean success = false;
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
                success = true;
            }
            return success;
        }

        public static Boolean DbDelete(this IServerMedia serverMedia)
        {
            lock (_connection)
            {
                string query = "DELETE FROM ServerMedia WHERE idServerMedia=@idServerMedia;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.IdPersistentMedia);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public static Boolean DbDelete(this IArchiveMedia archiveMedia)
        {
            lock (_connection)
            {
                string query = "DELETE FROM archivemedia WHERE idArchiveMedia=@idArchiveMedia;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.IdPersistentMedia);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public static Boolean DbUpdate(this IServerMedia serverMedia, ulong serverId)
        {
            Boolean success = false;
            lock (_connection)
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
                success = _mediaFillParamsAndExecute(cmd, serverMedia, serverId);
                Debug.WriteLineIf(success, serverMedia, "ServerMediaUpdate-d");
            }
            return success;
        }

        public static Boolean DbUpdate(this IArchiveMedia archiveMedia, ulong serverId)
        {
            Boolean success = false;
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
                success = _mediaFillParamsAndExecute(cmd, archiveMedia, serverId);
                Debug.WriteLineIf(success, archiveMedia, "ArchiveMediaUpdate-d");
            }
            return success;
        }


        #endregion // Media

        #region MediaSegment
        private static System.Collections.Hashtable _mediaSegments;
        private static System.Reflection.ConstructorInfo _mediaSegmentsConstructorInfo;

        public static ObservableSynchronizedCollection<IMediaSegment> DbMediaSegmentsRead<T>(this IPersistentMedia media) where T: IMediaSegment
        {
            lock (_connection)
            {
                if (_mediaSegmentsConstructorInfo == null)
                    _mediaSegmentsConstructorInfo = typeof(T).GetConstructor(new[] { typeof(Guid), typeof(UInt64)});

                Guid mediaGuid = media.MediaGuid;
                ObservableSynchronizedCollection<IMediaSegment> segments = null;
                IMediaSegment newMediaSegment;
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM MediaSegments where MediaGuid = @MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                if (_mediaSegments == null)
                    _mediaSegments = new System.Collections.Hashtable();
                segments = (ObservableSynchronizedCollection<IMediaSegment>)_mediaSegments[mediaGuid];
                if (segments == null)
                {
                    segments = new ObservableSynchronizedCollection<IMediaSegment>();
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            newMediaSegment = (T)_mediaSegmentsConstructorInfo.Invoke(new object[] { mediaGuid, dataReader.GetUInt64("idMediaSegment") });
                            newMediaSegment.SegmentName = (dataReader.IsDBNull(dataReader.GetOrdinal("SegmentName")) ? string.Empty : dataReader.GetString("SegmentName"));
                            newMediaSegment.TcIn = dataReader.IsDBNull(dataReader.GetOrdinal("TCIn")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCIn");
                            newMediaSegment.TcOut = dataReader.IsDBNull(dataReader.GetOrdinal("TCOut")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCOut");
                            segments.Add(newMediaSegment);
                        }
                        dataReader.Close();
                    }
                    _mediaSegments.Add(mediaGuid, segments);
                }
                return segments;
            }
        }

        public static void DbDelete(this IMediaSegment mediaSegment)
        {
            if (mediaSegment.IdMediaSegment != 0)
            {
                var segments = (ObservableSynchronizedCollection<IMediaSegment>)_mediaSegments[mediaSegment.MediaGuid];
                if (segments != null)
                {
                    segments.Remove(mediaSegment);
                }
                lock (_connection)
                {

                    string query = "DELETE FROM mediasegments WHERE idMediaSegment=@idMediaSegment;";
                    DbCommandRedundant cmd = new DbCommandRedundant(query, _connection);
                    cmd.Parameters.AddWithValue("@idMediaSegment", mediaSegment.IdMediaSegment);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static UInt64 DbSave(this IMediaSegment mediaSegment)
        {
            lock (_connection)
            {
                DbCommandRedundant command;
                if (mediaSegment.IdMediaSegment == 0)
                    command = new DbCommandRedundant("INSERT INTO mediasegments (MediaGuid, TCIn, TCOut, SegmentName) VALUES (@MediaGuid, @TCIn, @TCOut, @SegmentName);", _connection);
                else
                {
                    command = new DbCommandRedundant("UPDATE mediasegments SET TCIn = @TCIn, TCOut = @TCOut, SegmentName = @SegmentName WHERE idMediaSegment=@idMediaSegment AND MediaGuid = @MediaGuid;", _connection);
                    command.Parameters.AddWithValue("@idMediaSegment", mediaSegment.IdMediaSegment);
                }
                command.Parameters.AddWithValue("@MediaGuid", mediaSegment.MediaGuid);
                command.Parameters.AddWithValue("@TCIn", mediaSegment.TcIn);
                command.Parameters.AddWithValue("@TCOut", mediaSegment.TcOut);
                command.Parameters.AddWithValue("@SegmentName", mediaSegment.SegmentName);
                command.ExecuteNonQuery();
                return (UInt64)command.LastInsertedId;
            }
        }


        #endregion // MediaSegment
    }
}
