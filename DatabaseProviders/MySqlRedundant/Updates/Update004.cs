namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update004 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `rundownevent` 
CHANGE COLUMN `ScheduledTime` `ScheduledTime` DATETIME(3) NULL DEFAULT NULL,
CHANGE COLUMN `StartTime` `StartTime` DATETIME(3) NULL DEFAULT NULL;
ALTER TABLE `asrunlog` 
CHANGE COLUMN `ExecuteTime` `ExecuteTime` DATETIME(3) NULL DEFAULT NULL;
ALTER TABLE `servermedia` 
CHANGE COLUMN `LastUpdated` `LastUpdated` DATETIME NULL DEFAULT NULL;
ALTER TABLE `archivemedia` 
CHANGE COLUMN `LastUpdated` `LastUpdated` DATETIME NULL DEFAULT NULL ;
";

        public override void Update(DbConnectionRedundant connection)
        {
            SimpleUpdate(connection, Script);
        }
    }


}
