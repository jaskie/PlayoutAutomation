namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update005 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `rundownevent`
ADD COLUMN `TransitionPauseTime` time(3) DEFAULT NULL AFTER `TransitionTime`,
CHANGE COLUMN `typTransition` `typTransition` SMALLINT UNSIGNED NULL DEFAULT NULL;
UPDATE `rundownevent` SET `Layer`=`Layer`+16 WHERE `Layer` >= 0;
UPDATE `rundownevent` SET `Layer`=26 WHERE `typEvent` = 6;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
