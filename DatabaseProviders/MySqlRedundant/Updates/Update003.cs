namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update003 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `media_templated` 
ADD COLUMN `Method` TINYINT NULL AFTER `MediaGuid`,
 ADD COLUMN `TemplateLayer` INT NULL AFTER `Method`;
ALTER TABLE `rundownevent_templated` 
ADD COLUMN `Method` TINYINT NULL AFTER `idrundownevent_templated`, 
ADD COLUMN `TemplateLayer` INT NULL AFTER `Method`;
";

        public override void Update(DbConnectionRedundant connection)
        {
            SimpleUpdate(connection, Script);
        }
    }


}
