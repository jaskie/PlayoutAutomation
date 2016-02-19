
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Collections;
using TAS.Common;
using TAS.Server;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.Database;

namespace TAS.Data
{
    public static class DatabaseConnector
    {
        private static DbConnectionRedundant connection;

        public static void Initialize(string connectionString, string connectionStringSecondary)
        {
            connection = new DbConnectionRedundant(connectionString, connectionStringSecondary);
            Debug.WriteLine(connection, "DbConnection initialized");
            connection.Open();
        }

        public static DbConnectionRedundant Connection { get { return connection; } }

        internal static void DbReadRootEvents(this Engine engine)
        {
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where typStart in (@StartTypeManual, @StartTypeOnFixedTime, @StartTypeNone) and idEventBinding=0 and idEngine=@idEngine order by ScheduledTime, EventName", connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                cmd.Parameters.AddWithValue("@StartTypeManual", (byte)TStartType.Manual);
                cmd.Parameters.AddWithValue("@StartTypeOnFixedTime", (byte)TStartType.OnFixedTime);
                cmd.Parameters.AddWithValue("@StartTypeNone", (byte)TStartType.None);
                Event NewEvent;
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        NewEvent = _EventRead(engine, dataReader);
                        engine.RootEvents.Add(NewEvent);
                    }
                    dataReader.Close();
                }
                Debug.WriteLine(engine, "EventReadRootEvents read");
            }
        }

        internal static void DbSearchMissing(this Engine engine)
        {
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM rundownevent m WHERE m.idEngine=@idEngine and (SELECT s.idRundownEvent FROM rundownevent s WHERE m.idEventBinding = s.idRundownEvent) IS NULL", connection);
                cmd.Parameters.AddWithValue("@idEngine", engine.Id);
                Event newEvent;
                List<Event> foundEvents = new List<Event>();
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        lock (engine.RootEvents.SyncRoot)
                            if (!engine.RootEvents.Any(e => e.IdRundownEvent == dataReader.GetUInt64("idRundownEvent")))
                            {
                                newEvent = _EventRead(engine, dataReader);
                                foundEvents.Add(newEvent);
                            }
                    }
                    dataReader.Close();
                }
                foreach (Event e in foundEvents)
                {
                    e.StartType = TStartType.Manual;
                    e.Save();
                    engine.RootEvents.Add(e);
                }
            }
        }

        internal static SynchronizedCollection<IEvent> DbReadSubEvents(this Event eventOwner)
        {
            lock (connection)
            {
                var EventList = new SynchronizedCollection<IEvent>();
                DbCommandRedundant cmd;
                if (eventOwner != null)
                {
                    cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idEventBinding = @idEventBinding and typStart=@StartType;", connection);
                    cmd.Parameters.AddWithValue("@idEventBinding", eventOwner.IdRundownEvent);
                    if (eventOwner.EventType == TEventType.Container)
                        cmd.Parameters.AddWithValue("@StartType", TStartType.Manual);
                    else
                        cmd.Parameters.AddWithValue("@StartType", TStartType.With);
                    Event NewEvent;
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        try
                        {
                            while (dataReader.Read())
                            {
                                NewEvent = _EventRead(eventOwner.Engine, dataReader);
                                NewEvent._parent = eventOwner;
                                EventList.Add(NewEvent);
                            }
                        }
                        finally
                        {
                            dataReader.Close();
                        }
                    }
                }

                return EventList;
            }
        }

        internal static Event DbReadNext(this Event aEvent)
        {
            lock (connection)
            {
                if (aEvent != null)
                {
                    DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM RundownEvent where idEventBinding = @idEventBinding and typStart=@StartType;", connection);
                    cmd.Parameters.AddWithValue("@idEventBinding", aEvent.IdRundownEvent);
                    cmd.Parameters.AddWithValue("@StartType", TStartType.After);
                    DbDataReaderRedundant reader = cmd.ExecuteReader();
                    try
                    {
                        if (reader.Read())
                            return _EventRead(aEvent.Engine, reader);
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                return null;
            }
        }


        private static Event _EventRead(IEngine engine, DbDataReaderRedundant dataReader)
        {
            Event aEvent = new Event(engine);
            uint flags = dataReader.IsDBNull(dataReader.GetOrdinal("flagsEvent")) ? 0 : dataReader.GetUInt32("flagsEvent");
            aEvent._idRundownEvent = dataReader.GetUInt64("idRundownEvent");
            aEvent._layer = (VideoLayer)dataReader.GetSByte("Layer");
            aEvent._eventType = (TEventType)dataReader.GetByte("typEvent");
            aEvent._startType = (TStartType)dataReader.GetByte("typStart");
            aEvent._scheduledTime = dataReader.GetDateTime("ScheduledTime");
            aEvent._duration = dataReader.IsDBNull(dataReader.GetOrdinal("Duration")) ? default(TimeSpan) : dataReader.GetTimeSpan("Duration");
            aEvent._scheduledDelay = dataReader.IsDBNull(dataReader.GetOrdinal("ScheduledDelay")) ? default(TimeSpan) : dataReader.GetTimeSpan("ScheduledDelay");
            aEvent._scheduledTc = dataReader.IsDBNull(dataReader.GetOrdinal("ScheduledTC")) ? TimeSpan.Zero : dataReader.GetTimeSpan("ScheduledTC");
            aEvent._mediaGuid = (dataReader.IsDBNull(dataReader.GetOrdinal("MediaGuid"))) ? Guid.Empty : dataReader.GetGuid("MediaGuid");
            aEvent._eventName = dataReader.IsDBNull(dataReader.GetOrdinal("EventName")) ? default(string) : dataReader.GetString("EventName");
            var psb = dataReader.GetByte("PlayState");
            aEvent._playState = (TPlayState)psb;
            if (aEvent._playState == TPlayState.Playing || aEvent._playState == TPlayState.Paused)
                aEvent._playState = TPlayState.Aborted;
            if (aEvent._playState == TPlayState.Fading)
                aEvent._playState = TPlayState.Played;
            aEvent._startTime = dataReader.GetDateTime("StartTime");
            aEvent._startTc = dataReader.IsDBNull(dataReader.GetOrdinal("StartTC")) ? TimeSpan.Zero : dataReader.GetTimeSpan("StartTC");
            aEvent._requestedStartTime = dataReader.IsDBNull(dataReader.GetOrdinal("RequestedStartTime")) ? null : (TimeSpan?)dataReader.GetTimeSpan("RequestedStartTime");
            aEvent._transitionTime = dataReader.IsDBNull(dataReader.GetOrdinal("TransitionTime")) ? default(TimeSpan) : dataReader.GetTimeSpan("TransitionTime");
            aEvent._transitionType = (TTransitionType)dataReader.GetByte("typTransition");
            aEvent._audioVolume = dataReader.IsDBNull(dataReader.GetOrdinal("AudioVolume")) ? null : (decimal?)dataReader.GetDecimal("AudioVolume");
            aEvent._idProgramme = dataReader.IsDBNull(dataReader.GetOrdinal("idProgramme")) ? 0 : dataReader.GetUInt64("idProgramme");
            aEvent._idAux = dataReader.IsDBNull(dataReader.GetOrdinal("IdAux")) ? default(string) : dataReader.GetString("IdAux");
            aEvent._isEnabled = (flags & (1 << 0)) != 0;
            aEvent._isHold = (flags & (1 << 1)) != 0;
            aEvent._isLoop = (flags & (1 << 2)) != 0;
            EventGPI.FromUInt64(ref aEvent._gPI, (flags >> 4) & EventGPI.Mask);
            aEvent._nextLoaded = false;
            return aEvent;
        }

        private static DateTime _minMySqlDate = new DateTime(1000, 01, 01);
        private static DateTime _maxMySQLDate = new DateTime(9999, 12, 31, 23, 59, 59);

        private static Boolean _EventFillParamsAndExecute(DbCommandRedundant cmd, Event aEvent)
        {

            Debug.WriteLineIf(aEvent._duration.Days > 1, aEvent, "Duration extremely long");
            cmd.Parameters.AddWithValue("@idEngine", aEvent.Engine.Id);
            cmd.Parameters.AddWithValue("@idEventBinding", aEvent.idEventBinding);
            cmd.Parameters.AddWithValue("@Layer", (sbyte)aEvent._layer);
            cmd.Parameters.AddWithValue("@typEvent", aEvent._eventType);
            cmd.Parameters.AddWithValue("@typStart", aEvent._startType);
            if (aEvent._scheduledTime < _minMySqlDate || aEvent._scheduledTime > _maxMySQLDate)
            {
                cmd.Parameters.AddWithValue("@ScheduledTime", DBNull.Value);
                Debug.WriteLine(aEvent, "null ScheduledTime");
            }
            else
                cmd.Parameters.AddWithValue("@ScheduledTime", aEvent._scheduledTime);
            cmd.Parameters.AddWithValue("@Duration", aEvent._duration);
            if (aEvent._scheduledTc.Equals(TimeSpan.Zero))
                cmd.Parameters.AddWithValue("@ScheduledTC", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@ScheduledTC", aEvent._scheduledTc);
            cmd.Parameters.AddWithValue("@ScheduledDelay", aEvent._scheduledDelay);
            if (aEvent.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", aEvent._mediaGuid);
            cmd.Parameters.AddWithValue("@EventName", aEvent._eventName);
            cmd.Parameters.AddWithValue("@PlayState", aEvent._playState);
            if (aEvent._startTime < _minMySqlDate || aEvent._startTime > _maxMySQLDate)
                cmd.Parameters.AddWithValue("@StartTime", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@StartTime", aEvent._startTime);
            if (aEvent._startTc.Equals(TimeSpan.Zero))
                cmd.Parameters.AddWithValue("@StartTC", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@StartTC", aEvent._startTc);
            if (aEvent._requestedStartTime == null)
                cmd.Parameters.AddWithValue("@RequestedStartTime", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@RequestedStartTime", aEvent._requestedStartTime);
            cmd.Parameters.AddWithValue("@TransitionTime", aEvent._transitionTime);
            cmd.Parameters.AddWithValue("@typTransition", aEvent._transitionType);
            cmd.Parameters.AddWithValue("@idProgramme", aEvent._idProgramme);
            if (aEvent._audioVolume == null)
                cmd.Parameters.AddWithValue("@AudioVolume", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@AudioVolume", aEvent._audioVolume);
            UInt64 flags = Convert.ToUInt64(aEvent._isEnabled) << 0
                         | Convert.ToUInt64(aEvent._isHold) << 1
                         | Convert.ToUInt64(aEvent._isLoop) << 2
                         | aEvent.GPI.ToUInt64() << 4 // of size EventGPI.Size
                         ;
            cmd.Parameters.AddWithValue("@flagsEvent", flags);
            return cmd.ExecuteNonQuery() == 1;
        }

        internal static Boolean DbInsert(this Event aEvent)
        {
            Boolean success = false;
            lock (connection)
            {
                string query =
@"INSERT INTO RundownEvent 
(idEngine, idEventBinding, Layer, typEvent, typStart, ScheduledTime, ScheduledDelay, Duration, ScheduledTC, MediaGuid, EventName, PlayState, StartTime, StartTC, RequestedStartTime, TransitionTime, typTransition, AudioVolume, idProgramme, flagsEvent) 
VALUES 
(@idEngine, @idEventBinding, @Layer, @typEvent, @typStart, @ScheduledTime, @ScheduledDelay, @Duration, @ScheduledTC, @MediaGuid, @EventName, @PlayState, @StartTime, @StartTC, @RequestedStartTime, @TransitionTime, @typTransition, @AudioVolume, @idProgramme, @flagsEvent);";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                success = _EventFillParamsAndExecute(cmd, aEvent);
                aEvent.IdRundownEvent = (UInt64)cmd.LastInsertedId;
                Debug.WriteLineIf(success, aEvent, "Event DbInsert");
            }
            return success;
        }

        internal static Boolean DbUpdate(this Event aEvent)
        {
            lock (connection)
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
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.IdRundownEvent);
                if (_EventFillParamsAndExecute(cmd, aEvent))
                {
                    Debug.WriteLine(aEvent, "Event DbUpdate");
                    return true;
                }
            }
            return false;
        }

        internal static Boolean DbDelete(this Event aEvent)
        {
            Boolean success = false;
            lock (connection)
            {
                string query = "DELETE FROM RundownEvent WHERE idRundownEvent=@idRundownEvent;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idRundownEvent", aEvent.IdRundownEvent);
                cmd.ExecuteNonQuery();
                success = true;
                Debug.WriteLine(aEvent, "Event DbDelete");
            }
            return success;
        }

        private static Boolean _mediaFillParamsAndExecute(DbCommandRedundant cmd, PersistentMedia media)
        {
            cmd.Parameters.AddWithValue("@idProgramme", media.idProgramme);
            cmd.Parameters.AddWithValue("@idAux", media.IdAux);
            if (media.MediaGuid == Guid.Empty)
                cmd.Parameters.AddWithValue("@MediaGuid", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
            if (media.KillDate == default(DateTime))
                cmd.Parameters.AddWithValue("@KillDate", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@KillDate", media.KillDate);
            uint flags = ((media is ServerMedia && (media as ServerMedia).DoNotArchive) ? (uint)0x1 : (uint)0x0)
                        | ((uint)(media.MediaCategory) << 4) // bits 4-7 of 1st byte
                        | ((uint)media.MediaEmphasis << 8) // bits 1-3 of second byte
                        | ((uint)media.Parental << 12) // bits 4-7 of second byte
                        ;
            cmd.Parameters.AddWithValue("@flags", flags);
            if (media is ServerMedia && media.Directory is ServerDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", ((media as ServerMedia).Directory as ServerDirectory).Server.Id);
                cmd.Parameters.AddWithValue("@typVideo", (byte)media.VideoFormat);
            }
            if (media is ServerMedia && media.Directory is AnimationDirectory)
            {
                cmd.Parameters.AddWithValue("@idServer", ((media as ServerMedia).Directory as AnimationDirectory).Server.Id);
                cmd.Parameters.AddWithValue("@typVideo", DBNull.Value);
            }
            if (media is ArchiveMedia && media.Directory is ArchiveDirectory)
            {
                cmd.Parameters.AddWithValue("@idArchive", (((media as ArchiveMedia).Directory) as ArchiveDirectory).idArchive);
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
            cmd.Parameters.AddWithValue("@statusMedia", (Int32)media.MediaStatus);
            cmd.Parameters.AddWithValue("@typMedia", (Int32)media.MediaType);
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

        private static void _mediaReadFields(this PersistentMedia media, DbDataReaderRedundant dataReader)
        {
            uint flags = dataReader.IsDBNull(dataReader.GetOrdinal("flags")) ? (uint)0 : dataReader.GetUInt32("flags");
            media.MediaName = dataReader.IsDBNull(dataReader.GetOrdinal("MediaName")) ? string.Empty : dataReader.GetString("MediaName");
            media.Duration = dataReader.IsDBNull(dataReader.GetOrdinal("Duration")) ? default(TimeSpan) : dataReader.GetTimeSpan("Duration");
            media.DurationPlay = dataReader.IsDBNull(dataReader.GetOrdinal("DurationPlay")) ? default(TimeSpan) : dataReader.GetTimeSpan("DurationPlay");
            media.FullPath = Path.Combine(media.Directory.Folder, dataReader.IsDBNull(dataReader.GetOrdinal("Folder")) ? string.Empty : dataReader.GetString("Folder"), dataReader.IsDBNull(dataReader.GetOrdinal("FileName")) ? string.Empty : dataReader.GetString("FileName"));
            media.FileSize = dataReader.IsDBNull(dataReader.GetOrdinal("FileSize")) ? 0 : dataReader.GetUInt64("FileSize");
            media.LastUpdated = dataReader.GetDateTime("LastUpdated");
            media.MediaStatus = (TMediaStatus)(dataReader.IsDBNull(dataReader.GetOrdinal("statusMedia")) ? 0 : dataReader.GetInt32("statusMedia"));
            media.MediaType = (TMediaType)(dataReader.IsDBNull(dataReader.GetOrdinal("typMedia")) ? 0 : dataReader.GetInt32("typMedia"));
            media.TcStart = dataReader.IsDBNull(dataReader.GetOrdinal("TCStart")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCStart");
            media.TcPlay = dataReader.IsDBNull(dataReader.GetOrdinal("TCPlay")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCPlay");
            media.idProgramme = dataReader.IsDBNull(dataReader.GetOrdinal("idProgramme")) ? 0 : dataReader.GetUInt64("idProgramme");
            media.AudioVolume = dataReader.IsDBNull(dataReader.GetOrdinal("AudioVolume")) ? 0 : dataReader.GetDecimal("AudioVolume");
            media.AudioLevelIntegrated = dataReader.IsDBNull(dataReader.GetOrdinal("AudioLevelIntegrated")) ? 0 : dataReader.GetDecimal("AudioLevelIntegrated");
            media.AudioLevelPeak = dataReader.IsDBNull(dataReader.GetOrdinal("AudioLevelPeak")) ? 0 : dataReader.GetDecimal("AudioLevelPeak");
            media.AudioChannelMapping = dataReader.IsDBNull(dataReader.GetOrdinal("typAudio")) ? TAudioChannelMapping.Stereo : (TAudioChannelMapping)dataReader.GetByte("typAudio");
            media.VideoFormat = (TVideoFormat)(dataReader.IsDBNull(dataReader.GetOrdinal("typVideo")) ? (byte)0 : (byte)(dataReader.GetByte("typVideo") & 0x7F));
            media._idAux = dataReader.IsDBNull(dataReader.GetOrdinal("idAux")) ? string.Empty : dataReader.GetString("idAux");
            media.KillDate = dataReader.GetDateTime("KillDate");
            media.MediaEmphasis = (TMediaEmphasis)((flags >> 8) & 0xF);
            media.Parental = (TParental)((flags >> 12) & 0xF);
            if (media is ServerMedia)
                ((ServerMedia)media)._doNotArchive = (flags & 0x1) != 0;
            media.MediaCategory = (TMediaCategory)((flags >> 4) & 0xF); // bits 4-7 of 1st byte
            media.Modified = false;
        }

        internal static void Load(this AnimationDirectory directory)
        {
            Debug.WriteLine(directory, "ServerLoadMediaDirectory animation started");
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM serverMedia WHERE idServer=@idServer and typMedia = @typMedia", connection);
                cmd.Parameters.AddWithValue("@idServer", directory.Server.Id);
                cmd.Parameters.AddWithValue("@typMedia", TMediaType.AnimationFlash);
                try
                {
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            ServerMedia nm = new ServerMedia(directory, dataReader.GetGuid("MediaGuid"))
                            {
                                idPersistentMedia = dataReader.GetUInt64("idServerMedia"),
                            };
                            nm._mediaReadFields(dataReader);
                            if (nm.MediaStatus != TMediaStatus.Available)
                            {
                                nm.MediaStatus = TMediaStatus.Unknown;
                                ThreadPool.QueueUserWorkItem(o => nm.Verify());
                            }
                            directory.NotifyMediaAdded(nm);
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

        internal static void Load(this ServerDirectory directory)
        {
            Debug.WriteLine(directory, "ServerLoadMediaDirectory started");
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM serverMedia WHERE idServer=@idServer and typMedia in (@typMediaMovie, @typMediaStill)", connection);
                cmd.Parameters.AddWithValue("@idServer", directory.Server.Id);
                cmd.Parameters.AddWithValue("@typMediaMovie", TMediaType.Movie);
                cmd.Parameters.AddWithValue("@typMediaStill", TMediaType.Still);
                try
                {
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            ServerMedia nm = new ServerMedia(directory, dataReader.GetGuid("MediaGuid"))
                            {
                                idPersistentMedia = dataReader.GetUInt64("idServerMedia"),
                            };
                            nm._mediaReadFields(dataReader);
                            if (nm.MediaStatus != TMediaStatus.Available)
                            {
                                nm.MediaStatus = TMediaStatus.Unknown;
                                ThreadPool.QueueUserWorkItem(o => nm.Verify());
                            }
                            directory.NotifyMediaAdded(nm);
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

        internal static Boolean DbInsert(this ServerMedia serverMedia)
        {
            Boolean success = false;
            lock (connection)
            {
                string query =
@"INSERT INTO servermedia 
(idServer, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idServer, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                _mediaFillParamsAndExecute(cmd, serverMedia);
                serverMedia.idPersistentMedia = (UInt64)cmd.LastInsertedId;
                success = true;
                Debug.WriteLineIf(success, serverMedia, "ServerMediaInserte-d");
            }
            return success;
        }

        internal static Boolean DbInsert(this ArchiveMedia archiveMedia)
        {
            Boolean success = false;
            lock (connection)
            {
                string query =
@"INSERT INTO archivemedia 
(idArchive, MediaName, Folder, FileName, FileSize, LastUpdated, Duration, DurationPlay, idProgramme, statusMedia, typMedia, typAudio, typVideo, TCStart, TCPlay, AudioVolume, AudioLevelIntegrated, AudioLevelPeak, idAux, KillDate, MediaGuid, flags) 
VALUES 
(@idArchive, @MediaName, @Folder, @FileName, @FileSize, @LastUpdated, @Duration, @DurationPlay, @idProgramme, @statusMedia, @typMedia, @typAudio, @typVideo, @TCStart, @TCPlay, @AudioVolume, @AudioLevelIntegrated, @AudioLevelPeak, @idAux, @KillDate, @MediaGuid, @flags);";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                _mediaFillParamsAndExecute(cmd, archiveMedia);
                archiveMedia.idPersistentMedia = (UInt64)cmd.LastInsertedId;
                success = true;
            }
            return success;
        }

        internal static Boolean DbDelete(this ServerMedia serverMedia)
        {
            lock (connection)
            {
                string query = "DELETE FROM ServerMedia WHERE idServerMedia=@idServerMedia;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.idPersistentMedia);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        internal static Boolean DbDelete(this ArchiveMedia archiveMedia)
        {
            lock (connection)
            {
                string query = "DELETE FROM archivemedia WHERE idArchiveMedia=@idArchiveMedia;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.idPersistentMedia);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        internal static Boolean DbUpdate(this ServerMedia serverMedia)
        {
            Boolean success = false;
            lock (connection)
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
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idServerMedia", serverMedia.idPersistentMedia);
                success = _mediaFillParamsAndExecute(cmd, serverMedia);
                Debug.WriteLineIf(success, serverMedia, "ServerMediaUpdate-d");
            }
            return success;
        }

        internal static Boolean DbUpdate(this ArchiveMedia archiveMedia)
        {
            Boolean success = false;
            lock (connection)
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
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idArchiveMedia", archiveMedia.idPersistentMedia);
                success = _mediaFillParamsAndExecute(cmd, archiveMedia);
                Debug.WriteLineIf(success, archiveMedia, "ArchiveMediaUpdate-d");
            }
            return success;
        }

        internal static MediaDeleteDenyReason DbMediaInUse(this IServerMedia serverMedia)
        {
            MediaDeleteDenyReason reason = MediaDeleteDenyReason.NoDeny;
            lock (connection)
            {
                string query = "select * from rundownevent where MediaGuid=@MediaGuid and ADDTIME(ScheduledTime, Duration) > UTC_TIMESTAMP();";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@MediaGuid", serverMedia.MediaGuid);
                using (DbDataReaderRedundant reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.MediaInFutureSchedule, Media = serverMedia, Event = _EventRead(null, reader) };
                }
            }
            return reason;
        }

        private static ArchiveMedia _readArchiveMedia(DbDataReaderRedundant dataReader, ArchiveDirectory dir)
        {
            byte typVideo = dataReader.IsDBNull(dataReader.GetOrdinal("typVideo")) ? (byte)0 : dataReader.GetByte("typVideo");
            ArchiveMedia media = new ArchiveMedia(dir, dataReader.GetGuid("MediaGuid"))
            {
                idPersistentMedia = dataReader.GetUInt64("idArchiveMedia"),
            };
            media._mediaReadFields(dataReader);
            ((ArchiveDirectory)dir).NotifyMediaAdded(media);
            ThreadPool.QueueUserWorkItem(o => media.Verify());
            return media;
        }

        internal static void DbSearch(this ArchiveDirectory dir)
        {
            string search = dir.SearchString;
            if (string.IsNullOrWhiteSpace(search))
                return;
            dir.Clear();
            lock (connection)
            {
                var textSearches = from text in search.ToLower().Split(' ').Where(s => !string.IsNullOrEmpty(s)) select "(LOWER(MediaName) LIKE \"%" + text + "%\" or LOWER(FileName) LIKE \"%" + text + "%\")";
                DbCommandRedundant cmd;
                if (dir.SearchMediaCategory == null)
                    cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and " + string.Join(" and ", textSearches) + " LIMIT 0, 1000;", connection);
                else
                {
                    cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and ((flags >> 4) & 3)=@Category and  " + string.Join(" and ", textSearches) + " LIMIT 0, 1000;", connection);
                    cmd.Parameters.AddWithValue("@Category", (uint)dir.SearchMediaCategory);
                }
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                        _readArchiveMedia(dataReader, dir);
                    dataReader.Close();
                }
            }
        }

        internal static void AsRunLogWrite(this IEvent e)
        {
            try
            {
                lock (connection)
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
);", connection);
                    cmd.Parameters.AddWithValue("@ExecuteTime", e.StartTime);
                    IMedia media = e.Media;
                    if (media != null)
                    {
                        cmd.Parameters.AddWithValue("@MediaName", media.MediaName);
                        if (media is PersistentMedia)
                            cmd.Parameters.AddWithValue("@idAuxMedia", (media as PersistentMedia).IdAux);
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
                    cmd.Parameters.AddWithValue("@idProgramme", e.idProgramme);
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

        internal static IEnumerable<ArchiveMedia> DbFindStaleMedia(this ArchiveDirectory dir)
        {
            List<ArchiveMedia> returnList = new List<ArchiveMedia>();
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant(@"SELECT * FROM archivemedia WHERE idArchive=@idArchive and KillDate<CURRENT_DATE and KillDate>'2000-01-01' LIMIT 0, 1000;", connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                        returnList.Add(_readArchiveMedia(dataReader, dir));
                    dataReader.Close();
                }
            }
            return returnList;
        }

        internal static IArchiveMedia DbMediaFind(this ArchiveDirectory dir, IMedia media)
        {
            IArchiveMedia result = null;
            lock (connection)
            {
                DbCommandRedundant cmd;
                if (media.MediaGuid != Guid.Empty)
                {
                    cmd = new DbCommandRedundant("SELECT * FROM archivemedia WHERE idArchive=@idArchive && MediaGuid=@MediaGuid;", connection);
                    cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                    cmd.Parameters.AddWithValue("@MediaGuid", media.MediaGuid);
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (dataReader.Read())
                            result = _readArchiveMedia(dataReader, dir);
                        dataReader.Close();
                    }
                }
            }
            return result;
        }

        internal static ArchiveDirectory LoadArchiveDirectory(this MediaManager manager, UInt64 idArchive)
        {
            lock (connection)
            {
                string query = "SELECT Folder FROM archive WHERE idArchive=@idArchive;";
                string folder = null;
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idArchive", idArchive);
                folder = (string)cmd.ExecuteScalar();
                if (!string.IsNullOrEmpty(folder))
                {
                    ArchiveDirectory directory = new ArchiveDirectory(manager)
                    {
                        idArchive = idArchive,
                        Folder = folder,
                    };
                    directory.Initialize();
                    return directory;
                }
                return null;
            }
        }

        internal static bool FileExists(this ArchiveDirectory dir, string fileName)
        {
            lock (connection)
            {
                string query = "SELECT COUNT(*) FROM archivemedia WHERE idArchive=@idArchive AND FileName=@FileName AND Folder=@Folder;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idArchive", dir.idArchive);
                cmd.Parameters.AddWithValue("@FileName", fileName);
                cmd.Parameters.AddWithValue("@Folder", dir.GetCurrentFolder());
                return (long)cmd.ExecuteScalar() != 0;
            }
        }

        internal static List<Engine> DbLoadEngines(UInt64 instance)
        {
            List<Engine> Engines = new List<Engine>();
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM Engine where Instance=@Instance;", connection);
                cmd.Parameters.AddWithValue("Instance", instance);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        Engine newEngine = SerializationHelper.Deserialize<Engine>(dataReader.GetString("Config"));
                        newEngine.Id = dataReader.GetUInt64("idEngine");
                        newEngine.Instance = dataReader.GetUInt64("Instance");
                        newEngine.IdArchive = dataReader.GetUInt64("idArchive");
                        newEngine.IdServerPRI = dataReader.IsDBNull(dataReader.GetOrdinal("idServerPRI")) ? 0UL : dataReader.GetUInt64("idServerPRI");
                        newEngine.ServerChannelPRI = dataReader.IsDBNull(dataReader.GetOrdinal("ServerChannelPRI")) ? 0 : dataReader.GetInt32("ServerChannelPRI");
                        newEngine.IdServerSEC = dataReader.IsDBNull(dataReader.GetOrdinal("idServerSEC")) ? 0UL : dataReader.GetUInt64("idServerSEC");
                        newEngine.ServerChannelSEC = dataReader.IsDBNull(dataReader.GetOrdinal("ServerChannelSEC")) ? 0 : dataReader.GetInt32("ServerChannelSEC");
                        newEngine.IdServerPRV = dataReader.IsDBNull(dataReader.GetOrdinal("idServerPRV")) ? 0UL : dataReader.GetUInt64("idServerPRV");
                        newEngine.ServerChannelPRV = dataReader.IsDBNull(dataReader.GetOrdinal("ServerChannelPRV")) ? 0 : dataReader.GetInt32("ServerChannelPRV");
                        Engines.Add(newEngine);
                    }
                    dataReader.Close();
                }
            }
            return Engines;
        }

        internal static List<IPlayoutServer> DbLoadServers()
        {
            Debug.WriteLine("Begin loading servers");
            List<IPlayoutServer> servers = new List<IPlayoutServer>();
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM Server;", connection);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        switch ((TServerType)dataReader.GetInt32("typServer"))
                        {
                            case TServerType.Caspar:
                                {
                                    Debug.WriteLine("Adding Caspar server");
                                    string serverParams = dataReader.GetString("Config");
                                    CasparServer newServer = SerializationHelper.Deserialize<CasparServer>(serverParams);
                                    XmlDocument configXml = new XmlDocument();
                                    configXml.Load(new StringReader(serverParams));
                                    newServer.Id = dataReader.GetUInt64("idServer");
                                    XmlNode channelsNode = configXml.SelectSingleNode(@"CasparServer/Channels");
                                    Debug.WriteLine("Adding Caspar channels");
                                    newServer.Channels = SerializationHelper.Deserialize<List<CasparServerChannel>>(channelsNode.OuterXml, "Channels").ConvertAll<IPlayoutServerChannel>(pc => (CasparServerChannel)pc);
                                    servers.Add(newServer);
                                    Debug.WriteLine("Caspar server added");
                                    break;
                                }
                        }
                    }
                    dataReader.Close();
                }
            }
            return servers;
        }

        private static Hashtable _mediaSegments;

        internal static ObservableSynchronizedCollection<IMediaSegment> DbMediaSegmentsRead(this PersistentMedia media)
        {
            lock (connection)
            {
                Guid mediaGuid = media.MediaGuid;
                ObservableSynchronizedCollection<IMediaSegment> segments = null;
                MediaSegment newMediaSegment;
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT * FROM MediaSegments where MediaGuid = @MediaGuid;", connection);
                cmd.Parameters.AddWithValue("@MediaGuid", mediaGuid);
                if (_mediaSegments == null)
                    _mediaSegments = new Hashtable();
                segments = (ObservableSynchronizedCollection<IMediaSegment>)_mediaSegments[mediaGuid];
                if (segments == null)
                {
                    segments = new ObservableSynchronizedCollection<IMediaSegment>();
                    using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            newMediaSegment = new MediaSegment(mediaGuid)
                            {
                                idMediaSegment = dataReader.GetUInt64("idMediaSegment"),
                                SegmentName = (dataReader.IsDBNull(dataReader.GetOrdinal("SegmentName")) ? string.Empty : dataReader.GetString("SegmentName")),
                                TcIn = dataReader.IsDBNull(dataReader.GetOrdinal("TCIn")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCIn"),
                                TcOut = dataReader.IsDBNull(dataReader.GetOrdinal("TCOut")) ? default(TimeSpan) : dataReader.GetTimeSpan("TCOut"),
                            };
                            segments.Add(newMediaSegment);
                        }
                        dataReader.Close();
                    }
                    _mediaSegments.Add(mediaGuid, segments);
                }
                return segments;
            }
        }

        internal static void DbDelete(this MediaSegment mediaSegment)
        {
            if (mediaSegment.idMediaSegment != 0)
            {
                var segments = (ObservableSynchronizedCollection<IMediaSegment>)_mediaSegments[mediaSegment.MediaGuid];
                if (segments != null)
                {
                    segments.Remove(mediaSegment);
                }
                lock (connection)
                {

                    string query = "DELETE FROM mediasegments WHERE idMediaSegment=@idMediaSegment;";
                    DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                    cmd.Parameters.AddWithValue("@idMediaSegment", mediaSegment.idMediaSegment);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        internal static void DbSave(this MediaSegment mediaSegment)
        {
            lock (connection)
            {
                DbCommandRedundant command;
                if (mediaSegment.idMediaSegment == 0)
                    command = new DbCommandRedundant("INSERT INTO mediasegments (MediaGuid, TCIn, TCOut, SegmentName) VALUES (@MediaGuid, @TCIn, @TCOut, @SegmentName);", connection);
                else
                {
                    command = new DbCommandRedundant("UPDATE mediasegments SET TCIn = @TCIn, TCOut = @TCOut, SegmentName = @SegmentName WHERE idMediaSegment=@idMediaSegment AND MediaGuid = @MediaGuid;", connection);
                    command.Parameters.AddWithValue("@idMediaSegment", mediaSegment.idMediaSegment);
                }
                command.Parameters.AddWithValue("@MediaGuid", mediaSegment.MediaGuid);
                command.Parameters.AddWithValue("@TCIn", mediaSegment.TcIn);
                command.Parameters.AddWithValue("@TCOut", mediaSegment.TcOut);
                command.Parameters.AddWithValue("@SegmentName", mediaSegment.SegmentName);
                command.ExecuteNonQuery();
                if (mediaSegment.idMediaSegment == 0)
                    mediaSegment.idMediaSegment = (UInt64)command.LastInsertedId;
            }
        }


        internal static void DbSave(this Template template)
        {
            DbCommandRedundant cmd;
            bool isInsert = template.idTemplate == 0;
            lock (connection)
            {
                if (isInsert)
                {
                    cmd = new DbCommandRedundant(
    @"INSERT INTO template 
(idEngine, MediaGuid, Layer, TemplateName, TemplateFields) 
values 
(@idEngine,@MediaGuid, @Layer, @TemplateName, @TemplateFields)", connection);
                    cmd.Parameters.AddWithValue("@idEngine", template.Engine.Id);
                }
                else
                {
                    cmd = new DbCommandRedundant(
    @"UPDATE template SET 
MediaGuid=@MediaGuid, 
Layer=@Layer, 
TemplateName=@TemplateName, 
TemplateFields=@TemplateFields 
WHERE idTemplate=@idTemplate"
                    , connection);
                    cmd.Parameters.AddWithValue("@idTemplate", template.idTemplate);
                }
                cmd.Parameters.AddWithValue("@MediaGuid", template.MediaGuid);
                cmd.Parameters.AddWithValue("@Layer", template.Layer);
                cmd.Parameters.AddWithValue("@TemplateName", template.TemplateName);
                cmd.Parameters.AddWithValue("@TemplateFields", SerializationHelper.Serialize<Dictionary<string, string>>(template.TemplateFields));
                cmd.ExecuteNonQuery();
                if (isInsert)
                    template.idTemplate = (UInt64)cmd.LastInsertedId;
            }
        }

        internal static void DbDelete(this Template template)
        {
            if (template.idTemplate == 0)
                return;
            lock (connection)
            {
                string query = "DELETE FROM template WHERE idTemplate=@idTemplate;";
                DbCommandRedundant cmd = new DbCommandRedundant(query, connection);
                cmd.Parameters.AddWithValue("@idTemplate", template.idTemplate);
                cmd.ExecuteNonQuery();
            }
        }

        internal static void DbReadTemplates(this Engine engine)
        {
            lock (connection)
            {
                DbCommandRedundant cmd = new DbCommandRedundant("SELECT idTemplate, MediaGuid, Layer, TemplateName, TemplateFields from template where idEngine=@idEngine;", connection);
                cmd.Parameters.AddWithValue("idEngine", engine.Id);
                using (DbDataReaderRedundant dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        new Template(engine)
                        {
                            idTemplate = dataReader.GetUInt64("idTemplate"),
                            MediaGuid = (dataReader.IsDBNull(dataReader.GetOrdinal("MediaGuid"))) ? Guid.Empty : dataReader.GetGuid("MediaGuid"),
                            Layer = (dataReader.IsDBNull(dataReader.GetOrdinal("Layer"))) ? 0 : dataReader.GetInt32("Layer"),
                            TemplateName = (dataReader.IsDBNull(dataReader.GetOrdinal("TemplateName"))) ? string.Empty : dataReader.GetString("TemplateName"),
                            TemplateFields = dataReader.IsDBNull(dataReader.GetOrdinal("TemplateFields")) ? null : SerializationHelper.Deserialize<Dictionary<string, string>>(dataReader.GetString("TemplateFields")),
                        };
                    }
                }
                Debug.WriteLine(engine, "TemplateReadTemplates readed");
            }
        }
    }
}
