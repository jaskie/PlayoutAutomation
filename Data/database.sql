CREATE DATABASE  IF NOT EXISTS `tas` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `tas`;
-- MySQL dump 10.13  Distrib 5.6.17, for Win32 (x86)
--
-- Host: localhost    Database: tas
-- ------------------------------------------------------
-- Server version	5.6.20-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `archive`
--

DROP TABLE IF EXISTS `archive`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `archive` (
  `idArchive` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `Folder` varchar(255) COLLATE utf8_polish_ci DEFAULT NULL,
  PRIMARY KEY (`idArchive`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `archive`
--

LOCK TABLES `archive` WRITE;
/*!40000 ALTER TABLE `archive` DISABLE KEYS */;
INSERT INTO `archive` VALUES (1,'e:\\archive');
/*!40000 ALTER TABLE `archive` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `archivemedia`
--

DROP TABLE IF EXISTS `archivemedia`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `archivemedia` (
  `idArchiveMedia` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) DEFAULT NULL,
  `idArchive` bigint(20) unsigned DEFAULT NULL,
  `MediaName` varchar(100) COLLATE utf8_polish_ci DEFAULT NULL,
  `Folder` varchar(255) COLLATE utf8_polish_ci DEFAULT NULL,
  `FileName` varchar(255) COLLATE utf8_polish_ci DEFAULT NULL,
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
  `idAux` varchar(16) COLLATE utf8_polish_ci DEFAULT NULL,
  `KillDate` date DEFAULT NULL,
  `flags` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`idArchiveMedia`),
  KEY `idxArchive` (`idArchive`),
  KEY `idxMediaGuid` (`MediaGuid`)
) ENGINE=InnoDB AUTO_INCREMENT=56 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `asrunlog`
--

DROP TABLE IF EXISTS `asrunlog`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `asrunlog` (
  `idAsRunLog` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `ExecuteTime` timestamp(6) NULL DEFAULT NULL,
  `MediaName` varchar(100) DEFAULT NULL,
  `StartTC` time(6) DEFAULT NULL,
  `Duration` time(6) DEFAULT NULL,
  `idProgramme` bigint(20) unsigned DEFAULT NULL,
  `idAuxMedia` varchar(16) DEFAULT NULL,
  `idAuxRundown` varchar(16) DEFAULT NULL,
  `SecEvents` varchar(100) DEFAULT NULL,
  `typVideo` tinyint(4) DEFAULT NULL,
  `typAudio` tinyint(4) DEFAULT NULL,
  PRIMARY KEY (`idAsRunLog`),
  KEY `ixExecuteTime` (`ExecuteTime`)
) ENGINE=InnoDB AUTO_INCREMENT=512 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;


--
-- Table structure for table `customcommand`
--

DROP TABLE IF EXISTS `customcommand`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customcommand` (
  `idCustomCommand` bigint(20) unsigned NOT NULL,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `CommandName` varchar(45) COLLATE utf8_polish_ci DEFAULT NULL,
  `CommandIn` varchar(250) COLLATE utf8_polish_ci DEFAULT NULL,
  `CommandOut` varchar(250) COLLATE utf8_polish_ci DEFAULT NULL,
  PRIMARY KEY (`idCustomCommand`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `engine`
--

DROP TABLE IF EXISTS `engine`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `engine` (
  `idEngine` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `Instance` bigint(20) unsigned DEFAULT NULL,
  `idServerPGM` bigint(20) unsigned DEFAULT NULL,
  `ServerChannelPGM` int(11) DEFAULT NULL,
  `idServerPRV` bigint(20) unsigned DEFAULT NULL,
  `ServerChannelPRV` int(11) DEFAULT NULL,
  `idArchive` bigint(20) DEFAULT NULL,
  `Config` text COLLATE utf8_polish_ci,
  PRIMARY KEY (`idEngine`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `engine`
--

LOCK TABLES `engine` WRITE;
/*!40000 ALTER TABLE `engine` DISABLE KEYS */;
INSERT INTO `engine` VALUES (1,0,1,1,2,1,1,'<?xml version=\"1.0\" encoding=\"utf-8\"?>\n  <Engine xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n 	<VideoMode>PAL</VideoMode>\n 	<EngineName>TVP Szczecin</EngineName>\n 	<ArchivePolicy>ArchivePlayedAndNotUsedWhenDeleteEvent</ArchivePolicy>\n 	<GPI Type=\"Remote\" Address=\"127.0.0.1\" />\n  </Engine>');
/*!40000 ALTER TABLE `engine` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `mediasegments`
--

DROP TABLE IF EXISTS `mediasegments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `mediasegments` (
  `idMediaSegment` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) NOT NULL,
  `TCIn` time(6) DEFAULT NULL,
  `TCOut` time(6) DEFAULT NULL,
  `SegmentName` varchar(45) COLLATE utf8_polish_ci DEFAULT NULL,
  PRIMARY KEY (`idMediaSegment`),
  KEY `ixMediaGuid` (`MediaGuid`)
) ENGINE=InnoDB AUTO_INCREMENT=55 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;


--
-- Table structure for table `rundownevent`
--

DROP TABLE IF EXISTS `rundownevent`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
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
  `EventName` varchar(100) COLLATE utf8_polish_ci DEFAULT NULL,
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
  `flagsEvent` int(10) unsigned DEFAULT NULL COMMENT 'bits: 0-disabled, 1-hold. Next not used yet.',
  `idAux` varchar(16) COLLATE utf8_polish_ci DEFAULT NULL,
  PRIMARY KEY (`idRundownEvent`),
  KEY `idEventBinding` (`idEventBinding`) USING BTREE,
  KEY `id_ScheduledTime` (`ScheduledTime`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=6914 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `server`
--

DROP TABLE IF EXISTS `server`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `server` (
  `idServer` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `typServer` int(11) DEFAULT NULL,
  `Config` text COLLATE utf8_polish_ci,
  PRIMARY KEY (`idServer`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `servermedia`
--

DROP TABLE IF EXISTS `servermedia`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `servermedia` (
  `idserverMedia` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `MediaGuid` binary(16) DEFAULT NULL,
  `idServer` bigint(20) unsigned DEFAULT NULL,
  `MediaName` varchar(100) COLLATE utf8_polish_ci DEFAULT NULL,
  `Folder` varchar(255) COLLATE utf8_polish_ci DEFAULT NULL,
  `FileName` varchar(255) COLLATE utf8_polish_ci DEFAULT NULL,
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
  `idAux` varchar(16) COLLATE utf8_polish_ci DEFAULT NULL,
  `KillDate` date DEFAULT NULL,
  `flags` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`idserverMedia`),
  KEY `idxServer` (`idServer`),
  KEY `idxMediaGuid` (`MediaGuid`)
) ENGINE=InnoDB AUTO_INCREMENT=5209 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `template`
--

DROP TABLE IF EXISTS `template`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `template` (
  `idTemplate` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `idEngine` bigint(20) unsigned DEFAULT NULL,
  `MediaGuid` binary(16) DEFAULT NULL,
  `Layer` int(11) DEFAULT NULL,
  `TemplateName` varchar(100) COLLATE utf8_polish_ci DEFAULT NULL,
  `TemplateFields` text COLLATE utf8_polish_ci,
  PRIMARY KEY (`idTemplate`),
  KEY `ixMediaGuid` (`MediaGuid`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8 COLLATE=utf8_polish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `template`
--

LOCK TABLES `template` WRITE;
/*!40000 ALTER TABLE `template` DISABLE KEYS */;
INSERT INTO `template` VALUES (4,NULL,NULL,NULL,NULL,NULL);
/*!40000 ALTER TABLE `template` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'tas'
--

--
-- Dumping routines for database 'tas'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2014-12-30 12:09:01
