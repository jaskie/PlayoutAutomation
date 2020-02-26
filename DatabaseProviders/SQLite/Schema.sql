BEGIN TRANSACTION;
CREATE TABLE archive (
  idArchive INTEGER PRIMARY KEY,
  Folder TEXT
);

CREATE TABLE archivemedia (
  idArchiveMedia INTEGER PRIMARY KEY,
  MediaGuid BLOB NOT NULL,
  idArchive TEXT,
  MediaName TEXT,
  Folder TEXT,
  FileName TEXT,
  FileSize INTEGER NOT NULL,
  LastUpdated INTEGER NOT NULL,
  Duration INTEGER NOT NULL,
  DurationPlay INTEGER NOT NULL,
  typVideo INTEGER NOT NULL,
  typAudio INTEGER NOT NULL,
  typMedia INTEGER NOT NULL,
  AudioVolume NUMERIC NOT NULL,
  AudioLevelIntegrated NUMERIC NOT NULL,
  AudioLevelPeak NUMERIC NOT NULL,
  statusMedia INTEGER NOT NULL,
  TCStart INTEGER NOT NULL,
  TCPlay INTEGER NOT NULL,
  idProgramme INTEGER NOT NULL,
  idAux TEXT,
  KillDate INTEGER,
  flags INTEGER NOT NULL
);

CREATE INDEX archivemedia_idArchive ON archivemedia (idArchive);
CREATE INDEX archivemedia_MediaGuid ON archivemedia (MediaGuid);

CREATE TABLE asrunlog (
  idAsRunLog INTEGER PRIMARY KEY,
  idEngine INTEGER NOT NULL,
  ExecuteTime INTEGER NOT NULL,
  MediaName TEXT,
  StartTC INTEGER NOT NULL,
  Duration INTEGER NOT NULL,
  idProgramme INTEGER NOT NULL,
  idAuxMedia TEXT,
  idAuxRundown TEXT,
  SecEvents TEXT,
  typVideo INTEGER,
  typAudio INTEGER,
  Flags INTEGER NOT NULL
);

CREATE INDEX asrunlog_ExecuteTime ON asrunlog (ExecuteTime);
CREATE INDEX asrunlog_idEngine ON asrunlog (idEngine);

CREATE TABLE customcommand (
  idCustomCommand INTEGER PRIMARY KEY,
  idEngine INTEGER NOT NULL,
  CommandName TEXT,
  CommandIn TEXT,
  CommandOut TEXT 
);

CREATE TABLE engine (
  idEngine INTEGER PRIMARY KEY,
  Instance INTEGER NOT NULL,
  idServerPRI INTEGER NOT NULL,
  ServerChannelPRI INTEGER NOT NULL,
  idServerSEC INTEGER NOT NULL,
  ServerChannelSEC INTEGER NOT NULL,
  idServerPRV INTEGER NOT NULL,
  ServerChannelPRV INTEGER NOT NULL,
  idArchive INTEGER NOT NULL,
  Config TEXT NOT NULL
);

CREATE TABLE mediasegments (
  idMediaSegment INTEGER PRIMARY KEY,
  MediaGuid BLOB NOT NULL,
  TCIn INTEGER NOT NULL,
  TCOut INTEGER NOT NULL,
  SegmentName TEXT NOT NULL
);

CREATE INDEX mediasegments_MediaGuid ON mediasegments (MediaGuid);

CREATE TABLE media_templated (
  MediaGuid BLOB PRIMARY KEY NOT NULL,
  Method INTEGER NOT NULL,
  TemplateLayer INTEGER NOT NULL,
  ScheduledDelay INTEGER NOT NULL,
  StartType INTEGER NOT NULL,
  Fields TEXT NOT NULL
);

CREATE TABLE rundownevent (
  idRundownEvent INTEGER PRIMARY KEY,
  idEngine INTEGER NOT NULL,
  idEventBinding INTEGER NOT NULL,
  MediaGuid BLOB,
  typEvent INTEGER NOT NULL,
  typStart INTEGER NOT NULL,
  ScheduledTime INTEGER,
  ScheduledDelay INTEGER NOT NULL,
  ScheduledTC INTEGER,
  Duration INTEGER NOT NULL,
  EventName TEXT NOT NULL,
  Layer INTEGER NOT NULL,
  AudioVolume NUMERIC,
  StartTime INTEGER,
  StartTC INTEGER,
  RequestedStartTime INTEGER,
  PlayState INTEGER NOT NULL,
  TransitionTime INTEGER NOT NULL,
  TransitionPauseTime INTEGER NOT NULL,
  typTransition INTEGER NOT NULL,
  idProgramme INTEGER NOT NULL,
  idCustomCommand INTEGER,
  flagsEvent INTEGER,
  idAux TEXT,
  Commands TEXT,
  RouterPort INTEGER,
  RecordingInfo TEXT
);

CREATE INDEX rundownevent_idEventBinding ON rundownevent (idEventBinding);
CREATE INDEX rundownevent_ScheduledTime ON rundownevent (ScheduledTime);
CREATE INDEX rundownevent_PlayState ON rundownevent (PlayState);

CREATE TABLE rundownevent_templated (
  idrundownevent_templated INTEGER PRIMARY KEY,
  Method INTEGER NOT NULL,
  TemplateLayer INTEGER NOT NULL,
  Fields TEXT
);

CREATE TABLE server (
  idServer INTEGER PRIMARY KEY,
  Config TEXT
);

CREATE TABLE servermedia (
  idserverMedia INTEGER PRIMARY KEY,
  MediaGuid BLOB NOT NULL,
  idServer INTEGER NOT NULL,
  MediaName TEXT,
  Folder TEXT,
  FileName TEXT,
  FileSize INTEGER NOT NULL,
  LastUpdated INTEGER NOT NULL,
  Duration INTEGER NOT NULL,
  DurationPlay INTEGER NOT NULL,
  typVideo INTEGER NOT NULL,
  typAudio INTEGER NOT NULL,
  typMedia INTEGER NOT NULL,
  AudioVolume NUMERIC NOT NULL,
  AudioLevelIntegrated NUMERIC NOT NULL,
  AudioLevelPeak NUMERIC NOT NULL,
  statusMedia INTEGER NOT NULL,
  TCStart INTEGER NOT NULL,
  TCPlay INTEGER NOT NULL,
  idProgramme INTEGER NOT NULL,
  idAux TEXT,
  KillDate INTEGER,
  flags INTEGER NOT NULL
);

CREATE INDEX servermedia_idServer ON servermedia (idServer);
CREATE INDEX servermedia_MediaGuid ON servermedia (MediaGuid);


CREATE TABLE aco (
  idACO INTEGER PRIMARY KEY,
  typACO INTEGER NOT NULL,
  Config TEXT
);

CREATE TABLE rundownevent_acl (
  idRundownevent_ACL INTEGER PRIMARY KEY,
  idRundownEvent INTEGER NOT NULL,
  idACO INTEGER NOT NULL,
  ACL INTEGER NOT NULL,
  CONSTRAINT rundownevent_acl_ACO FOREIGN KEY (idACO) REFERENCES aco (idACO) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT rundownevent_acl_RundownEvent FOREIGN KEY (idRundownEvent) REFERENCES rundownevent (idRundownEvent) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX rundownevent_acl_idRundownEvent ON rundownevent_acl (idRundownEvent);
CREATE INDEX rundownevent_acl_idACO ON rundownevent_acl (idACO);

CREATE TABLE engine_acl (
  idEngine_ACL INTEGER PRIMARY KEY,
  idEngine INTEGER NOT NULL,
  idACO INTEGER NOT NULL,
  ACL INTEGER NOT NULL,
  CONSTRAINT engine_acl_ACO FOREIGN KEY (idACO) REFERENCES aco (idACO) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT engine_acl_Engine FOREIGN KEY (idEngine) REFERENCES engine (idEngine) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX engine_acl_idEngine ON engine_acl (idEngine);
CREATE INDEX engine_acl_idACO ON engine_acl (idACO);

PRAGMA user_version = 1;

COMMIT;