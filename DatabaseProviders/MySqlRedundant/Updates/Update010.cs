namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update010 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `asrunlog` 
CHANGE COLUMN `typVideo` `typVideo` TINYINT(3) UNSIGNED NULL DEFAULT NULL ,
CHANGE COLUMN `typAudio` `typAudio` TINYINT(3) UNSIGNED NULL DEFAULT NULL ;
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
