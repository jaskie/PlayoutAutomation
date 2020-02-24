namespace TAS.Database.MySqlRedundant.Updates
{
    internal class Update011 : UpdateBase
    {
        private const string Script = @"
ALTER TABLE `asrunlog` 
ADD COLUMN `idEngine` BIGINT(20) NULL AFTER `idAsRunLog`,
ADD INDEX `ixIdEngine` (`idEngine` ASC);
";

        public override void Update()
        {
            SimpleUpdate(Script);
        }
    }


}
