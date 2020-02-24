namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update006 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `rundownevent` 
ADD COLUMN `Commands` TEXT NULL;
ALTER TABLE `rundownevent_templated` 
CHANGE COLUMN `idrundownevent_templated` `idrundownevent_templated` BIGINT UNSIGNED NOT NULL ;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
