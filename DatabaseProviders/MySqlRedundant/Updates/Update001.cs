namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update001 : UpdateBase
    {
        private const string Script =@"
ALTER TABLE `engine` 
CHANGE COLUMN `idServerPGM` `idServerPRI` BIGINT(20) UNSIGNED NULL DEFAULT NULL,
CHANGE COLUMN `ServerChannelPGM` `ServerChannelPRI` INT(11) NULL DEFAULT NULL,
ADD COLUMN `idServerSEC` BIGINT UNSIGNED NULL AFTER `ServerChannelPRI`,
ADD COLUMN `ServerChannelSEC` INT NULL AFTER `idServerSEC`;
CREATE TABLE `params` (
  `Section` VARCHAR(50) NOT NULL,
  `Key` VARCHAR(50) NOT NULL,
  `Value` VARCHAR(100) NULL,
  PRIMARY KEY (`Section`, `Key`));
UPDATE `engine` SET `idServerSEC`='0', `ServerChannelSEC`='0';
ALTER TABLE `rundownevent` ADD INDEX `idPlayState` (`PlayState` ASC);
INSERT INTO `params` (`Section`, `Key`, `Value`) VALUES ('DATABASE', 'VERSION', '1');
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
