namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update009 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `media_templated` 
ADD COLUMN `ScheduledDelay` TIME(6) NULL AFTER `TemplateLayer`,
ADD COLUMN `StartType` TINYINT NULL AFTER `ScheduledDelay`;
";

        public override void Update(DbConnectionRedundant connection)
        {
            SimpleUpdate(connection, Script);
        }
    }


}
