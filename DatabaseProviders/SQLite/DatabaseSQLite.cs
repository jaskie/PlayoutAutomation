using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Database.Interfaces;
using TAS.Common.Database.Interfaces.Media;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Common.Interfaces.Security;

namespace TAS.Database.SQLite
{
    [Export(typeof(IDatabase))]
    public class DatabaseSQLite : IDatabase
    {
        private SQLiteConnection _connection;
        public string ConnectionStringPrimary { get; private set; }
        public string ConnectionStringSecondary { get; private set; }

        public ConnectionStateRedundant ConnectionState => (ConnectionStateRedundant)_connection.State;

        public IDictionary<string, int> ServerMediaFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> ArchiveMediaFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> EventFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> SecurityObjectFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> MediaSegmentFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> EngineFieldLengths { get; } = new Dictionary<string, int>();

        public IDictionary<string, int> ServerFieldLengths { get; } = new Dictionary<string, int>();

        public event EventHandler<RedundantConnectionStateEventArgs> ConnectionStateChanged;

        public bool ArchiveContainsMedia(IArchiveDirectoryProperties dir, Guid mediaGuid)
        {
            if (dir == null || mediaGuid == Guid.Empty)
                return false;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT count(*) FROM archivemedia WHERE idArchive=@idArchive AND MediaGuid=@MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                var result = cmd.ExecuteScalar();
                return result != null && (long)result > 0;
            }
        }

        public T ArchiveMediaFind<T>(IArchiveDirectoryServerSide dir, Guid mediaGuid) where T : IArchiveMedia, new()
        {
            var result = default(T);
            if (mediaGuid == Guid.Empty)
                return result;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT * FROM archivemedia WHERE idArchive=@idArchive AND MediaGuid=@MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                using (var dataReader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (dataReader.Read())
                    {
                        result = _readArchiveMedia<T>(dataReader);
                    }
                    dataReader.Close();
                }
            }
            return result;
        }

        public List<T> ArchiveMediaSearch<T>(IArchiveDirectoryServerSide dir, TMediaCategory? mediaCategory, string search) where T : IArchiveMedia, new()
        {
            lock (_connection)
            {
                var textSearches = (from text in search.ToLower().Split(' ').Where(s => !string.IsNullOrEmpty(s)) select "(LOWER(MediaName) LIKE \"%" + text + "%\" or LOWER(FileName) LIKE \"%" + text + "%\")").ToArray();
                SQLiteCommand cmd;
                if (mediaCategory == null)
                    cmd = new SQLiteCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive"
                                                + (textSearches.Length > 0 ? " and" + string.Join(" and", textSearches) : string.Empty)
                                                + " order by idArchiveMedia DESC LIMIT 0, 1000;", _connection);
                else
                {
                    cmd = new SQLiteCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and ((flags >> 4) & 3)=@Category"
                                                + (textSearches.Length > 0 ? " and" + string.Join(" and", textSearches) : string.Empty)
                                                + " order by idArchiveMedia DESC LIMIT 0, 1000;", _connection);
                    cmd.Parameters.AddWithValue("@Category", (uint)mediaCategory);
                }
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                using (var dataReader = cmd.ExecuteReader())
                {
                    var result = new List<T>();
                    while (dataReader.Read())
                    {
                        var media = _readArchiveMedia<T>(dataReader);
                        dir.AddMedia(media);
                        result.Add(media);
                    }
                    dataReader.Close();
                    return result;
                }
            }
        }

        public void AsRunLogWrite(ulong idEngine, IEvent e)
        {
            try
            {
                lock (_connection)
                {
                    var cmd = new SQLiteCommand(
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
);", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", idEngine);
                    cmd.Parameters.AddWithValue("@ExecuteTime", e.StartTime.Ticks);
                    var media = e.Media;
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
                        cmd.Parameters.AddWithValue("@MediaName", e.EventName);
                        cmd.Parameters.AddWithValue("@idAuxMedia", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@typAudio", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@StartTC", e.StartTc.Ticks);
                    cmd.Parameters.AddWithValue("@Duration", e.Duration.Ticks);
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

        public void CloneDatabase(string connectionStringSource, string connectionStringDestination)
        {
            throw new InvalidOperationException();
        }

        public void Close()
        {
            _connection.Close();
        }

        public bool CreateEmptyDatabase(string connectionString, string collate)
        {
            var csb = new SQLiteConnectionStringBuilder(connectionString);
            var databaseName = csb.DataSource;
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;
            csb.Remove("Database");
            csb.Remove("CharacterSet");
            using (var connection = new SQLiteConnection(csb.ConnectionString))
            {
                connection.Open();
                using (var createCommand = new SQLiteCommand($"CREATE DATABASE `{databaseName}` COLLATE = {collate};", connection))
                {
                    if (createCommand.ExecuteNonQuery() == 1)
                    {
                        using (var useCommand = new SQLiteCommand($"use {databaseName};", connection))
                        {
                            useCommand.ExecuteNonQuery();
                            using (var scriptReader = new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Database.MySqlRedundant.database.sql")))
                            {
                                var createStatements = scriptReader.ReadToEnd();
                                var createScript = new SQLiteCommand( createStatements,connection);
                                if (createScript.ExecuteNonQuery() > 0)
                                    return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        public void DeleteArchiveDirectory(IArchiveDirectoryProperties dir)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM archive WHERE idArchive=@idArchive;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteEngine(IEnginePersistent engine)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM engine WHERE idEngine=@idEngine;", _connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public bool DeleteEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM engine_acl WHERE idEngine_ACL=@idEngine_ACL;", _connection);
                cmd.Parameters.AddWithValue("@idEngine_ACL", acl.Id);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public bool DeleteEvent(IEventPersistent aEvent)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM rundownevent WHERE idRundownEvent=@idRundownEvent;", _connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                cmd.ExecuteNonQuery();
                Debug.WriteLine("DbDeleteEvent Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                return true;
            }
        }

        public bool DeleteEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (_connection)
            {
                var query = "DELETE FROM rundownevent_acl WHERE idRundownevent_ACL=@idRundownevent_ACL;";
                var cmd = new SQLiteCommand(query, _connection);
                cmd.Parameters.AddWithValue("@idRundownevent_ACL", acl.Id);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public bool DeleteMedia(IAnimatedMedia animatedMedia)
        {
            lock (_connection)
            {
                var result = false;
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        result = _deleteServerMedia(animatedMedia);
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

        private bool _deleteServerMedia(IPersistentMedia serverMedia)
        {
            var cmd = new SQLiteCommand("DELETE FROM servermedia WHERE idServerMedia=@idServerMedia;", _connection);
            cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.IdPersistentMedia);
            return cmd.ExecuteNonQuery() == 1;
        }

        private bool _delete_media_templated(IAnimatedMedia media)
        {
            try
            {
                var cmd = new SQLiteCommand(@"DELETE FROM media_templated WHERE MediaGuid = @MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"_delete_media_templated failed with {e.Message}");
                return false;
            }
        }

        public bool DeleteMedia(IArchiveMedia archiveMedia)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM archivemedia WHERE idArchiveMedia=@idArchiveMedia;", _connection);
                cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.IdPersistentMedia);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public bool DeleteMedia(IServerMedia serverMedia)
        {
            lock (_connection)
            {
                return _deleteServerMedia(serverMedia);
            }
        }

        public void DeleteMediaSegment(IMediaSegment mediaSegment)
        {
            if (!(mediaSegment is IPersistent ps) || ps.Id == 0)
                return;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM mediasegments WHERE idMediaSegment=@idMediaSegment;", _connection);
                cmd.Parameters.AddWithValue("@idMediaSegment", ps.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSecurityObject(ISecurityObject aco)
        {
            if (!(aco is IPersistent pAco) || pAco.Id == 0)
                throw new ArgumentNullException(nameof(aco));
            lock (_connection)
            {
                {
                    var cmd = new SQLiteCommand(@"delete from aco where idACO=@idACO;", _connection);
                    cmd.Parameters.AddWithValue("@idACO", pAco.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteServer(IPlayoutServerProperties server)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("DELETE FROM server WHERE idServer=@idServer;", _connection);
                cmd.Parameters.AddWithValue("@idServer", server.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public bool DropDatabase(string connectionString)
        {
            var csb = new SQLiteConnectionStringBuilder(connectionString);
            var databaseName = csb.DataSource;
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;
            csb.Remove("Database");
            using (var connection = new SQLiteConnection(csb.ConnectionString))
            {
                connection.Open();
                using (var dropCommand = new SQLiteCommand($"DROP DATABASE `{databaseName}`;", connection))
                {
                    if (dropCommand.ExecuteNonQuery() > 0)
                        return true;
                }
                return false;
            }
        }

        public List<T> FindArchivedStaleMedia<T>(IArchiveDirectoryServerSide dir) where T : IArchiveMedia, new()
        {
            var returnList = new List<T>();
            lock (_connection)
            {
                var cmd = new SQLiteCommand(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and KillDate<CURRENT_DATE and KillDate>'2000-01-01' LIMIT 0, 1000;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                        returnList.Add(_readArchiveMedia<T>(dataReader));
                    dataReader.Close();
                }
            }
            return returnList;
        }

        private T _readArchiveMedia<T>(SQLiteDataReader dataReader) where T : IArchiveMedia, new()
        {
            var media = new T
            {
                IdPersistentMedia = dataReader.GetUInt64("idArchiveMedia")
            };
            _mediaReadFields(media, dataReader);
            return media;
        }

        private void _mediaReadFields(IPersistentMedia media, SQLiteDataReader dataReader)
        {
            var flags = dataReader.IsDBNull("flags") ? 0 : dataReader.GetInt32("flags");
            media.MediaName = dataReader.IsDBNull("MediaName") ? string.Empty : dataReader.GetString("MediaName");
            media.LastUpdated = dataReader.GetDateTime("LastUpdated");
            media.MediaGuid = dataReader.GetGuid("MediaGuid");
            media.MediaType = (TMediaType)(dataReader.IsDBNull("typMedia") ? 0 : dataReader.GetInt32("typMedia"));
            media.Duration = dataReader.IsDBNull("Duration") ? default(TimeSpan) : dataReader.GetTimeSpan("Duration");
            media.DurationPlay = dataReader.IsDBNull("DurationPlay") ? default(TimeSpan) : dataReader.GetTimeSpan("DurationPlay");
            media.Folder = dataReader.IsDBNull("Folder") ? string.Empty : dataReader.GetString("Folder");
            media.FileName = dataReader.IsDBNull("FileName") ? string.Empty : dataReader.GetString("FileName");
            media.FileSize = dataReader.IsDBNull("FileSize") ? 0 : dataReader.GetUInt64("FileSize");
            media.MediaStatus = (TMediaStatus)(dataReader.IsDBNull("statusMedia") ? 0 : dataReader.GetInt32("statusMedia"));
            media.TcStart = dataReader.IsDBNull("TCStart") ? default(TimeSpan) : dataReader.GetTimeSpan("TCStart");
            media.TcPlay = dataReader.IsDBNull("TCPlay") ? default(TimeSpan) : dataReader.GetTimeSpan("TCPlay");
            media.IdProgramme = dataReader.IsDBNull("idProgramme") ? 0 : dataReader.GetUInt64("idProgramme");
            media.AudioVolume = dataReader.IsDBNull("AudioVolume") ? 0 : dataReader.GetDouble("AudioVolume");
            media.AudioLevelIntegrated = dataReader.IsDBNull("AudioLevelIntegrated") ? 0 : dataReader.GetDouble("AudioLevelIntegrated");
            media.AudioLevelPeak = dataReader.IsDBNull("AudioLevelPeak") ? 0 : dataReader.GetDouble("AudioLevelPeak");
            media.AudioChannelMapping = dataReader.IsDBNull("typAudio") ? TAudioChannelMapping.Stereo : (TAudioChannelMapping)dataReader.GetByte("typAudio");
            media.VideoFormat = dataReader.IsDBNull("typVideo") ? TVideoFormat.Other : (TVideoFormat)dataReader.GetByte("typVideo");
            media.IdAux = dataReader.IsDBNull("idAux") ? string.Empty : dataReader.GetString("idAux");
            media.KillDate = dataReader.IsDBNull("KillDate") ? (DateTime?)null : dataReader.GetDateTime("KillDate");
            media.MediaEmphasis = (TMediaEmphasis)((flags >> 8) & 0xF);
            media.Parental = (byte)((flags >> 12) & 0xF);
            if (media is IServerMedia serverMedia)
                serverMedia.DoNotArchive = (flags & 0x1) != 0;
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
            media.IsModified = false;
        }

        private Dictionary<string, Dictionary<string, int>> _tablesStringFieldsLenghts;

        public void InitializeFieldLengths()
        {
            
        }

        public void InsertArchiveDirectory(IArchiveDirectoryProperties dir)
        {
            lock (_connection)
            {
                {
                    var cmd = new SQLiteCommand(@"INSERT INTO archive (Folder) VALUES (@Folder)", _connection);
                    cmd.Parameters.AddWithValue("@Folder", dir.Folder);
                    cmd.ExecuteNonQuery();
                    dir.IdArchive = (ulong)_connection.LastInsertRowId;
                }
            }
        }

        public void InsertEngine(IEnginePersistent engine)
        {
            lock (_connection)
            {
                {                   
                    var cmd = new SQLiteCommand(@"INSERT INTO engine (Instance, idServerPRI, ServerChannelPRI, idServerSEC, ServerChannelSEC, idServerPRV, ServerChannelPRV, idArchive, Config) VALUES(@Instance, @idServerPRI, @ServerChannelPRI, @idServerSEC, @ServerChannelSEC, @idServerPRV, @ServerChannelPRV, @idArchive, @Config);", _connection);
                    cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                    cmd.Parameters.AddWithValue("@idServerPRI", engine.IdServerPRI);
                    cmd.Parameters.AddWithValue("@ServerChannelPRI", engine.ServerChannelPRI);
                    cmd.Parameters.AddWithValue("@idServerSEC", engine.IdServerSEC);
                    cmd.Parameters.AddWithValue("@ServerChannelSEC", engine.ServerChannelSEC);
                    cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                    cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                    cmd.Parameters.AddWithValue("@IdArchive", engine.IdArchive);
                    var serializer = new XmlSerializer(engine.GetType());
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, engine);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.ExecuteNonQuery();
                    engine.Id = (ulong)_connection.LastInsertRowId;
                }
            }
        }

        public bool InsertEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            if (acl?.Owner == null)
                return false;
            lock (_connection)
            {
                using (var cmd =
                    new SQLiteCommand(
                        "INSERT INTO engine_acl (idEngine, idACO, ACL) VALUES (@idEngine, @idACO, @ACL);",
                        _connection))
                {
                    cmd.Parameters.AddWithValue("@idEngine", acl.Owner.Id);
                    cmd.Parameters.AddWithValue("@idACO", acl.SecurityObject.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    if (cmd.ExecuteNonQuery() == 1)
                        acl.Id = (ulong)_connection.LastInsertRowId;
                    return true;
                }
            }
        }

        public bool InsertEvent(IEventPersistent aEvent)
        {
            lock (_connection)
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    const string query = @"INSERT INTO RundownEvent 
(idEngine, idEventBinding, Layer, typEvent, typStart, ScheduledTime, ScheduledDelay, Duration, ScheduledTC, MediaGuid, EventName, PlayState, StartTime, StartTC, RequestedStartTime, TransitionTime, TransitionPauseTime, typTransition, AudioVolume, idProgramme, flagsEvent, Commands, RouterPort) 
VALUES 
(@idEngine, @idEventBinding, @Layer, @typEvent, @typStart, @ScheduledTime, @ScheduledDelay, @Duration, @ScheduledTC, @MediaGuid, @EventName, @PlayState, @StartTime, @StartTC, @RequestedStartTime, @TransitionTime, @TransitionPauseTime, @typTransition, @AudioVolume, @idProgramme, @flagsEvent, @Commands, @RouterPort);";
                    using (var cmd = new SQLiteCommand(query, _connection))
                        if (_eventFillParamsAndExecute(cmd, aEvent))
                        {
                            aEvent.Id = (ulong)_connection.LastInsertRowId;
                            Debug.WriteLine("DbInsertEvent Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                            if (aEvent is ITemplated eventTemplated)
                                _eventAnimatedSave(aEvent.Id, eventTemplated, true);
                            transaction.Commit();
                            return true;
                        }
                }
            }
            return false;
        }

        private bool _eventFillParamsAndExecute(SQLiteCommand cmd, IEventPersistent aEvent)
        {
            Debug.WriteLineIf(aEvent.Duration.Days > 1, aEvent, "Duration extremely long");
            cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)aEvent.Engine).Id);
            cmd.Parameters.AddWithValue("@idEventBinding", aEvent.IdEventBinding);
            cmd.Parameters.AddWithValue("@Layer", (sbyte)aEvent.Layer);
            cmd.Parameters.AddWithValue("@typEvent", aEvent.EventType);
            cmd.Parameters.AddWithValue("@typStart", aEvent.StartType);

            cmd.Parameters.AddWithValue("@ScheduledTime", DBNull.Value);

            cmd.Parameters.AddWithValue("@Duration", aEvent.Duration.Ticks);
            if (aEvent.ScheduledTc.Equals(TimeSpan.Zero))
                cmd.Parameters.AddWithValue("@ScheduledTC", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@ScheduledTC", aEvent.ScheduledTc.Ticks);
            cmd.Parameters.AddWithValue("@ScheduledDelay", aEvent.ScheduledDelay.Ticks);
            if (aEvent.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", aEvent.MediaGuid);
            cmd.Parameters.AddWithValue("@EventName", aEvent.EventName);
            cmd.Parameters.AddWithValue("@PlayState", aEvent.PlayState);
            cmd.Parameters.AddWithValue("@StartTime", DBNull.Value);
            if (aEvent.StartTc.Equals(TimeSpan.Zero))
                cmd.Parameters.AddWithValue("@StartTC", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@StartTC", aEvent.StartTc.Ticks);
            if (aEvent.RequestedStartTime == null)
                cmd.Parameters.AddWithValue("@RequestedStartTime", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@RequestedStartTime", aEvent.RequestedStartTime);
            cmd.Parameters.AddWithValue("@TransitionTime", aEvent.TransitionTime.Ticks);
            cmd.Parameters.AddWithValue("@TransitionPauseTime", aEvent.TransitionPauseTime.Ticks);
            cmd.Parameters.AddWithValue("@typTransition", (ushort)aEvent.TransitionType | ((ushort)aEvent.TransitionEasing) << 8);
            cmd.Parameters.AddWithValue("@idProgramme", aEvent.IdProgramme);
            if (aEvent.AudioVolume == null)
                cmd.Parameters.AddWithValue("@AudioVolume", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@AudioVolume", aEvent.AudioVolume);
            cmd.Parameters.AddWithValue("@flagsEvent", aEvent.ToFlags());
            cmd.Parameters.AddWithValue("@RouterPort", aEvent.RouterPort == -1 ? (object)DBNull.Value : aEvent.RouterPort);
            var command = aEvent.EventType == TEventType.CommandScript && aEvent is ICommandScript
                ? (object)((ICommandScript)aEvent).Command
                : DBNull.Value;
            cmd.Parameters.AddWithValue("@Commands", command);
            return cmd.ExecuteNonQuery() == 1;
        }

        private void _eventAnimatedSave(ulong id, ITemplated e, bool inserting)
        {
            var query = inserting ?
                @"INSERT INTO `rundownevent_templated` (`idrundownevent_templated`, `Method`, `TemplateLayer`, `Fields`) VALUES (@idrundownevent_templated, @Method, @TemplateLayer, @Fields);" :
                @"UPDATE `rundownevent_templated` SET  `Method`=@Method, `TemplateLayer`=@TemplateLayer, `Fields`=@Fields WHERE `idrundownevent_templated`=@idrundownevent_templated;";
            using (var cmd = new SQLiteCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@idrundownevent_templated", id);
                cmd.Parameters.AddWithValue("@Method", (byte)e.Method);
                cmd.Parameters.AddWithValue("@TemplateLayer", e.TemplateLayer);
                var fields = Newtonsoft.Json.JsonConvert.SerializeObject(e.Fields);
                cmd.Parameters.AddWithValue("@Fields", fields);
                cmd.ExecuteNonQuery();
            }
        }

        public bool InsertEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            if (acl?.Owner == null)
                return false;
            lock (_connection)
            {
                using (var cmd =
                    new SQLiteCommand(
                        "INSERT INTO rundownevent_acl (idRundownEvent, idACO, ACL) VALUES (@idRundownEvent, @idACO, @ACL);",
                        _connection))
                {
                    cmd.Parameters.AddWithValue("@idRundownEvent", acl.Owner.Id);
                    cmd.Parameters.AddWithValue("@idACO", acl.SecurityObject.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    if (cmd.ExecuteNonQuery() == 1)
                        acl.Id = (ulong)_connection.LastInsertRowId;
                    return true;
                }
            }
        }

        public bool InsertMedia(IAnimatedMedia animatedMedia, ulong serverId)
        {
            var result = false;
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

        private bool _dbInsertMedia(IPersistentMedia media, ulong serverId)
        {
            var cmd = new SQLiteCommand(@"INSERT INTO servermedia 
(idServer, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idServer, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);", _connection);
            _mediaFillParamsAndExecute(cmd, "servermedia", media, serverId);
            media.IdPersistentMedia = (ulong)_connection.LastInsertRowId;
            Debug.WriteLine(media, "ServerMediaInserte-d");
            return true;
        }

        public bool InsertMedia(IArchiveMedia archiveMedia, ulong serverid)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand(@"INSERT INTO archivemedia 
(idArchive, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idArchive, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);", _connection);
                _mediaFillParamsAndExecute(cmd, "archivemedia", archiveMedia, serverid);
                archiveMedia.IdPersistentMedia = (ulong)_connection.LastInsertRowId;
            }
            return true;
        }

        private bool _insert_media_templated(IAnimatedMedia media)
        {
            try
            {
                var cmd = new SQLiteCommand(@"INSERT IGNORE INTO media_templated (MediaGuid, Fields, TemplateLayer, Method) VALUES (@MediaGuid, @Fields, @TemplateLayer, @Method);", _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                cmd.Parameters.AddWithValue("@TemplateLayer", media.TemplateLayer);
                cmd.Parameters.AddWithValue("@Method", (byte)media.Method);
                cmd.Parameters.AddWithValue("@Fields", Newtonsoft.Json.JsonConvert.SerializeObject(media.Fields));

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"_insert_media_templated failed with {e.Message}");
                return false;
            }
        }

        public bool InsertMedia(IServerMedia serverMedia, ulong serverId)
        {
            lock (_connection)
                return _dbInsertMedia(serverMedia, serverId);
        }

        public void InsertSecurityObject(ISecurityObject aco)
        {
            if (!(aco is IPersistent pAco))
                throw new ArgumentNullException(nameof(aco));
            lock (_connection)
            {
                {
                    var cmd = new SQLiteCommand(@"insert into aco (typAco, Config) VALUES (@typAco, @Config);", _connection);
                    var serializer = new XmlSerializer(pAco.GetType());
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, pAco);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.Parameters.AddWithValue("@typAco", (int)aco.SecurityObjectTypeType);
                    cmd.ExecuteNonQuery();
                    pAco.Id = (ulong)_connection.LastInsertRowId;
                }
            }
        }

        public void InsertServer(IPlayoutServerProperties server)
        {
            lock (_connection)
            {
                {
                    var cmd = new SQLiteCommand(@"INSERT INTO server VALUES(typServer=0, Config=@Config);", _connection);
                    var serializer = new XmlSerializer(server.GetType());
                    using (var writer = new StringWriter())
                    {
                        serializer.Serialize(writer, server);
                        cmd.Parameters.AddWithValue("@Config", writer.ToString());
                    }
                    cmd.ExecuteNonQuery();
                    server.Id = (ulong)_connection.LastInsertRowId;
                }
            }
        }

        public List<T> Load<T>() where T : ISecurityObject
        {
            var users = new List<T>();
            lock (_connection)
            {
                var cmd = new SQLiteCommand("select * from aco where typACO=@typACO;", _connection);
                if (typeof(IUser).IsAssignableFrom(typeof(T)))
                    cmd.Parameters.AddWithValue("@typACO", (int)SecurityObjectType.User);
                if (typeof(IGroup).IsAssignableFrom(typeof(T)))
                    cmd.Parameters.AddWithValue("@typACO", (int)SecurityObjectType.Group);
                using (SQLiteDataReader dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var reader = new StringReader(dataReader.GetString("Config"));
                        var serializer = new XmlSerializer(typeof(T));
                        var user = (T)serializer.Deserialize(reader);
                        if (user is IPersistent pUser)
                            pUser.Id = dataReader.GetUInt64("idACO");
                        users.Add(user);
                    }
                    dataReader.Close();
                    return users;
                }
            }
        }

        public void LoadAnimationDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IAnimatedMedia, new()
        {
            Debug.WriteLine(directory, "AnimationDirectory load started");
            lock (_connection)
            {
                SQLiteCommand cmd = new SQLiteCommand("SELECT servermedia.*, media_templated.`Fields`, media_templated.`Method`, media_templated.`TemplateLayer`, media_templated.`ScheduledDelay`, media_templated.`StartType` FROM serverMedia LEFT JOIN media_templated ON servermedia.MediaGuid = media_templated.MediaGuid WHERE idServer=@idServer and typMedia = @typMedia", _connection);
                cmd.Parameters.AddWithValue("@idServer", serverId);
                cmd.Parameters.AddWithValue("@typMedia", TMediaType.Animation);
                try
                {
                    using (SQLiteDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {

                            var media = new T
                            {
                                IdPersistentMedia = dataReader.GetUInt64("idServerMedia"),
                            };
                            _mediaReadFields(media, dataReader);
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

        public List<T> LoadArchiveDirectories<T>() where T : IArchiveDirectoryProperties, new()
        {
            var directories = new List<T>();
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT * FROM archive;", _connection);
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
            return directories;
        }

        public T LoadArchiveDirectory<T>(ulong idArchive) where T : IArchiveDirectoryServerSide, new()
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT Folder FROM archive WHERE idArchive=@idArchive;", _connection);
                cmd.Parameters.AddWithValue("@idArchive", idArchive);
                var folder = (string)cmd.ExecuteScalar();
                if (string.IsNullOrEmpty(folder))
                    return default(T);
                return new T
                {
                    IdArchive = idArchive,
                    Folder = folder
                };
            }
        }

        public List<T> LoadEngines<T>(ulong? instance = null) where T : IEnginePersistent
        {
            var engines = new List<T>();
            lock (_connection)
            {
                SQLiteCommand cmd;
                if (instance == null)
                    cmd = new SQLiteCommand("SELECT * FROM engine;", _connection);
                else
                {
                    cmd = new SQLiteCommand("SELECT * FROM engine where Instance=@Instance;", _connection);
                    cmd.Parameters.AddWithValue("@Instance", instance);
                }
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var reader = new StringReader(dataReader.GetString("Config"));
                        var serializer = new XmlSerializer(typeof(T));
                        var engine = (T)serializer.Deserialize(reader);
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

        public void LoadServerDirectory<T>(IMediaDirectoryServerSide directory, ulong serverId) where T : IServerMedia, new()
        {
            Debug.WriteLine(directory, "ServerLoadMediaDirectory started");
            lock (_connection)
            {

                var cmd = new SQLiteCommand("SELECT * FROM serverMedia WHERE idServer=@idServer and typMedia in (@typMediaMovie, @typMediaStill)", _connection);
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
                            _mediaReadFields(media, dataReader);
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

        public List<T> LoadServers<T>() where T : IPlayoutServerProperties
        {
            var servers = new List<T>();
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT * FROM server;", _connection);
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var reader = new StringReader(dataReader.GetString("Config"));
                        var serializer = new XmlSerializer(typeof(T));
                        var server = (T)serializer.Deserialize(reader);
                        server.Id = dataReader.GetUInt64("idServer");
                        servers.Add(server);
                    }
                    dataReader.Close();
                }
            }
            return servers;
        }

        public MediaDeleteResult MediaInUse(IEngine engine, IServerMedia serverMedia)
        {
            var reason = MediaDeleteResult.NoDeny;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("select * from rundownevent where MediaGuid=@MediaGuid and (ScheduledTime + Duration) > datetime('now', 'localtime');", _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", serverMedia.MediaGuid);
                IEvent futureScheduled = null;
                using (var reader = cmd.ExecuteReader())
                    if (reader.Read())
                        futureScheduled = _eventRead(engine, reader);
                if (futureScheduled is ITemplated && futureScheduled is IEventPersistent)
                {
                    _readAnimatedEvent(((IEventPersistent)futureScheduled).Id, futureScheduled as ITemplated);
                    ((IEventPersistent)futureScheduled).IsModified = false;
                }
                if (futureScheduled != null)
                    return new MediaDeleteResult { Result = MediaDeleteResult.MediaDeleteResultEnum.InSchedule, Media = serverMedia, Event = futureScheduled };
            }
            return reason;
        }

        private IEvent _eventRead(IEngine engine, SQLiteDataReader dataReader)
        {
            var flags = dataReader.IsDBNull("flagsEvent") ? 0 : dataReader.GetUInt32("flagsEvent");
            var transitionType = dataReader.GetInt16("typTransition");
            var eventType = (TEventType)dataReader.GetByte("typEvent");
            var newEvent = engine.CreateNewEvent(
                dataReader.GetUInt64("idRundownEvent"),
                dataReader.GetUInt64("idEventBinding"),
                (VideoLayer)dataReader.GetByte("Layer"),
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
                routerPort: dataReader.IsDBNull("RouterPort") ? (short)-1 : dataReader.GetInt16("RouterPort")
                );
            return newEvent;
        }

        private void _readAnimatedEvent(ulong id, ITemplated animatedEvent)
        {
            var cmd = new SQLiteCommand("SELECT * FROM `rundownevent_templated` where `idrundownevent_templated` = @id;", _connection);
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
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, WeakReference> _mediaSegments = new System.Collections.Concurrent.ConcurrentDictionary<Guid, WeakReference>();
        private IMediaSegments _findInDictionary(Guid mediaGuid)
        {
            if (!_mediaSegments.TryGetValue(mediaGuid, out var existingRef))
                return null;
            if (existingRef.IsAlive)
                return (IMediaSegments)existingRef.Target;
            _mediaSegments.TryRemove(mediaGuid, out existingRef);
            return null;
        }
        private ConstructorInfo _mediaSegmentsConstructorInfo;

        public T MediaSegmentsRead<T>(IPersistentMedia media) where T : IMediaSegments
        {
            lock (_connection)
            {
                if (_mediaSegmentsConstructorInfo == null)
                    _mediaSegmentsConstructorInfo = typeof(T).GetConstructor(new[] { typeof(Guid) });

                if (_mediaSegmentsConstructorInfo == null)
                    throw new ApplicationException("No constructor found for IMediaSegments");

                var mediaGuid = media.MediaGuid;
                var cmd = new SQLiteCommand("SELECT * FROM MediaSegments where MediaGuid = @MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                var segments = _findInDictionary(mediaGuid);
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
                            dataReader.IsDBNull("TCIn") ? default(TimeSpan) : dataReader.GetTimeSpan("TCIn"),
                            dataReader.IsDBNull("TCOut") ? default(TimeSpan) : dataReader.GetTimeSpan("TCOut"),
                            dataReader.IsDBNull("SegmentName") ? string.Empty : dataReader.GetString("SegmentName")
                            );
                        newSegment.Id = dataReader.GetUInt64("idMediaSegment");
                    }
                    dataReader.Close();
                }
                return (T)segments;
            }
        }

        public void Open(string connectionStringPrimary = null, string connectionStringSecondary = null)
        {
            ConnectionStringPrimary = connectionStringPrimary;
#if DEBUG
            ConnectionStringPrimary = @"Data Source=C:\Users\Emisja\Downloads\blop.db; Version=3; FailIfMissing=False; Foreign Keys=True;";
#endif

            _connection = new SQLiteConnection(ConnectionStringPrimary);
            _connection.Open();
        }

        public List<IAclRight> ReadEngineAclList<TEngineAcl>(IPersistent engine, IAuthenticationServicePersitency authenticationService) where TEngineAcl : IAclRight, IPersistent, new()
        {
            lock (_connection)
            {
                var cmd =
                    new SQLiteCommand("SELECT * FROM engine_acl WHERE idEngine=@idEngine;",
                        _connection);
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

        public IEvent ReadEvent(IEngine engine, ulong idRundownEvent)
        {
            if (idRundownEvent <= 0)
                return null;
            lock (_connection)
            {
                IEvent result = null;
                var cmd = new SQLiteCommand("SELECT * FROM rundownevent where idRundownEvent = @idRundownEvent", _connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", idRundownEvent);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        result = _eventRead(engine, reader);
                }
                if (!(result is ITemplated) || !(result is IEventPersistent))
                    return result;
                _readAnimatedEvent(((IEventPersistent)result).Id, result as ITemplated);
                ((IEventPersistent)result).IsModified = false;
                return result;
            }
        }

        public List<IAclRight> ReadEventAclList<TEventAcl>(IEventPersistent aEvent, IAuthenticationServicePersitency authenticationService) where TEventAcl : IAclRight, IPersistent, new()
        {
            if (aEvent == null)
                return null;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT * FROM rundownevent_acl WHERE idRundownEvent = @idRundownEvent;", _connection);
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

        public IEvent ReadNext(IEngine engine, IEventPersistent aEvent)
        {
            if (aEvent == null)
                return null;
            lock (_connection)
            {
                IEvent next = null;
                var cmd = new SQLiteCommand("SELECT * FROM rundownevent where idEventBinding = @idEventBinding and typStart=@StartType;", _connection);
                cmd.Parameters.AddWithValue("@idEventBinding", aEvent.Id);
                cmd.Parameters.AddWithValue("@StartType", TStartType.After);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        next = _eventRead(engine, reader);
                }
                if (!(next is ITemplated) || !(next is IEventPersistent))
                    return next;
                _readAnimatedEvent(((IEventPersistent)next).Id, next as ITemplated);
                ((IEventPersistent)next).IsModified = false;
                return next;
            }
        }

        public void ReadRootEvents(IEngine engine)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT * FROM rundownevent where typStart in (@StartTypeManual, @StartTypeOnFixedTime, @StartTypeNone) and idEventBinding=0 and idEngine=@idEngine order by ScheduledTime, EventName", _connection);
                cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                cmd.Parameters.AddWithValue("@StartTypeManual", (byte)TStartType.Manual);
                cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", (byte)TStartType.OnFixedTime);
                cmd.Parameters.AddWithValue("@StartTypeNone", (byte)TStartType.None);
                using (var dataReader = cmd.ExecuteReader())
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

        public List<IEvent> ReadSubEvents(IEngine engine, IEventPersistent eventOwner)
        {
            if (eventOwner == null)
                return null;
            lock (_connection)
            {
                var cmd = new SQLiteCommand("SELECT * FROM rundownevent WHERE idEventBinding = @idEventBinding AND (typStart=@StartTypeManual OR typStart=@StartTypeOnFixedTime);", _connection);
                if (eventOwner.EventType == TEventType.Container)
                {
                    cmd.Parameters.AddWithValue("@StartTypeManual", TStartType.Manual);
                    cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", TStartType.OnFixedTime);
                }
                else
                {
                    cmd = new SQLiteCommand("SELECT * FROM rundownevent where idEventBinding = @idEventBinding and typStart in (@StartTypeWithParent, @StartTypeWithParentFromEnd);", _connection);
                    cmd.Parameters.AddWithValue("@StartTypeWithParent", TStartType.WithParent);
                    cmd.Parameters.AddWithValue("@StartTypeWithParentFromEnd", TStartType.WithParentFromEnd);
                }
                cmd.Parameters.AddWithValue("@idEventBinding", eventOwner.Id);
                var subevents = new List<IEvent>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                        subevents.Add(_eventRead(engine, dataReader));
                }
                foreach (var e in subevents)
                    if (e is ITemplated)
                    {
                        _readAnimatedEvent(e.Id, e as ITemplated);
                        ((IEventPersistent)e).IsModified = false;
                    }
                return subevents;
            }
        }

        public ulong SaveMediaSegment(IMediaSegment mediaSegment)
        {
            if (!(mediaSegment is IPersistent ps))
                return 0;
            lock (_connection)
            {
                SQLiteCommand command;
                if (ps.Id == 0)
                    command = new SQLiteCommand("INSERT INTO mediasegments (MediaGuid, TCIn, TCOut, SegmentName) VALUES (@MediaGuid, @TCIn, @TCOut, @SegmentName);", _connection);
                else
                {
                    command = new SQLiteCommand("UPDATE mediasegments SET TCIn = @TCIn, TCOut = @TCOut, SegmentName = @SegmentName WHERE idMediaSegment=@idMediaSegment AND MediaGuid = @MediaGuid;", _connection);
                    command.Parameters.AddWithValue("@idMediaSegment", ps.Id);
                }
                command.Parameters.AddWithValue("@MediaGuid", mediaSegment.Owner.MediaGuid);
                command.Parameters.AddWithValue("@TCIn", mediaSegment.TcIn);
                command.Parameters.AddWithValue("@TCOut", mediaSegment.TcOut);
                command.Parameters.AddWithValue("@SegmentName", mediaSegment.SegmentName);
                command.ExecuteNonQuery();
                return (ulong)_connection.LastInsertRowId;
            }
        }

        public void SearchMissing(IEngine engine)
        {
            {
                lock (_connection)
                {
                    var cmd = new SQLiteCommand("SELECT * FROM rundownevent m WHERE m.idEngine=@idEngine and m.typStart <= @typStart and (SELECT s.idRundownEvent FROM rundownevent s WHERE m.idEventBinding = s.idRundownEvent) IS NULL", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                    cmd.Parameters.AddWithValue("@typStart", (int)TStartType.Manual);
                    var foundEvents = new List<IEvent>();
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            if (engine.GetRootEvents().Any(e => (e as IEventPersistent)?.Id == dataReader.GetUInt64("idRundownEvent")))
                                continue;
                            var newEvent = _eventRead(engine, dataReader);
                            foundEvents.Add(newEvent);
                        }
                        dataReader.Close();
                    }
                    foreach (var e in foundEvents)
                    {
                        if (e is ITemplated et && e is IEventPersistent ep)
                            _readAnimatedEvent(ep.Id, et);
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

        public List<IEvent> SearchPlaying(IEngine engine)
        {
            {
                lock (_connection)
                {
                    var cmd = new SQLiteCommand("SELECT * FROM rundownevent WHERE idEngine=@idEngine and PlayState=@PlayState", _connection);
                    cmd.Parameters.AddWithValue("@idEngine", ((IPersistent)engine).Id);
                    cmd.Parameters.AddWithValue("@PlayState", TPlayState.Playing);
                    var foundEvents = new List<IEvent>();
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var newEvent = _eventRead(engine, dataReader);
                            foundEvents.Add(newEvent);
                        }
                        dataReader.Close();
                    }
                    foreach (var ev in foundEvents)
                        if (ev is ITemplated && ev is IEventPersistent)
                        {
                            _readAnimatedEvent(((IEventPersistent)ev).Id, ev as ITemplated);
                            ((IEventPersistent)ev).IsModified = false;
                        }
                    return foundEvents;
                }
            }
        }

        public void TestConnect(string connectionString)
        {
            _connection.Open();
        }

        public void UpdateArchiveDirectory(IArchiveDirectoryProperties dir)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand(@"UPDATE archive set Folder=@Folder where idArchive=@idArchive", _connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.IdArchive);
                cmd.Parameters.AddWithValue("@Folder", dir.Folder);
                cmd.ExecuteNonQuery();
            }
        }

        public bool UpdateDb()
        {
            var command = new SQLiteCommand("select `value` from `params` where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", _connection);
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
            var schemaUpdates = new System.Resources.ResourceManager("TAS.Database.MySqlRedundant.SchemaUpdates", Assembly.GetExecutingAssembly());
            var resourceEnumerator = schemaUpdates.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true).GetEnumerator();
            var updatesPending = new SortedList<int, string>();
            while (resourceEnumerator.MoveNext())
            {
                if (resourceEnumerator.Key is string s && resourceEnumerator.Value is string)
                {
                    var regexMatchRes = System.Text.RegularExpressions.Regex.Match(s, @"\d+");
                    if (regexMatchRes.Success
                        && int.TryParse(regexMatchRes.Value, out var resVersionNr)
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
                        try
                        {
                            var cmdUpdateVersion = new SQLiteCommand($"update `params` set `value` = \"{kvp.Key}\" where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", _connection);
                            cmdUpdateVersion.ExecuteNonQuery();
                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public void UpdateEngine(IEnginePersistent engine)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand(@"UPDATE engine set Instance=@Instance, idServerPRI=@idServerPRI, ServerChannelPRI=@ServerChannelPRI, idServerSEC=@idServerSEC, ServerChannelSEC=@ServerChannelSEC, idServerPRV=@idServerPRV, ServerChannelPRV=@ServerChannelPRV, idArchive=@idArchive, Config=@Config where idEngine=@idEngine", _connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.Parameters.AddWithValue("@Instance", engine.Instance);
                cmd.Parameters.AddWithValue("@idServerPRI", engine.IdServerPRI);
                cmd.Parameters.AddWithValue("@ServerChannelPRI", engine.ServerChannelPRI);
                cmd.Parameters.AddWithValue("@idServerSEC", engine.IdServerSEC);
                cmd.Parameters.AddWithValue("@ServerChannelSEC", engine.ServerChannelSEC);
                cmd.Parameters.AddWithValue("@idServerPRV", engine.IdServerPRV);
                cmd.Parameters.AddWithValue("@ServerChannelPRV", engine.ServerChannelPRV);
                cmd.Parameters.AddWithValue("@IdArchive", engine.IdArchive);
                var serializer = new XmlSerializer(engine.GetType());
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, engine);
                    cmd.Parameters.AddWithValue("@Config", writer.ToString());
                }
                cmd.ExecuteNonQuery();
            }
        }

        public bool UpdateEngineAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (_connection)
            {
                using (var cmd =
                    new SQLiteCommand(
                        "UPDATE engine_acl SET ACL=@ACL WHERE idEngine_ACL=@idEngine_ACL;",
                        _connection))
                {
                    cmd.Parameters.AddWithValue("@idEngine_ACL", acl.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        public bool UpdateEvent<TEvent>(TEvent aEvent) where TEvent : IEventPersistent
        {
            lock (_connection)
            {
                using (var transaction = _connection.BeginTransaction())
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
RouterPort=@RouterPort
WHERE idRundownEvent=@idRundownEvent;";
                    using (var cmd = new SQLiteCommand(query, _connection))
                    {
                        cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.Id);
                        if (!_eventFillParamsAndExecute(cmd, aEvent))
                            return false;
                        Debug.WriteLine("DbUpdateEvent Id={0}, EventName={1}", aEvent.Id, aEvent.EventName);
                        if (aEvent is ITemplated eventTemplated)
                            _eventAnimatedSave(aEvent.Id, eventTemplated, false);
                        transaction.Commit();
                        return true;
                    }
                }
            }
        }

        public bool UpdateEventAcl<TEventAcl>(TEventAcl acl) where TEventAcl : IAclRight, IPersistent
        {
            lock (_connection)
            {
                using (var cmd =
                    new SQLiteCommand(
                        "UPDATE rundownevent_acl SET ACL=@ACL WHERE idRundownevent_ACL=@idRundownevent_ACL;",
                        _connection))
                {
                    cmd.Parameters.AddWithValue("@idRundownevent_ACL", acl.Id);
                    cmd.Parameters.AddWithValue("@ACL", acl.Acl);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
        }

        public void UpdateMedia(IAnimatedMedia animatedMedia, ulong serverId)
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

        public void UpdateMedia(IArchiveMedia archiveMedia, ulong serverId)
        {
            lock (_connection)
            {
                var cmd = new SQLiteCommand(@"UPDATE archivemedia SET 
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
WHERE idArchiveMedia=@idArchiveMedia;", _connection);
                cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.IdPersistentMedia);
                _mediaFillParamsAndExecute(cmd, "archivemedia", archiveMedia, serverId);
                Debug.WriteLine(archiveMedia, "ArchiveMediaUpdate-d");
            }
        }

        public void UpdateMedia(IServerMedia serverMedia, ulong serverId)
        {
            lock (_connection)
                _dbUpdateMedia(serverMedia, serverId);
        }

        private void _dbUpdateMedia(IPersistentMedia serverMedia, ulong serverId)
        {
            var cmd = new SQLiteCommand(@"UPDATE servermedia SET 
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
WHERE idServerMedia=@idServerMedia;", _connection);
            cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.IdPersistentMedia);
            _mediaFillParamsAndExecute(cmd, "servermedia", serverMedia, serverId);
            Debug.WriteLine(serverMedia, "ServerMediaUpdate-d");
        }

        private void _mediaFillParamsAndExecute(SQLiteCommand cmd, string tableName, IPersistentMedia media, ulong serverId)
        {
            cmd.Parameters.AddWithValue("@idProgramme", media.IdProgramme);
            cmd.Parameters.AddWithValue("@idAux", media.IdAux);
            if (media.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
            if (media.KillDate == null)
                cmd.Parameters.AddWithValue("@KillDate", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@KillDate", media.KillDate);
            var flags = ((media is IServerMedia serverMedia && serverMedia.DoNotArchive) ? 0x1 : (uint)0x0)
                        | (media.IsProtected ? 0x2 : (uint)0x0)
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
            if (media is IArchiveMedia && media.Directory is IArchiveDirectoryServerSide archiveDirectory)
            {
                cmd.Parameters.AddWithValue("@idArchive", archiveDirectory.IdArchive);
                cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
            }
            cmd.Parameters.AddWithValue("@MediaName", media.MediaName);
            cmd.Parameters.AddWithValue("@Duration", media.Duration.Ticks);
            cmd.Parameters.AddWithValue("@DurationPlay", media.DurationPlay.Ticks);
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
            cmd.Parameters.AddWithValue("@TCStart", media.TcStart.Ticks);
            cmd.Parameters.AddWithValue("@TCPlay", media.TcPlay.Ticks);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            { Debug.WriteLine(media, e.Message); }
        }

        private void _update_media_templated(IAnimatedMedia media)
        {
            try
            {
                var cmd = new SQLiteCommand(@"UPDATE media_templated SET Fields = @Fields, TemplateLayer=@TemplateLayer, ScheduledDelay=@ScheduledDelay, StartType=@StartType, Method=@Method WHERE MediaGuid = @MediaGuid;", _connection);
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                cmd.Parameters.AddWithValue("@TemplateLayer", media.TemplateLayer);
                cmd.Parameters.AddWithValue("@Method", (byte)media.Method);
                cmd.Parameters.AddWithValue("@ScheduledDelay", media.ScheduledDelay.Ticks);
                cmd.Parameters.AddWithValue("@StartType", (byte)media.StartType);
                cmd.Parameters.AddWithValue("@Fields", Newtonsoft.Json.JsonConvert.SerializeObject(media.Fields));
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"_update_media_templated failed with {e.Message}");
            }
        }

        public bool UpdateRequired()
        {
            var command =
                new SQLiteCommand(
                    "select `value` from `params` where `SECTION`=\"DATABASE\" and `key`=\"VERSION\"", _connection);
            var dbVersionNr = 0;
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
            var schemaUpdates = new System.Resources.ResourceManager("TAS.Database.MySqlRedundant.SchemaUpdates",
                Assembly.GetExecutingAssembly());
            var resourceEnumerator = schemaUpdates
                .GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true).GetEnumerator();
            while (resourceEnumerator.MoveNext())
            {
                if (!(resourceEnumerator.Key is string) || !(resourceEnumerator.Value is string))
                    continue;
                var regexMatchRes = System.Text.RegularExpressions.Regex.Match((string)resourceEnumerator.Key, @"\d+");
                if (regexMatchRes.Success
                    && int.TryParse(regexMatchRes.Value, out var resVersionNr)
                    && resVersionNr > dbVersionNr)
                    return true;
            }
            return false;
        }

        public void UpdateSecurityObject(ISecurityObject aco)
        {
            if (!(aco is IPersistent pAco) || pAco.Id == 0)
                throw new ArgumentNullException(nameof(aco));
            lock (_connection)
            {
                {
                    var cmd = new SQLiteCommand(@"update aco set Config=@Config where idACO=@idACO;", _connection);
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

        public void UpdateServer(IPlayoutServerProperties server)
        {
            lock (_connection)
            {

                var cmd = new SQLiteCommand("UPDATE server SET Config=@Config WHERE idServer=@idServer;", _connection);
                cmd.Parameters.AddWithValue("@idServer", server.Id);
                var serializer = new XmlSerializer(server.GetType());
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, server);
                    cmd.Parameters.AddWithValue("@Config", writer.ToString());
                }
                cmd.ExecuteNonQuery();
            }
        }
    }
}
