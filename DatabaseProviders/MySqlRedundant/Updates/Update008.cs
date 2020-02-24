namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update008 : UpdateBase
    {
        private const string Script = @"
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
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
