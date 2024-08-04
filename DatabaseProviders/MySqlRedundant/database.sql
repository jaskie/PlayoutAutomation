CREATE TABLE `archive` (
  `idArchive` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `Folder` varchar(255) COLLATE `utf8_general_ci` DEFAULT NULL,
  PRIMARY KEY (`idArchive`)
);

CREATE TABLE `archivemedia` (
  `idArchiveMedia` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) DEFAULT NULL,
  `idArchive` bigint(20) unsigned DEFAULT NULL,
  `MediaName` varchar(255) DEFAULT NULL,
  `Folder` varchar(255) DEFAULT NULL,
  `FileName` varchar(255) DEFAULT NULL,
  `FileSize` bigint(20) unsigned DEFAULT NULL,
  `LastUpdated` datetime NULL DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `DurationPlay` time(6) DEFAULT NULL,
  `typVideo` tinyint(3) unsigned DEFAULT NULL,
  `typAudio` tinyint(3) unsigned DEFAULT NULL,
  `typMedia` int(11) DEFAULT NULL,
  `AudioVolume` decimal(4,2) DEFAULT NULL,
  `AudioLevelIntegrated` decimal(4,2) DEFAULT NULL,
  `AudioLevelPeak` decimal(4,2) DEFAULT NULL,
  `statusMedia` int(11) DEFAULT NULL,
  `TCStart` time(6) DEFAULT NULL,
  `TCPlay` time(6) DEFAULT NULL,
  `idProgramme` bigint(20) unsigned DEFAULT NULL,
  `idAux` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  `KillDate` date DEFAULT NULL,
  `flags` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`idArchiveMedia`),
  KEY `idxArchive` (`idArchive`),
  KEY `idxMediaGuid` (`MediaGuid`)
);

CREATE TABLE `asrunlog` (
  `idAsRunLog` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `idEngine` BIGINT(20) NULL,
  `ExecuteTime` datetime(3) NULL DEFAULT NULL,
  `MediaName` varchar(255) DEFAULT NULL,
  `StartTC` time(6) DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `idProgramme` bigint(20) unsigned DEFAULT NULL,
  `idAuxMedia` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  `idAuxRundown` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  `SecEvents` varchar(100) COLLATE `utf8_general_ci` DEFAULT NULL,
  `typVideo` tinyint(3) unsigned DEFAULT NULL,
  `typAudio` tinyint(3) unsigned DEFAULT NULL,
  `Flags` bigint(20) unsigned DEFAULT NULL,
  PRIMARY KEY (`idAsRunLog`),
  KEY `ixExecuteTime` (`ExecuteTime`),
  KEY `ixIdEngine` (`idEngine`)
);

CREATE TABLE `customcommand` (
  `idCustomCommand` bigint(20) unsigned NOT NULL,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `CommandName` varchar(45) DEFAULT NULL,
  `CommandIn` varchar(250) COLLATE `utf8_general_ci` DEFAULT NULL,
  `CommandOut` varchar(250) COLLATE `utf8_general_ci` DEFAULT NULL,
  PRIMARY KEY (`idCustomCommand`)
);

CREATE TABLE `engine` (
  `idEngine` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `Instance` bigint(20) unsigned DEFAULT NULL,
  `idServerPRI` bigint(20) unsigned DEFAULT NULL,
  `ServerChannelPRI` int(11) DEFAULT NULL,
  `idServerSEC` bigint(20) unsigned DEFAULT NULL,
  `ServerChannelSEC` int(11) DEFAULT NULL,
  `idServerPRV` bigint(20) unsigned DEFAULT NULL,
  `ServerChannelPRV` int(11) DEFAULT NULL,
  `idArchive` bigint(20) DEFAULT NULL,
  `Config` text COLLATE `utf8_general_ci`,
  PRIMARY KEY (`idEngine`)
);

CREATE TABLE `mediasegments` (
  `idMediaSegment` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) NOT NULL,
  `TCIn` time(6) DEFAULT NULL,
  `TCOut` time(6) DEFAULT NULL,
  `SegmentName` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`idMediaSegment`),
  KEY `ixMediaGuid` (`MediaGuid`)
);

CREATE TABLE `media_templated` (
  `MediaGuid` binary(16) NOT NULL,
  `Method` TINYINT NULL,
  `TemplateLayer` INT NULL,
  `ScheduledDelay` TIME(6) NULL,
  `StartType` TINYINT NULL,
  `Fields` text,
  PRIMARY KEY (`MediaGuid`)
);

CREATE TABLE `rundownevent` (
  `idRundownEvent` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `idEventBinding` bigint(20) unsigned DEFAULT NULL,
  `MediaGuid` binary(16) DEFAULT NULL,
  `typEvent` tinyint(3) unsigned DEFAULT NULL,
  `typStart` tinyint(3) unsigned DEFAULT NULL,
  `ScheduledTime` datetime(3) NULL DEFAULT NULL,
  `ScheduledDelay` time(6) DEFAULT NULL,
  `ScheduledTC` time(6) DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `EventName` varchar(255) DEFAULT NULL,
  `Layer` tinyint(3) DEFAULT NULL,
  `AudioVolume` decimal(4,2) DEFAULT NULL,
  `StartTime` datetime(3) NULL DEFAULT NULL,
  `StartTC` time(6) DEFAULT NULL,
  `RequestedStartTime` time(6) DEFAULT NULL,
  `PlayState` tinyint(3) unsigned DEFAULT NULL,
  `TransitionTime` time(3) DEFAULT NULL,
  `TransitionPauseTime` time(3) DEFAULT NULL,
  `typTransition` smallint unsigned DEFAULT NULL,
  `idProgramme` bigint(20) unsigned DEFAULT NULL,
  `idCustomCommand` bigint(20) unsigned DEFAULT NULL,
  `flagsEvent` int(10) unsigned DEFAULT NULL,
  `idAux` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  `Commands` TEXT NULL,
  `RouterPort` smallint DEFAULT NULL,
  `RecordingInfo` JSON DEFAULT NULL,
  PRIMARY KEY (`idRundownEvent`),
  KEY `idEventBinding` (`idEventBinding`) USING BTREE,
  KEY `id_ScheduledTime` (`ScheduledTime`) USING BTREE,
  KEY `idPlaystate` (`PlayState`) USING BTREE
);

CREATE TABLE `rundownevent_templated` (
  `idrundownevent_templated` bigint(20) unsigned NOT NULL,
  `Method` TINYINT NULL,
  `TemplateLayer` INT NULL,
  `Fields` text,
  PRIMARY KEY (`idrundownevent_templated`)
);

CREATE TABLE `server` (
  `idServer` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `typServer` int(11) DEFAULT NULL,
  `Config` text COLLATE `utf8_general_ci`,
  PRIMARY KEY (`idServer`)
);

CREATE TABLE `servermedia` (
  `idserverMedia` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) DEFAULT NULL,
  `idServer` bigint(20) unsigned DEFAULT NULL,
  `MediaName` varchar(255) DEFAULT NULL,
  `Folder` varchar(255) DEFAULT NULL,
  `FileName` varchar(255) DEFAULT NULL,
  `FileSize` bigint(20) unsigned DEFAULT NULL,
  `LastUpdated` datetime NULL DEFAULT NULL,
  `LastPlayed` datetime(3) NULL DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `DurationPlay` time(6) DEFAULT NULL,
  `typVideo` tinyint(3) unsigned DEFAULT NULL,
  `typAudio` tinyint(3) unsigned DEFAULT NULL,
  `typMedia` int(11) DEFAULT NULL,
  `AudioVolume` decimal(4,2) DEFAULT NULL,
  `AudioLevelIntegrated` decimal(4,2) DEFAULT NULL,
  `AudioLevelPeak` decimal(4,2) DEFAULT NULL,
  `statusMedia` int(11) DEFAULT NULL,
  `TCStart` time(6) DEFAULT NULL,
  `TCPlay` time(6) DEFAULT NULL,
  `idProgramme` bigint(20) unsigned DEFAULT NULL,
  `idAux` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  `KillDate` date DEFAULT NULL,
  `flags` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`idserverMedia`),
  KEY `idxServer` (`idServer`),
  KEY `idxMediaGuid` (`MediaGuid`)
);

CREATE TABLE `params` (
  `Section` VARCHAR(50) NOT NULL,
  `Key` VARCHAR(50) NOT NULL,
  `Value` VARCHAR(100) NULL,
  PRIMARY KEY (`Section`, `Key`));

CREATE TABLE `aco` (
  `idACO` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `typACO` int(11) DEFAULT NULL,
  `Config` text,
  PRIMARY KEY (`idACO`)
) COMMENT='groups, users, roles';

CREATE TABLE `rundownevent_acl` (
  `idRundownevent_ACL` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `idRundownEvent` bigint(20) unsigned DEFAULT NULL,
  `idACO` bigint(20) unsigned DEFAULT NULL,
  `ACL` bigint(20) unsigned DEFAULT NULL,
  PRIMARY KEY (`idRundownevent_ACL`),
  KEY `idRundownEvent_idx` (`idRundownEvent`),
  KEY `idACO_idx` (`idACO`),
  CONSTRAINT `rundownevent_acl_ACO` FOREIGN KEY (`idACO`) REFERENCES `aco` (`idACO`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `rundownevent_acl_RundownEvent` FOREIGN KEY (`idRundownEvent`) REFERENCES `rundownevent` (`idRundownEvent`) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE `engine_acl` (
  `idEngine_ACL` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `idACO` bigint(20) unsigned DEFAULT NULL,
  `ACL` bigint(20) unsigned DEFAULT NULL,
  PRIMARY KEY (`idEngine_ACL`),
  KEY `idEngine_idx` (`idEngine`),
  KEY `idAco_idx` (`idACO`),
  CONSTRAINT `engine_acl_ACO` FOREIGN KEY (`idACO`) REFERENCES `aco` (`idACO`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `engine_acl_Engine` FOREIGN KEY (`idEngine`) REFERENCES `engine` (`idEngine`) ON DELETE CASCADE ON UPDATE CASCADE
);


INSERT INTO `params` (`Section`, `Key`, `Value`) VALUES ('DATABASE', 'VERSION', '15');