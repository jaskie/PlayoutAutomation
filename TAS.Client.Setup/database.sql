CREATE TABLE `archive` (
  `idArchive` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `Folder` varchar(255) COLLATE `utf8_general_ci` DEFAULT NULL,
  PRIMARY KEY (`idArchive`)
) ENGINE=InnoDB;

CREATE TABLE `archivemedia` (
  `idArchiveMedia` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) DEFAULT NULL,
  `idArchive` bigint(20) unsigned DEFAULT NULL,
  `MediaName` varchar(100) DEFAULT NULL,
  `Folder` varchar(255) DEFAULT NULL,
  `FileName` varchar(255) DEFAULT NULL,
  `FileSize` bigint(20) unsigned DEFAULT NULL,
  `LastUpdated` timestamp NULL DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `DurationPlay` time(6) DEFAULT NULL,
  `idFormat` bigint(20) unsigned DEFAULT NULL,
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
) ENGINE=InnoDB;

CREATE TABLE `asrunlog` (
  `idAsRunLog` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `ExecuteTime` timestamp(6) NULL DEFAULT NULL,
  `MediaName` varchar(100) DEFAULT NULL,
  `StartTC` time(6) DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `idProgramme` bigint(20) unsigned DEFAULT NULL,
  `idAuxMedia` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  `idAuxRundown` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  `SecEvents` varchar(100) COLLATE `utf8_general_ci` DEFAULT NULL,
  `typVideo` tinyint(4) DEFAULT NULL,
  `typAudio` tinyint(4) DEFAULT NULL,
  PRIMARY KEY (`idAsRunLog`),
  KEY `ixExecuteTime` (`ExecuteTime`)
) ENGINE=InnoDB;

CREATE TABLE `customcommand` (
  `idCustomCommand` bigint(20) unsigned NOT NULL,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `CommandName` varchar(45) DEFAULT NULL,
  `CommandIn` varchar(250) COLLATE `utf8_general_ci` DEFAULT NULL,
  `CommandOut` varchar(250) COLLATE `utf8_general_ci` DEFAULT NULL,
  PRIMARY KEY (`idCustomCommand`)
) ENGINE=InnoDB;

CREATE TABLE `engine` (
  `idEngine` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `Instance` bigint(20) unsigned DEFAULT NULL,
  `idServerPGM` bigint(20) unsigned DEFAULT NULL,
  `ServerChannelPGM` int(11) DEFAULT NULL,
  `idServerPRV` bigint(20) unsigned DEFAULT NULL,
  `ServerChannelPRV` int(11) DEFAULT NULL,
  `idArchive` bigint(20) DEFAULT NULL,
  `Config` text COLLATE `utf8_general_ci`,
  PRIMARY KEY (`idEngine`)
) ENGINE=InnoDB;

CREATE TABLE `mediasegments` (
  `idMediaSegment` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) NOT NULL,
  `TCIn` time(6) DEFAULT NULL,
  `TCOut` time(6) DEFAULT NULL,
  `SegmentName` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`idMediaSegment`),
  KEY `ixMediaGuid` (`MediaGuid`)
) ENGINE=InnoDB;

CREATE TABLE `rundownevent` (
  `idRundownEvent` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `idEventBinding` bigint(20) unsigned DEFAULT NULL,
  `MediaGuid` binary(16) DEFAULT NULL,
  `typEvent` tinyint(3) unsigned DEFAULT NULL,
  `typStart` tinyint(3) unsigned DEFAULT NULL,
  `ScheduledTime` timestamp(3) NULL DEFAULT NULL,
  `ScheduledDelay` time(6) DEFAULT NULL,
  `ScheduledTC` time(6) DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `EventName` varchar(100) DEFAULT NULL,
  `Layer` tinyint(3) DEFAULT NULL,
  `AudioVolume` decimal(4,2) DEFAULT NULL,
  `StartTime` timestamp(3) NULL DEFAULT NULL,
  `StartTC` time(6) DEFAULT NULL,
  `RequestedStartTime` time(6) DEFAULT NULL,
  `PlayState` tinyint(3) unsigned DEFAULT NULL,
  `TransitionTime` time(6) DEFAULT NULL,
  `typTransition` tinyint(3) unsigned DEFAULT NULL,
  `idProgramme` bigint(20) unsigned DEFAULT NULL,
  `idCustomCommand` bigint(20) unsigned DEFAULT NULL,
  `flagsEvent` int(10) unsigned DEFAULT NULL,
  `idAux` varchar(16) COLLATE `utf8_general_ci` DEFAULT NULL,
  PRIMARY KEY (`idRundownEvent`),
  KEY `idEventBinding` (`idEventBinding`) USING BTREE,
  KEY `id_ScheduledTime` (`ScheduledTime`) USING BTREE
) ENGINE=InnoDB;

CREATE TABLE `server` (
  `idServer` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `typServer` int(11) DEFAULT NULL,
  `Config` text COLLATE `utf8_general_ci`,
  PRIMARY KEY (`idServer`)
) ENGINE=InnoDB;

CREATE TABLE `servermedia` (
  `idserverMedia` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) DEFAULT NULL,
  `idServer` bigint(20) unsigned DEFAULT NULL,
  `MediaName` varchar(100) DEFAULT NULL,
  `Folder` varchar(255) DEFAULT NULL,
  `FileName` varchar(255) DEFAULT NULL,
  `FileSize` bigint(20) unsigned DEFAULT NULL,
  `LastUpdated` timestamp NULL DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `DurationPlay` time(6) DEFAULT NULL,
  `idFormat` bigint(20) unsigned DEFAULT NULL,
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
) ENGINE=InnoDB;

CREATE TABLE `template` (
  `idTemplate` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `MediaGuid` binary(16) DEFAULT NULL,
  `Layer` int(11) DEFAULT NULL,
  `TemplateName` varchar(100) DEFAULT NULL,
  `TemplateFields` text COLLATE `utf8_general_ci`,
  PRIMARY KEY (`idTemplate`),
  KEY `ixMediaGuid` (`MediaGuid`)
) ENGINE=InnoDB;
